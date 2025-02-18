// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System.Numerics;
using static Box2D.NET.core;
using static Box2D.NET.math_function;

namespace Box2D.NET
{

    /**
     * @defgroup geometry Geometry
     * @brief Geometry types and algorithms
     *
     * Definitions of circles, capsules, segments, and polygons. Various algorithms to compute hulls, mass properties, and so on.
     * @{
     */
    /// Low level ray cast input data
    public struct b2RayCastInput
    {
        /// Start point of the ray cast
        public b2Vec2 origin;

        /// Translation of the ray cast
        public b2Vec2 translation;

        /// The maximum fraction of the translation to consider, typically 1
        public float maxFraction;

        public b2RayCastInput(b2Vec2 origin, b2Vec2 translation, float maxFraction)
        {
            this.origin = origin;
            this.translation = translation;
            this.maxFraction = maxFraction;
        }
    }

    /// Low level shape cast input in generic form. This allows casting an arbitrary point
    /// cloud wrap with a radius. For example, a circle is a single point with a non-zero radius.
    /// A capsule is two points with a non-zero radius. A box is four points with a zero radius.
    public struct b2ShapeCastInput
    {
        /// A point cloud to cast
        //public b2Vec2[] points = new b2Vec2[constants.B2_MAX_POLYGON_VERTICES];
        public b2Vec2[] points;

        /// The number of points
        public int count;

        /// The radius around the point cloud
        public float radius;

        /// The translation of the shape cast
        public b2Vec2 translation;

        /// The maximum fraction of the translation to consider, typically 1
        public float maxFraction;
    }

    /// Low level ray cast or shape-cast output data
    public class b2CastOutput
    {
        /// The surface normal at the hit point
        public b2Vec2 normal;

        /// The surface hit point
        public b2Vec2 point;

        /// The fraction of the input translation at collision
        public float fraction;

        /// The number of iterations used
        public int iterations;

        /// Did the cast hit?
        public bool hit;
    }

    /// This holds the mass data computed for a shape.
    public class b2MassData // TODO: @ikpil class or struct
    {
        /// The mass of the shape, usually in kilograms.
        public float mass;

        /// The position of the shape's centroid relative to the shape's origin.
        public b2Vec2 center;

        /// The rotational inertia of the shape about the local origin.
        public float rotationalInertia;

        public b2MassData()
        {

        }

        public b2MassData(float mass, b2Vec2 center, float rotationalInertia)
        {
            this.mass = mass;
            this.center = center;
            this.rotationalInertia = rotationalInertia;
        }
    }

    /// A solid circle
    public class b2Circle
    {
        /// The local center
        public b2Vec2 center;

        /// The radius
        public float radius;

        public b2Circle(b2Vec2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
    }

    /// A solid capsule can be viewed as two semicircles connected
    /// by a rectangle.
    public class b2Capsule
    {
        /// Local center of the first semicircle
        public b2Vec2 center1;

        /// Local center of the second semicircle
        public b2Vec2 center2;

        /// The radius of the semicircles
        public float radius;

        public b2Capsule(b2Vec2 center1, b2Vec2 center2, float radius)
        {
            this.center1 = center1;
            this.center2 = center2;
            this.radius = radius;
        }
    }

    /// A solid convex polygon. It is assumed that the interior of the polygon is to
    /// the left of each edge.
    /// Polygons have a maximum number of vertices equal to B2_MAX_POLYGON_VERTICES.
    /// In most cases you should not need many vertices for a convex polygon.
    /// @warning DO NOT fill this out manually, instead use a helper function like
    /// b2MakePolygon or b2MakeBox.
    public class b2Polygon
    {
        /// The polygon vertices
        public readonly b2Vec2[] vertices = new b2Vec2[constants.B2_MAX_POLYGON_VERTICES];

        /// The outward normal vectors of the polygon sides
        public readonly b2Vec2[] normals = new b2Vec2[constants.B2_MAX_POLYGON_VERTICES];

        /// The centroid of the polygon
        public b2Vec2 centroid;

        /// The external radius for rounded polygons
        public float radius;

        /// The number of polygon vertices
        public int count;

        public b2Polygon Clone()
        {
            var p = new b2Polygon();
            memcpy<b2Vec2>(p.vertices, vertices);
            memcpy<b2Vec2>(p.normals, normals);
            p.centroid = centroid;
            p.radius = radius;
            p.count = count;
            return p;
        }
    }

    /// A line segment with two-sided collision.
    public class b2Segment
    {
        /// The first point
        public b2Vec2 point1;

        /// The second point
        public b2Vec2 point2;

        public b2Segment(b2Vec2 point1, b2Vec2 point2)
        {
            this.point1 = point1;
            this.point2 = point2;
        }
    }

    /// A line segment with one-sided collision. Only collides on the right side.
    /// Several of these are generated for a chain shape.
    /// ghost1 -> point1 -> point2 -> ghost2
    public class b2ChainSegment
    {
        /// The tail ghost vertex
        public b2Vec2 ghost1;

        /// The line segment
        public b2Segment segment = new b2Segment(b2Vec2_zero, b2Vec2_zero);

        /// The head ghost vertex
        public b2Vec2 ghost2;

        /// The owning chain shape index (internal usage only)
        public int chainId;
    }

    /// A convex hull. Used to create convex polygons.
    /// @warning Do not modify these values directly, instead use b2ComputeHull()
    public class b2Hull
    {
        /// The final points of the hull
        public b2Vec2[] points = new b2Vec2[constants.B2_MAX_POLYGON_VERTICES];

        /// The number of points
        public int count;
    }

    /**@}*/
    /**
     * @defgroup distance Distance
     * Functions for computing the distance between shapes.
     *
     * These are advanced functions you can use to perform distance calculations. There
     * are functions for computing the closest points between shapes, doing linear shape casts,
     * and doing rotational shape casts. The latter is called time of impact (TOI).
     * @{
     */
    /// Result of computing the distance between two line segments
    public class b2SegmentDistanceResult
    {
        /// The closest point on the first segment
        public b2Vec2 closest1;

        /// The closest point on the second segment
        public b2Vec2 closest2;

        /// The barycentric coordinate on the first segment
        public float fraction1;

        /// The barycentric coordinate on the second segment
        public float fraction2;

        /// The squared distance between the closest points
        public float distanceSquared;
    }

    /// A distance proxy is used by the GJK algorithm. It encapsulates any shape.
    public class b2ShapeProxy
    {
        /// The point cloud
        public b2Vec2[] points = new b2Vec2[constants.B2_MAX_POLYGON_VERTICES];

        /// The number of points
        public int count;

        /// The external radius of the point cloud
        public float radius;
    }

    /// Used to warm start the GJK simplex. If you call this function multiple times with nearby
    /// transforms this might improve performance. Otherwise you can zero initialize this.
    /// The distance cache must be initialized to zero on the first call.
    /// Users should generally just zero initialize this structure for each call.
    public class b2SimplexCache // TODO: @ikpil, check class or struct, struct 성격이 강하다
    {
        /// The number of stored simplex points
        public ushort count;

        /// The cached simplex indices on shape A
        public byte[] indexA = new byte[3];

        /// The cached simplex indices on shape B
        public byte[] indexB = new byte[3];
    }

    /// Input for b2ShapeDistance
    public class b2DistanceInput
    {
        /// The proxy for shape A
        public b2ShapeProxy proxyA;

        /// The proxy for shape B
        public b2ShapeProxy proxyB;

        /// The world transform for shape A
        public b2Transform transformA;

        /// The world transform for shape B
        public b2Transform transformB;

        /// Should the proxy radius be considered?
        public bool useRadii;
    }

    /// Output for b2ShapeDistance
    public class b2DistanceOutput
    {
        public b2Vec2 pointA; // Closest point on shapeA

        public b2Vec2 pointB; // Closest point on shapeB

        // todo_erin implement this
        // b2Vec2 normal;			// Normal vector that points from A to B
        public float distance; // The final distance, zero if overlapped
        public int iterations; // Number of GJK iterations used
        public int simplexCount; // The number of simplexes stored in the simplex array
    }

    /// Simplex vertex for debugging the GJK algorithm
    public class b2SimplexVertex
    {
        public b2Vec2 wA; // support point in proxyA
        public b2Vec2 wB; // support point in proxyB
        public b2Vec2 w; // wB - wA
        public float a; // barycentric coordinate for closest point
        public int indexA; // wA index
        public int indexB; // wB index
    }

    /// Simplex from the GJK algorithm
    public class b2Simplex
    {
        public b2SimplexVertex v1 = new b2SimplexVertex();
        public b2SimplexVertex v2 = new b2SimplexVertex();
        public b2SimplexVertex v3 = new b2SimplexVertex(); // vertices
        public int count; // number of valid vertices
    }

    /// Input parameters for b2ShapeCast
    public class b2ShapeCastPairInput
    {
        public b2ShapeProxy proxyA; // The proxy for shape A
        public b2ShapeProxy proxyB; // The proxy for shape B
        public b2Transform transformA; // The world transform for shape A
        public b2Transform transformB; // The world transform for shape B
        public b2Vec2 translationB; // The translation of shape B
        public float maxFraction; // The fraction of the translation to consider, typically 1
    }

    /// This describes the motion of a body/shape for TOI computation. Shapes are defined with respect to the body origin,
    /// which may not coincide with the center of mass. However, to support dynamics we must interpolate the center of mass
    /// position.
    public class b2Sweep
    {
        public b2Vec2 localCenter; // Local center of mass position
        public b2Vec2 c1; // Starting center of mass world position
        public b2Vec2 c2; // Ending center of mass world position
        public b2Rot q1; // Starting world rotation
        public b2Rot q2; // Ending world rotation

        public b2Sweep()
        {

        }

        public b2Sweep(b2Vec2 localCenter, b2Vec2 c1, b2Vec2 c2, b2Rot q1, b2Rot q2)
        {
            this.localCenter = localCenter;
            this.c1 = c1;
            this.c2 = c2;
            this.q1 = q1;
            this.q2 = q2;
        }

        public b2Sweep Clone()
        {
            return new b2Sweep(localCenter, c1, c2, q1, q2);
        }
    }

    /// Input parameters for b2TimeOfImpact
    public class b2TOIInput
    {
        public b2ShapeProxy proxyA; // The proxy for shape A
        public b2ShapeProxy proxyB; // The proxy for shape B
        public b2Sweep sweepA; // The movement of shape A
        public b2Sweep sweepB; // The movement of shape B
        public float maxFraction; // Defines the sweep interval [0, maxFraction]
    }

    /// Describes the TOI output
    public enum b2TOIState
    {
        b2_toiStateUnknown,
        b2_toiStateFailed,
        b2_toiStateOverlapped,
        b2_toiStateHit,
        b2_toiStateSeparated
    }

    /// Output parameters for b2TimeOfImpact.
    public class b2TOIOutput
    {
        public b2TOIState state; // The type of result
        public float fraction; // The sweep time of the collision
    }

    /**@}*/
    /**
     * @defgroup collision Collision
     * @brief Functions for colliding pairs of shapes
     * @{
     */
    /// A manifold point is a contact point belonging to a contact manifold.
    /// It holds details related to the geometry and dynamics of the contact points.
    /// Box2D uses speculative collision so some contact points may be separated.
    /// You may use the maxNormalImpulse to determine if there was an interaction during
    /// the time step.
    public struct b2ManifoldPoint
    {
        /// Location of the contact point in world space. Subject to precision loss at large coordinates.
        /// @note Should only be used for debugging.
        public b2Vec2 point;

        /// Location of the contact point relative to shapeA's origin in world space
        /// @note When used internally to the Box2D solver, this is relative to the body center of mass.
        public b2Vec2 anchorA;

        /// Location of the contact point relative to shapeB's origin in world space
        /// @note When used internally to the Box2D solver, this is relative to the body center of mass.
        public b2Vec2 anchorB;

        /// The separation of the contact point, negative if penetrating
        public float separation;

        /// The impulse along the manifold normal vector.
        public float normalImpulse;

        /// The friction impulse
        public float tangentImpulse;

        /// The maximum normal impulse applied during sub-stepping. This is important
        /// to identify speculative contact points that had an interaction in the time step.
        public float maxNormalImpulse;

        /// Relative normal velocity pre-solve. Used for hit events. If the normal impulse is
        /// zero then there was no hit. Negative means shapes are approaching.
        public float normalVelocity;

        /// Uniquely identifies a contact point between two shapes
        public ushort id;

        /// Did this contact point exist the previous step?
        public bool persisted;
    }

    /// A contact manifold describes the contact points between colliding shapes.
    /// @note Box2D uses speculative collision so some contact points may be separated.
    public struct b2Manifold
    {
        /// The unit normal vector in world space, points from shape A to bodyB
        public b2Vec2 normal;

        /// Angular impulse applied for rolling resistance. N * m * s = kg * m^2 / s
        public float rollingImpulse;

        /// The manifold points, up to two are possible in 2D
        public b2ManifoldPoint[] points;

        private Vector<int> sda;

        /// The number of contacts points, will be 0, 1, or 2
        public int pointCount;

        public b2Manifold()
        {
            points = new b2ManifoldPoint[2];
        }
    }

    /**@}*/
    /**
     * @defgroup tree Dynamic Tree
     * The dynamic tree is a binary AABB tree to organize and query large numbers of geometric objects
     *
     * Box2D uses the dynamic tree internally to sort collision shapes into a binary bounding volume hierarchy.
     * This data structure may have uses in games for organizing other geometry data and may be used independently
     * of Box2D rigid body simulation.
     *
     * A dynamic AABB tree broad-phase, inspired by Nathanael Presson's btDbvt.
     * A dynamic tree arranges data in a binary tree to accelerate
     * queries such as AABB queries and ray casts. Leaf nodes are proxies
     * with an AABB. These are used to hold a user collision object.
     * Nodes are pooled and relocatable, so I use node indices rather than pointers.
     * The dynamic tree is made available for advanced users that would like to use it to organize
     * spatial game data besides rigid bodies.
     * @{
     */
    /// The dynamic tree structure. This should be considered private data.
    /// It is placed here for performance reasons.
    public class b2DynamicTree
    {
        /// The tree nodes
        public b2TreeNode[] nodes;

        /// The root index
        public int root;

        /// The number of nodes
        public int nodeCount;

        /// The allocated node space
        public int nodeCapacity;

        /// Node free list
        public int freeList;

        /// Number of proxies created
        public int proxyCount;

        /// Leaf indices for rebuild
        public int[] leafIndices;

        /// Leaf bounding boxes for rebuild
        public b2AABB[] leafBoxes;

        /// Leaf bounding box centers for rebuild
        public b2Vec2[] leafCenters;

        /// Bins for sorting during rebuild
        public int[] binIndices;

        /// Allocated space for rebuilding
        public int rebuildCapacity;

        public void Clear()
        {
            nodes = null;
            root = 0;
            nodeCount = 0;
            nodeCapacity = 0;
            freeList = 0;
            proxyCount = 0;
            leafIndices = null;
            leafBoxes = null;
            leafCenters = null;
            binIndices = null;
            rebuildCapacity = 0;
        }
    }

    /// These are performance results returned by dynamic tree queries.
    public class b2TreeStats
    {
        /// Number of internal nodes visited during the query
        public int nodeVisits;

        /// Number of leaf nodes visited during the query
        public int leafVisits;
    }

    public static class collision
    {
        // TODO: @ikpil, check empty or class
        public static readonly b2SimplexCache b2_emptySimplexCache = new b2SimplexCache();
    }
}