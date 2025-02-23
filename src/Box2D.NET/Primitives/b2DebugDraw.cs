using System;

namespace Box2D.NET.Primitives
{
    /// This struct holds callbacks you can implement to draw a Box2D world.
    /// This structure should be zero initialized.
    /// @ingroup world
    public class b2DebugDraw
    {
        /// Draw a closed polygon provided in CCW order.
        //void ( *DrawPolygon )( const b2Vec2* vertices, int vertexCount, b2HexColor color, object context );
        public delegate void DrawPolygonDelegate(ReadOnlySpan<b2Vec2> vertices, int vertexCount, b2HexColor color, object context);

        public DrawPolygonDelegate DrawPolygon;

        /// Draw a solid closed polygon provided in CCW order.
        public delegate void DrawSolidPolygonDelegate(ref b2Transform transform, ReadOnlySpan<b2Vec2> vertices, int vertexCount, float radius, b2HexColor color, object context);

        public DrawSolidPolygonDelegate DrawSolidPolygon;

        /// Draw a circle.
        public delegate void DrawCircleDelegate(b2Vec2 center, float radius, b2HexColor color, object context);

        public DrawCircleDelegate DrawCircle;

        /// Draw a solid circle.
        public delegate void DrawSolidCircleDelegate(ref b2Transform transform, float radius, b2HexColor color, object context);

        public DrawSolidCircleDelegate DrawSolidCircle;

        /// Draw a solid capsule.
        public delegate void DrawSolidCapsuleDelegate(b2Vec2 p1, b2Vec2 p2, float radius, b2HexColor color, object context);

        public DrawSolidCapsuleDelegate DrawSolidCapsule;

        /// Draw a line segment.
        public delegate void DrawSegmentDelegate(b2Vec2 p1, b2Vec2 p2, b2HexColor color, object context);

        public DrawSegmentDelegate DrawSegment;

        /// Draw a transform. Choose your own length scale.
        public delegate void DrawTransformDelegate(b2Transform transform, object context);

        public DrawTransformDelegate DrawTransform;

        /// Draw a point.
        public delegate void DrawPointDelegate(b2Vec2 p, float size, b2HexColor color, object context);

        public DrawPointDelegate DrawPoint;

        /// Draw a string in world space
        public delegate void DrawStringDelegate(b2Vec2 p, string s, b2HexColor color, object context);

        public DrawStringDelegate DrawString;

        /// Bounds to use if restricting drawing to a rectangular region
        public b2AABB drawingBounds;

        /// Option to restrict drawing to a rectangular region. May suffer from unstable depth sorting.
        public bool useDrawingBounds;

        /// Option to draw shapes
        public bool drawShapes;

        /// Option to draw joints
        public bool drawJoints;

        /// Option to draw additional information for joints
        public bool drawJointExtras;

        /// Option to draw the bounding boxes for shapes
        public bool drawAABBs;

        /// Option to draw the mass and center of mass of dynamic bodies
        public bool drawMass;

        /// Option to draw body names
        public bool drawBodyNames;

        /// Option to draw contact points
        public bool drawContacts;

        /// Option to visualize the graph coloring used for contacts and joints
        public bool drawGraphColors;

        /// Option to draw contact normals
        public bool drawContactNormals;

        /// Option to draw contact normal impulses
        public bool drawContactImpulses;

        /// Option to draw contact friction impulses
        public bool drawFrictionImpulses;

        /// User context that is passed as an argument to drawing callback functions
        public object context;
    }
}