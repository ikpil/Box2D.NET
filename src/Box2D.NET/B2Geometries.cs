// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET
{
    public static class B2Geometries
    {
        // B2_ASSERT( B2_MAX_POLYGON_VERTICES > 2, "must be 3 or more" );

        /// Validate ray cast input data (NaN, etc)
        public static bool b2IsValidRay(ref B2RayCastInput input)
        {
            bool isValid = b2IsValidVec2(input.origin) && b2IsValidVec2(input.translation) &&
                           b2IsValidFloat(input.maxFraction) && 0.0f <= input.maxFraction && input.maxFraction < B2_HUGE;
            return isValid;
        }

        public static B2Vec2 b2ComputePolygonCentroid(ReadOnlySpan<B2Vec2> vertices, int count)
        {
            B2Vec2 center = new B2Vec2(0.0f, 0.0f);
            float area = 0.0f;

            // Get a reference point for forming triangles.
            // Use the first vertex to reduce round-off errors.
            B2Vec2 origin = vertices[0];

            const float inv3 = 1.0f / 3.0f;

            for (int i = 1; i < count - 1; ++i)
            {
                // Triangle edges
                B2Vec2 e1 = b2Sub(vertices[i], origin);
                B2Vec2 e2 = b2Sub(vertices[i + 1], origin);
                float a = 0.5f * b2Cross(e1, e2);

                // Area weighted centroid
                center = b2MulAdd(center, a * inv3, b2Add(e1, e2));
                area += a;
            }

            B2_ASSERT(area > FLT_EPSILON);
            float invArea = 1.0f / area;
            center.X *= invArea;
            center.Y *= invArea;

            // Restore offset
            center = b2Add(origin, center);

            return center;
        }

        /// Make a convex polygon from a convex hull. This will assert if the hull is not valid.
        /// @warning Do not manually fill in the hull data, it must come directly from b2ComputeHull
        public static B2Polygon b2MakePolygon(ref B2Hull hull, float radius)
        {
            B2_ASSERT(b2ValidateHull(ref hull));

            if (hull.count < 3)
            {
                // Handle a bad hull when assertions are disabled
                return b2MakeSquare(0.5f);
            }

            B2Polygon shape = new B2Polygon();
            shape.count = hull.count;
            shape.radius = radius;

            // Copy vertices
            for (int i = 0; i < shape.count; ++i)
            {
                shape.vertices[i] = hull.points[i];
            }

            // Compute normals. Ensure the edges have non-zero length.
            for (int i = 0; i < shape.count; ++i)
            {
                int i1 = i;
                int i2 = i + 1 < shape.count ? i + 1 : 0;
                B2Vec2 edge = b2Sub(shape.vertices[i2], shape.vertices[i1]);
                B2_ASSERT(b2Dot(edge, edge) > FLT_EPSILON * FLT_EPSILON);
                shape.normals[i] = b2Normalize(b2CrossVS(edge, 1.0f));
            }

            shape.centroid = b2ComputePolygonCentroid(shape.vertices.AsSpan(), shape.count);

            return shape;
        }

        /// Make an offset convex polygon from a convex hull. This will assert if the hull is not valid.
        /// @warning Do not manually fill in the hull data, it must come directly from b2ComputeHull
        public static B2Polygon b2MakeOffsetPolygon(ref B2Hull hull, B2Vec2 position, B2Rot rotation)
        {
            return b2MakeOffsetRoundedPolygon(ref hull, position, rotation, 0.0f);
        }

        /// Make an offset convex polygon from a convex hull. This will assert if the hull is not valid.
        /// @warning Do not manually fill in the hull data, it must come directly from b2ComputeHull
        public static B2Polygon b2MakeOffsetRoundedPolygon(ref B2Hull hull, B2Vec2 position, B2Rot rotation, float radius)
        {
            B2_ASSERT(b2ValidateHull(ref hull));

            if (hull.count < 3)
            {
                // Handle a bad hull when assertions are disabled
                return b2MakeSquare(0.5f);
            }

            B2Transform transform = new B2Transform(position, rotation);

            B2Polygon shape = new B2Polygon();
            shape.count = hull.count;
            shape.radius = radius;

            // Copy vertices
            for (int i = 0; i < shape.count; ++i)
            {
                shape.vertices[i] = b2TransformPoint(ref transform, hull.points[i]);
            }

            // Compute normals. Ensure the edges have non-zero length.
            for (int i = 0; i < shape.count; ++i)
            {
                int i1 = i;
                int i2 = i + 1 < shape.count ? i + 1 : 0;
                B2Vec2 edge = b2Sub(shape.vertices[i2], shape.vertices[i1]);
                B2_ASSERT(b2Dot(edge, edge) > FLT_EPSILON * FLT_EPSILON);
                shape.normals[i] = b2Normalize(b2CrossVS(edge, 1.0f));
            }

            shape.centroid = b2ComputePolygonCentroid(shape.vertices.AsSpan(), shape.count);

            return shape;
        }

        /// Make a square polygon, bypassing the need for a convex hull.
        /// @param halfWidth the half-width
        public static B2Polygon b2MakeSquare(float halfWidth)
        {
            return b2MakeBox(halfWidth, halfWidth);
        }

        /// Make a box (rectangle) polygon, bypassing the need for a convex hull.
        /// @param halfWidth the half-width (x-axis)
        /// @param halfHeight the half-height (y-axis)
        public static B2Polygon b2MakeBox(float halfWidth, float halfHeight)
        {
            B2_ASSERT(b2IsValidFloat(halfWidth) && halfWidth > 0.0f);
            B2_ASSERT(b2IsValidFloat(halfHeight) && halfHeight > 0.0f);

            B2Polygon shape = new B2Polygon();
            shape.count = 4;
            shape.vertices[0] = new B2Vec2(-halfWidth, -halfHeight);
            shape.vertices[1] = new B2Vec2(halfWidth, -halfHeight);
            shape.vertices[2] = new B2Vec2(halfWidth, halfHeight);
            shape.vertices[3] = new B2Vec2(-halfWidth, halfHeight);
            shape.normals[0] = new B2Vec2(0.0f, -1.0f);
            shape.normals[1] = new B2Vec2(1.0f, 0.0f);
            shape.normals[2] = new B2Vec2(0.0f, 1.0f);
            shape.normals[3] = new B2Vec2(-1.0f, 0.0f);
            shape.radius = 0.0f;
            shape.centroid = b2Vec2_zero;
            return shape;
        }

        /// Make a rounded box, bypassing the need for a convex hull.
        /// @param halfWidth the half-width (x-axis)
        /// @param halfHeight the half-height (y-axis)
        /// @param radius the radius of the rounded extension
        public static B2Polygon b2MakeRoundedBox(float halfWidth, float halfHeight, float radius)
        {
            B2_ASSERT(b2IsValidFloat(radius) && radius >= 0.0f);
            B2Polygon shape = b2MakeBox(halfWidth, halfHeight);
            shape.radius = radius;
            return shape;
        }

        /// Make an offset box, bypassing the need for a convex hull.
        /// @param halfWidth the half-width (x-axis)
        /// @param halfHeight the half-height (y-axis)
        /// @param center the local center of the box
        /// @param rotation the local rotation of the box
        public static B2Polygon b2MakeOffsetBox(float halfWidth, float halfHeight, B2Vec2 center, B2Rot rotation)
        {
            B2Transform xf = new B2Transform(center, rotation);

            B2Polygon shape = new B2Polygon();
            shape.count = 4;
            shape.vertices[0] = b2TransformPoint(ref xf, new B2Vec2(-halfWidth, -halfHeight));
            shape.vertices[1] = b2TransformPoint(ref xf, new B2Vec2(halfWidth, -halfHeight));
            shape.vertices[2] = b2TransformPoint(ref xf, new B2Vec2(halfWidth, halfHeight));
            shape.vertices[3] = b2TransformPoint(ref xf, new B2Vec2(-halfWidth, halfHeight));
            shape.normals[0] = b2RotateVector(xf.q, new B2Vec2(0.0f, -1.0f));
            shape.normals[1] = b2RotateVector(xf.q, new B2Vec2(1.0f, 0.0f));
            shape.normals[2] = b2RotateVector(xf.q, new B2Vec2(0.0f, 1.0f));
            shape.normals[3] = b2RotateVector(xf.q, new B2Vec2(-1.0f, 0.0f));
            shape.radius = 0.0f;
            shape.centroid = xf.p;
            return shape;
        }

        /// Make an offset rounded box, bypassing the need for a convex hull.
        /// @param halfWidth the half-width (x-axis)
        /// @param halfHeight the half-height (y-axis)
        /// @param center the local center of the box
        /// @param rotation the local rotation of the box
        /// @param radius the radius of the rounded extension
        public static B2Polygon b2MakeOffsetRoundedBox(float halfWidth, float halfHeight, B2Vec2 center, B2Rot rotation, float radius)
        {
            B2_ASSERT(b2IsValidFloat(radius) && radius >= 0.0f);
            B2Transform xf = new B2Transform(center, rotation);

            B2Polygon shape = new B2Polygon();
            shape.count = 4;
            shape.vertices[0] = b2TransformPoint(ref xf, new B2Vec2(-halfWidth, -halfHeight));
            shape.vertices[1] = b2TransformPoint(ref xf, new B2Vec2(halfWidth, -halfHeight));
            shape.vertices[2] = b2TransformPoint(ref xf, new B2Vec2(halfWidth, halfHeight));
            shape.vertices[3] = b2TransformPoint(ref xf, new B2Vec2(-halfWidth, halfHeight));
            shape.normals[0] = b2RotateVector(xf.q, new B2Vec2(0.0f, -1.0f));
            shape.normals[1] = b2RotateVector(xf.q, new B2Vec2(1.0f, 0.0f));
            shape.normals[2] = b2RotateVector(xf.q, new B2Vec2(0.0f, 1.0f));
            shape.normals[3] = b2RotateVector(xf.q, new B2Vec2(-1.0f, 0.0f));
            shape.radius = radius;
            shape.centroid = xf.p;
            return shape;
        }

        /// Transform a polygon. This is useful for transferring a shape from one body to another.
        public static B2Polygon b2TransformPolygon(B2Transform transform, ref B2Polygon polygon)
        {
            B2Polygon p = polygon;

            for (int i = 0; i < p.count; ++i)
            {
                p.vertices[i] = b2TransformPoint(ref transform, p.vertices[i]);
                p.normals[i] = b2RotateVector(transform.q, p.normals[i]);
            }

            p.centroid = b2TransformPoint(ref transform, p.centroid);

            return p;
        }

        /// Compute mass properties of a circle
        public static B2MassData b2ComputeCircleMass(ref B2Circle shape, float density)
        {
            float rr = shape.radius * shape.radius;

            B2MassData massData = new B2MassData();
            massData.mass = density * B2_PI * rr;
            massData.center = shape.center;

            // inertia about the center of mass
            massData.rotationalInertia = massData.mass * 0.5f * rr;

            return massData;
        }

        /// Compute mass properties of a capsule
        public static B2MassData b2ComputeCapsuleMass(ref B2Capsule shape, float density)
        {
            float radius = shape.radius;
            float rr = radius * radius;
            B2Vec2 p1 = shape.center1;
            B2Vec2 p2 = shape.center2;
            float length = b2Length(b2Sub(p2, p1));
            float ll = length * length;

            float circleMass = density * (B2_PI * radius * radius);
            float boxMass = density * (2.0f * radius * length);

            B2MassData massData = new B2MassData();
            massData.mass = circleMass + boxMass;
            massData.center.X = 0.5f * (p1.X + p2.X);
            massData.center.Y = 0.5f * (p1.Y + p2.Y);

            // two offset half circles, both halves add up to full circle and each half is offset by half length
            // semi-circle centroid = 4 r / 3 pi
            // Need to apply parallel-axis theorem twice:
            // 1. shift semi-circle centroid to origin
            // 2. shift semi-circle to box end
            // m * ((h + lc)^2 - lc^2) = m * (h^2 + 2 * h * lc)
            // See: https://en.wikipedia.org/wiki/Parallel_axis_theorem
            // I verified this formula by computing the convex hull of a 128 vertex capsule

            // half circle centroid
            float lc = 4.0f * radius / (3.0f * B2_PI);

            // half length of rectangular portion of capsule
            float h = 0.5f * length;

            float circleInertia = circleMass * (0.5f * rr + h * h + 2.0f * h * lc);
            float boxInertia = boxMass * (4.0f * rr + ll) / 12.0f;
            massData.rotationalInertia = circleInertia + boxInertia;

            return massData;
        }

        /// Compute mass properties of a polygon
        public static B2MassData b2ComputePolygonMass(ref B2Polygon shape, float density)
        {
            // Polygon mass, centroid, and inertia.
            // Let rho be the polygon density in mass per unit area.
            // Then:
            // mass = rho * int(dA)
            // centroid.x = (1/mass) * rho * int(x * dA)
            // centroid.y = (1/mass) * rho * int(y * dA)
            // I = rho * int((x*x + y*y) * dA)
            //
            // We can compute these integrals by summing all the integrals
            // for each triangle of the polygon. To evaluate the integral
            // for a single triangle, we make a change of variables to
            // the (u,v) coordinates of the triangle:
            // x = x0 + e1x * u + e2x * v
            // y = y0 + e1y * u + e2y * v
            // where 0 <= u && 0 <= v && u + v <= 1.
            //
            // We integrate u from [0,1-v] and then v from [0,1].
            // We also need to use the Jacobian of the transformation:
            // D = cross(e1, e2)
            //
            // Simplification: triangle centroid = (1/3) * (p1 + p2 + p3)
            //
            // The rest of the derivation is handled by computer algebra.

            B2_ASSERT(shape.count > 0);

            if (shape.count == 1)
            {
                B2Circle circle = new B2Circle(shape.vertices[0], shape.radius);
                return b2ComputeCircleMass(ref circle, density);
            }

            if (shape.count == 2)
            {
                B2Capsule capsule = new B2Capsule(shape.vertices[0], shape.vertices[1], shape.radius);
                // capsule.center1 = shape.vertices[0];
                // capsule.center2 = shape.vertices[1];
                // capsule.radius = shape.radius;
                return b2ComputeCapsuleMass(ref capsule, density);
            }

            Span<B2Vec2> vertices = stackalloc B2Vec2[B2_MAX_POLYGON_VERTICES];
            int count = shape.count;
            float radius = shape.radius;

            if (radius > 0.0f)
            {
                // Approximate mass of rounded polygons by pushing out the vertices.
                float sqrt2 = 1.412f;
                for (int i = 0; i < count; ++i)
                {
                    int j = i == 0 ? count - 1 : i - 1;
                    B2Vec2 n1 = shape.normals[j];
                    B2Vec2 n2 = shape.normals[i];

                    B2Vec2 mid = b2Normalize(b2Add(n1, n2));
                    vertices[i] = b2MulAdd(shape.vertices[i], sqrt2 * radius, mid);
                }
            }
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    vertices[i] = shape.vertices[i];
                }
            }

            B2Vec2 center = new B2Vec2(0.0f, 0.0f);
            float area = 0.0f;
            float rotationalInertia = 0.0f;

            // Get a reference point for forming triangles.
            // Use the first vertex to reduce round-off errors.
            B2Vec2 r = vertices[0];

            const float inv3 = 1.0f / 3.0f;

            for (int i = 1; i < count - 1; ++i)
            {
                // Triangle edges
                B2Vec2 e1 = b2Sub(vertices[i], r);
                B2Vec2 e2 = b2Sub(vertices[i + 1], r);

                float D = b2Cross(e1, e2);

                float triangleArea = 0.5f * D;
                area += triangleArea;

                // Area weighted centroid, r at origin
                center = b2MulAdd(center, triangleArea * inv3, b2Add(e1, e2));

                float ex1 = e1.X, ey1 = e1.Y;
                float ex2 = e2.X, ey2 = e2.Y;

                float intx2 = ex1 * ex1 + ex2 * ex1 + ex2 * ex2;
                float inty2 = ey1 * ey1 + ey2 * ey1 + ey2 * ey2;

                rotationalInertia += (0.25f * inv3 * D) * (intx2 + inty2);
            }

            B2MassData massData = new B2MassData();

            // Total mass
            massData.mass = density * area;

            // Center of mass, shift back from origin at r
            B2_ASSERT(area > FLT_EPSILON);
            float invArea = 1.0f / area;
            center.X *= invArea;
            center.Y *= invArea;
            massData.center = b2Add(r, center);

            // Inertia tensor relative to the local origin (point s).
            massData.rotationalInertia = density * rotationalInertia;

            // Shift inertia to center of mass
            massData.rotationalInertia -= massData.mass * b2Dot(center, center);

            // If this goes negative we are hosed
            B2_ASSERT(massData.rotationalInertia >= 0.0f);

            return massData;
        }

        /// Compute the bounding box of a transformed circle
        public static B2AABB b2ComputeCircleAABB(ref B2Circle shape, B2Transform xf)
        {
            B2Vec2 p = b2TransformPoint(ref xf, shape.center);
            float r = shape.radius;

            B2AABB aabb = new B2AABB(new B2Vec2(p.X - r, p.Y - r), new B2Vec2(p.X + r, p.Y + r));
            return aabb;
        }

        /// Compute the bounding box of a transformed capsule
        public static B2AABB b2ComputeCapsuleAABB(ref B2Capsule shape, B2Transform xf)
        {
            B2Vec2 v1 = b2TransformPoint(ref xf, shape.center1);
            B2Vec2 v2 = b2TransformPoint(ref xf, shape.center2);

            B2Vec2 r = new B2Vec2(shape.radius, shape.radius);
            B2Vec2 lower = b2Sub(b2Min(v1, v2), r);
            B2Vec2 upper = b2Add(b2Max(v1, v2), r);

            B2AABB aabb = new B2AABB(lower, upper);
            return aabb;
        }

        /// Compute the bounding box of a transformed polygon
        public static B2AABB b2ComputePolygonAABB(ref B2Polygon shape, B2Transform xf)
        {
            B2_ASSERT(shape.count > 0);
            B2Vec2 lower = b2TransformPoint(ref xf, shape.vertices[0]);
            B2Vec2 upper = lower;

            for (int i = 1; i < shape.count; ++i)
            {
                B2Vec2 v = b2TransformPoint(ref xf, shape.vertices[i]);
                lower = b2Min(lower, v);
                upper = b2Max(upper, v);
            }

            B2Vec2 r = new B2Vec2(shape.radius, shape.radius);
            lower = b2Sub(lower, r);
            upper = b2Add(upper, r);

            B2AABB aabb = new B2AABB(lower, upper);
            return aabb;
        }

        /// Compute the bounding box of a transformed line segment
        public static B2AABB b2ComputeSegmentAABB(ref B2Segment shape, B2Transform xf)
        {
            B2Vec2 v1 = b2TransformPoint(ref xf, shape.point1);
            B2Vec2 v2 = b2TransformPoint(ref xf, shape.point2);

            B2Vec2 lower = b2Min(v1, v2);
            B2Vec2 upper = b2Max(v1, v2);

            B2AABB aabb = new B2AABB(lower, upper);
            return aabb;
        }

        /// Test a point for overlap with a circle in local space
        public static bool b2PointInCircle(ref B2Circle shape, B2Vec2 point)
        {
            B2Vec2 center = shape.center;
            return b2DistanceSquared(point, center) <= shape.radius * shape.radius;
        }

        /// Test a point for overlap with a capsule in local space
        public static bool b2PointInCapsule(ref B2Capsule shape, B2Vec2 point)
        {
            float rr = shape.radius * shape.radius;
            B2Vec2 p1 = shape.center1;
            B2Vec2 p2 = shape.center2;

            B2Vec2 d = b2Sub(p2, p1);
            float dd = b2Dot(d, d);
            if (dd == 0.0f)
            {
                // Capsule is really a circle
                return b2DistanceSquared(point, p1) <= rr;
            }

            // Get closest point on capsule segment
            // c = p1 + t * d
            // dot(point - c, d) = 0
            // dot(point - p1 - t * d, d) = 0
            // t = dot(point - p1, d) / dot(d, d)
            float t = b2Dot(b2Sub(point, p1), d) / dd;
            t = b2ClampFloat(t, 0.0f, 1.0f);
            B2Vec2 c = b2MulAdd(p1, t, d);

            // Is query point within radius around closest point?
            return b2DistanceSquared(point, c) <= rr;
        }

        /// Test a point for overlap with a convex polygon in local space
        public static bool b2PointInPolygon(ref B2Polygon shape, B2Vec2 point)
        {
            B2DistanceInput input = new B2DistanceInput();
            input.proxyA = b2MakeProxy(shape.vertices.AsSpan(), shape.count, 0.0f);
            input.proxyB = b2MakeProxy(point, 1, 0.0f);
            input.transformA = b2Transform_identity;
            input.transformB = b2Transform_identity;
            input.useRadii = false;

            B2SimplexCache cache = new B2SimplexCache();
            B2DistanceOutput output = b2ShapeDistance(ref input, ref cache, null, 0);

            return output.distance <= shape.radius;
        }

        /// Ray cast versus circle shape in local space.
        // Precision Improvements for Ray / Sphere Intersection - Ray Tracing Gems 2019
        // http://www.codercorner.com/blog/?p=321
        public static B2CastOutput b2RayCastCircle(ref B2Circle shape, ref B2RayCastInput input)
        {
            B2_ASSERT(b2IsValidRay(ref input));

            B2Vec2 p = shape.center;

            B2CastOutput output = new B2CastOutput();

            // Shift ray so circle center is the origin
            B2Vec2 s = b2Sub(input.origin, p);

            float r = shape.radius;
            float rr = r * r;

            float length = 0;
            B2Vec2 d = b2GetLengthAndNormalize(ref length, input.translation);
            if (length == 0.0f)
            {
                // zero length ray

                if (b2LengthSquared(s) < rr)
                {
                    // initial overlap
                    output.point = input.origin;
                    output.hit = true;
                }

                return output;
            }

            // Find closest point on ray to origin

            // solve: dot(s + t * d, d) = 0
            float t = -b2Dot(s, d);

            // c is the closest point on the line to the origin
            B2Vec2 c = b2MulAdd(s, t, d);

            float cc = b2Dot(c, c);

            if (cc > rr)
            {
                // closest point is outside the circle
                return output;
            }

            // Pythagoras
            float h = MathF.Sqrt(rr - cc);

            float fraction = t - h;

            if (fraction < 0.0f || input.maxFraction * length < fraction)
            {
                // intersection is point outside the range of the ray segment

                if (b2LengthSquared(s) < rr)
                {
                    // initial overlap
                    output.point = input.origin;
                    output.hit = true;
                }

                return output;
            }

            // hit point relative to center
            B2Vec2 hitPoint = b2MulAdd(s, fraction, d);

            output.fraction = fraction / length;
            output.normal = b2Normalize(hitPoint);
            output.point = b2MulAdd(p, shape.radius, output.normal);
            output.hit = true;

            return output;
        }

        /// Ray cast versus capsule shape in local space.
        public static B2CastOutput b2RayCastCapsule(ref B2Capsule shape, ref B2RayCastInput input)
        {
            B2_ASSERT(b2IsValidRay(ref input));

            B2CastOutput output = new B2CastOutput();

            B2Vec2 v1 = shape.center1;
            B2Vec2 v2 = shape.center2;

            B2Vec2 e = b2Sub(v2, v1);

            float capsuleLength = 0;
            B2Vec2 a = b2GetLengthAndNormalize(ref capsuleLength, e);

            if (capsuleLength < FLT_EPSILON)
            {
                // Capsule is really a circle
                B2Circle circle = new B2Circle(v1, shape.radius);
                return b2RayCastCircle(ref circle, ref input);
            }

            B2Vec2 p1 = input.origin;
            B2Vec2 d = input.translation;

            // Ray from capsule start to ray start
            B2Vec2 q = b2Sub(p1, v1);
            float qa = b2Dot(q, a);

            // Vector to ray start that is perpendicular to capsule axis
            B2Vec2 qp = b2MulAdd(q, -qa, a);

            float radius = shape.radius;

            // Does the ray start within the infinite length capsule?
            if (b2Dot(qp, qp) < radius * radius)
            {
                if (qa < 0.0f)
                {
                    // start point behind capsule segment
                    B2Circle circle = new B2Circle(v1, shape.radius);
                    return b2RayCastCircle(ref circle, ref input);
                }

                if (qa > capsuleLength)
                {
                    // start point ahead of capsule segment
                    B2Circle circle = new B2Circle(v2, shape.radius);
                    return b2RayCastCircle(ref circle, ref input);
                }

                // ray starts inside capsule . no hit
                output.point = input.origin;
                output.hit = true;
                return output;
            }

            // Perpendicular to capsule axis, pointing right
            B2Vec2 n = new B2Vec2(a.Y, -a.X);

            float rayLength = 0;
            B2Vec2 u = b2GetLengthAndNormalize(ref rayLength, d);

            // Intersect ray with infinite length capsule
            // v1 + radius * n + s1 * a = p1 + s2 * u
            // v1 - radius * n + s1 * a = p1 + s2 * u

            // s1 * a - s2 * u = b
            // b = q - radius * ap
            // or
            // b = q + radius * ap

            // Cramer's rule [a -u]
            float den = -a.X * u.Y + u.X * a.Y;
            if (-FLT_EPSILON < den && den < FLT_EPSILON)
            {
                // Ray is parallel to capsule and outside infinite length capsule
                return output;
            }

            B2Vec2 b1 = b2MulSub(q, radius, n);
            B2Vec2 b2 = b2MulAdd(q, radius, n);

            float invDen = 1.0f / den;

            // Cramer's rule [a b1]
            float s21 = (a.X * b1.Y - b1.X * a.Y) * invDen;

            // Cramer's rule [a b2]
            float s22 = (a.X * b2.Y - b2.X * a.Y) * invDen;

            float s2;
            B2Vec2 b;
            if (s21 < s22)
            {
                s2 = s21;
                b = b1;
            }
            else
            {
                s2 = s22;
                b = b2;
                n = b2Neg(n);
            }

            if (s2 < 0.0f || input.maxFraction * rayLength < s2)
            {
                return output;
            }

            // Cramer's rule [b -u]
            float s1 = (-b.X * u.Y + u.X * b.Y) * invDen;

            if (s1 < 0.0f)
            {
                // ray passes behind capsule segment
                B2Circle circle = new B2Circle(v1, shape.radius);
                return b2RayCastCircle(ref circle, ref input);
            }
            else if (capsuleLength < s1)
            {
                // ray passes ahead of capsule segment
                B2Circle circle = new B2Circle(v2, shape.radius);
                return b2RayCastCircle(ref circle, ref input);
            }
            else
            {
                // ray hits capsule side
                output.fraction = s2 / rayLength;
                output.point = b2Add(b2Lerp(v1, v2, s1 / capsuleLength), b2MulSV(shape.radius, n));
                output.normal = n;
                output.hit = true;
                return output;
            }
        }

        /// Ray cast versus segment shape in local space. Optionally treat the segment as one-sided with hits from
        /// the left side being treated as a miss.
        // Ray vs line segment
        public static B2CastOutput b2RayCastSegment(ref B2Segment shape, ref B2RayCastInput input, bool oneSided)
        {
            if (oneSided)
            {
                // Skip left-side collision
                float offset = b2Cross(b2Sub(input.origin, shape.point1), b2Sub(shape.point2, shape.point1));
                if (offset < 0.0f)
                {
                    B2CastOutput output1 = new B2CastOutput();
                    return output1;
                }
            }

            // Put the ray into the edge's frame of reference.
            B2Vec2 p1 = input.origin;
            B2Vec2 d = input.translation;

            B2Vec2 v1 = shape.point1;
            B2Vec2 v2 = shape.point2;
            B2Vec2 e = b2Sub(v2, v1);

            B2CastOutput output = new B2CastOutput();

            float length = 0;
            B2Vec2 eUnit = b2GetLengthAndNormalize(ref length, e);
            if (length == 0.0f)
            {
                return output;
            }

            // Normal points to the right, looking from v1 towards v2
            B2Vec2 normal = b2RightPerp(eUnit);

            // Intersect ray with infinite segment using normal
            // Similar to intersecting a ray with an infinite plane
            // p = p1 + t * d
            // dot(normal, p - v1) = 0
            // dot(normal, p1 - v1) + t * dot(normal, d) = 0
            float numerator = b2Dot(normal, b2Sub(v1, p1));
            float denominator = b2Dot(normal, d);

            if (denominator == 0.0f)
            {
                // parallel
                return output;
            }

            float t = numerator / denominator;
            if (t < 0.0f || input.maxFraction < t)
            {
                // out of ray range
                return output;
            }

            // Intersection point on infinite segment
            B2Vec2 p = b2MulAdd(p1, t, d);

            // Compute position of p along segment
            // p = v1 + s * e
            // s = dot(p - v1, e) / dot(e, e)

            float s = b2Dot(b2Sub(p, v1), eUnit);
            if (s < 0.0f || length < s)
            {
                // out of segment range
                return output;
            }

            if (numerator > 0.0f)
            {
                normal = b2Neg(normal);
            }

            output.fraction = t;
            output.point = p;
            output.normal = normal;
            output.hit = true;

            return output;
        }

        /// Ray cast versus polygon shape in local space.
        public static B2CastOutput b2RayCastPolygon(ref B2Polygon shape, ref B2RayCastInput input)
        {
            B2_ASSERT(b2IsValidRay(ref input));

            if (shape.radius == 0.0f)
            {
                // Shift all math to first vertex since the polygon may be far
                // from the origin.
                B2Vec2 @base = shape.vertices[0];

                B2Vec2 p1 = b2Sub(input.origin, @base);
                B2Vec2 d = input.translation;

                float lower = 0.0f, upper = input.maxFraction;

                int index = -1;

                B2CastOutput output = new B2CastOutput();

                for (int edgeIndex = 0; edgeIndex < shape.count; ++edgeIndex)
                {
                    // p = p1 + a * d
                    // dot(normal, p - v) = 0
                    // dot(normal, p1 - v) + a * dot(normal, d) = 0
                    B2Vec2 vertex = b2Sub(shape.vertices[edgeIndex], @base);
                    float numerator = b2Dot(shape.normals[edgeIndex], b2Sub(vertex, p1));
                    float denominator = b2Dot(shape.normals[edgeIndex], d);

                    if (denominator == 0.0f)
                    {
                        // Parallel and runs outside edge
                        if (numerator < 0.0f)
                        {
                            return output;
                        }
                    }
                    else
                    {
                        // Note: we want this predicate without division:
                        // lower < numerator / denominator, where denominator < 0
                        // Since denominator < 0, we have to flip the inequality:
                        // lower < numerator / denominator <==> denominator * lower > numerator.
                        if (denominator < 0.0f && numerator < lower * denominator)
                        {
                            // Increase lower.
                            // The segment enters this half-space.
                            lower = numerator / denominator;
                            index = edgeIndex;
                        }
                        else if (denominator > 0.0f && numerator < upper * denominator)
                        {
                            // Decrease upper.
                            // The segment exits this half-space.
                            upper = numerator / denominator;
                        }
                    }

                    if (upper < lower)
                    {
                        // Ray misses
                        return output;
                    }
                }

                B2_ASSERT(0.0f <= lower && lower <= input.maxFraction);

                if (index >= 0)
                {
                    output.fraction = lower;
                    output.normal = shape.normals[index];
                    output.point = b2MulAdd(input.origin, lower, d);
                    output.hit = true;
                }
                else
                {
                    // initial overlap
                    output.point = input.origin;
                    output.hit = true;
                }

                return output;
            }

            B2ShapeCastPairInput castInput = new B2ShapeCastPairInput();
            castInput.proxyA = b2MakeProxy(shape.vertices.AsSpan(), shape.count, shape.radius);
            castInput.proxyB = b2MakeProxy(input.origin, 1, 0.0f);
            castInput.transformA = b2Transform_identity;
            castInput.transformB = b2Transform_identity;
            castInput.translationB = input.translation;
            castInput.maxFraction = input.maxFraction;
            castInput.canEncroach = false;
            return b2ShapeCast(ref castInput);
        }

        /// Shape cast versus a circle.
        public static B2CastOutput b2ShapeCastCircle(ref B2Circle shape, ref B2ShapeCastInput input)
        {
            B2ShapeCastPairInput pairInput = new B2ShapeCastPairInput();
            pairInput.proxyA = b2MakeProxy(shape.center, 1, shape.radius);
            pairInput.proxyB = input.proxy;
            pairInput.transformA = b2Transform_identity;
            pairInput.transformB = b2Transform_identity;
            pairInput.translationB = input.translation;
            pairInput.maxFraction = input.maxFraction;
            pairInput.canEncroach = input.canEncroach;

            B2CastOutput output = b2ShapeCast(ref pairInput);
            return output;
        }

        /// Shape cast versus a capsule.
        public static B2CastOutput b2ShapeCastCapsule(ref B2Capsule shape, ref B2ShapeCastInput input)
        {
            B2ShapeCastPairInput pairInput = new B2ShapeCastPairInput();
            pairInput.proxyA = b2MakeProxy(shape.center1, shape.center2, 2, shape.radius);
            pairInput.proxyB = input.proxy;
            pairInput.transformA = b2Transform_identity;
            pairInput.transformB = b2Transform_identity;
            pairInput.translationB = input.translation;
            pairInput.maxFraction = input.maxFraction;
            pairInput.canEncroach = input.canEncroach;

            B2CastOutput output = b2ShapeCast(ref pairInput);
            return output;
        }

        /// Shape cast versus a line segment.
        public static B2CastOutput b2ShapeCastSegment(ref B2Segment shape, ref B2ShapeCastInput input)
        {
            B2ShapeCastPairInput pairInput = new B2ShapeCastPairInput();
            pairInput.proxyA = b2MakeProxy(shape.point1, shape.point2, 2, 0.0f);
            pairInput.proxyB = input.proxy;
            pairInput.transformA = b2Transform_identity;
            pairInput.transformB = b2Transform_identity;
            pairInput.translationB = input.translation;
            pairInput.maxFraction = input.maxFraction;
            pairInput.canEncroach = input.canEncroach;

            B2CastOutput output = b2ShapeCast(ref pairInput);
            return output;
        }

        /// Shape cast versus a convex polygon.
        public static B2CastOutput b2ShapeCastPolygon(ref B2Polygon shape, ref B2ShapeCastInput input)
        {
            B2ShapeCastPairInput pairInput = new B2ShapeCastPairInput();
            pairInput.proxyA = b2MakeProxy(shape.vertices.AsSpan(), shape.count, shape.radius);
            pairInput.proxyB = input.proxy;
            pairInput.transformA = b2Transform_identity;
            pairInput.transformB = b2Transform_identity;
            pairInput.translationB = input.translation;
            pairInput.maxFraction = input.maxFraction;
            pairInput.canEncroach = input.canEncroach;

            B2CastOutput output = b2ShapeCast(ref pairInput);
            return output;
        }

        public static B2PlaneResult b2CollideMoverAndCircle(ref B2Capsule mover, ref B2Circle shape)
        {
            B2DistanceInput distanceInput = new B2DistanceInput();
            distanceInput.proxyA = b2MakeProxy(shape.center, 1, 0.0f);
            distanceInput.proxyB = b2MakeProxy(mover.center1, mover.center2, 2, mover.radius);
            distanceInput.transformA = b2Transform_identity;
            distanceInput.transformB = b2Transform_identity;
            distanceInput.useRadii = false;

            float totalRadius = mover.radius + shape.radius;

            B2SimplexCache cache = new B2SimplexCache();
            B2DistanceOutput distanceOutput = b2ShapeDistance(ref distanceInput, ref cache, null, 0);

            if (distanceOutput.distance <= totalRadius)
            {
                B2Plane plane = new B2Plane(distanceOutput.normal, totalRadius - distanceOutput.distance);
                return new B2PlaneResult(plane, distanceOutput.pointA, true);
            }

            return new B2PlaneResult();
        }

        public static B2PlaneResult b2CollideMoverAndCapsule(ref B2Capsule mover, ref B2Capsule shape)
        {
            B2DistanceInput distanceInput = new B2DistanceInput();
            distanceInput.proxyA = b2MakeProxy(shape.center1, shape.center2, 2, 0.0f);
            distanceInput.proxyB = b2MakeProxy(mover.center1, mover.center2, 2, mover.radius);
            distanceInput.transformA = b2Transform_identity;
            distanceInput.transformB = b2Transform_identity;
            distanceInput.useRadii = false;

            float totalRadius = mover.radius + shape.radius;

            B2SimplexCache cache = new B2SimplexCache();
            B2DistanceOutput distanceOutput = b2ShapeDistance(ref distanceInput, ref cache, null, 0);

            if (distanceOutput.distance <= totalRadius)
            {
                B2Plane plane = new B2Plane(distanceOutput.normal, totalRadius - distanceOutput.distance);
                return new B2PlaneResult(plane, distanceOutput.pointA, true);
            }

            return new B2PlaneResult();
        }

        public static B2PlaneResult b2CollideMoverAndPolygon(ref B2Capsule mover, ref B2Polygon shape)
        {
            B2DistanceInput distanceInput = new B2DistanceInput();
            distanceInput.proxyA = b2MakeProxy(shape.vertices.AsSpan(), shape.count, shape.radius);
            distanceInput.proxyB = b2MakeProxy(mover.center1, mover.center2, 2, mover.radius);
            distanceInput.transformA = b2Transform_identity;
            distanceInput.transformB = b2Transform_identity;
            distanceInput.useRadii = false;

            float totalRadius = mover.radius + shape.radius;

            B2SimplexCache cache = new B2SimplexCache();
            B2DistanceOutput distanceOutput = b2ShapeDistance(ref distanceInput, ref cache, null, 0);

            if (distanceOutput.distance <= totalRadius)
            {
                B2Plane plane = new B2Plane(distanceOutput.normal, totalRadius - distanceOutput.distance);
                return new B2PlaneResult(plane, distanceOutput.pointA, true);
            }

            return new B2PlaneResult();
        }

        public static B2PlaneResult b2CollideMoverAndSegment(ref B2Capsule mover, ref B2Segment shape)
        {
            B2DistanceInput distanceInput = new B2DistanceInput();
            distanceInput.proxyA = b2MakeProxy(shape.point1, shape.point2, 2, 0.0f);
            distanceInput.proxyB = b2MakeProxy(mover.center1, mover.center2, 2, mover.radius);
            distanceInput.transformA = b2Transform_identity;
            distanceInput.transformB = b2Transform_identity;
            distanceInput.useRadii = false;

            float totalRadius = mover.radius;

            B2SimplexCache cache = new B2SimplexCache();
            B2DistanceOutput distanceOutput = b2ShapeDistance(ref distanceInput, ref cache, null, 0);

            if (distanceOutput.distance <= totalRadius)
            {
                B2Plane plane = new B2Plane(distanceOutput.normal, totalRadius - distanceOutput.distance);
                return new B2PlaneResult(plane, distanceOutput.pointA, true);
            }

            return new B2PlaneResult();
        }
    }
}