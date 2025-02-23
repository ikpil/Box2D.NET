// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.math_function;
using static Box2D.NET.constants;

namespace Box2D.NET
{
    public static class distance
    {
        // Warning: writing to these globals significantly slows multithreading performance
#if B2_SNOOP_TOI_COUNTERS
        public static float b2_toiTime, b2_toiMaxTime;
        public static int b2_toiCalls, b2_toiDistanceIterations, b2_toiMaxDistanceIterations;
        public static int b2_toiRootIterations, b2_toiMaxRootIterations;
        public static int b2_toiFailedCount;
        public static int b2_toiOverlappedCount;
        public static int b2_toiHitCount;
        public static int b2_toiSeparatedCount;
#endif
        
        /// Evaluate the transform sweep at a specific time.
        public static b2Transform b2GetSweepTransform(b2Sweep sweep, float time)
        {
            // https://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
            b2Transform xf;
            xf.p = b2Add(b2MulSV(1.0f - time, sweep.c1), b2MulSV(time, sweep.c2));

            b2Rot q = new b2Rot(
                (1.0f - time) * sweep.q1.c + time * sweep.q2.c,
                (1.0f - time) * sweep.q1.s + time * sweep.q2.s
            );

            xf.q = b2NormalizeRot(q);

            // Shift to origin
            xf.p = b2Sub(xf.p, b2RotateVector(xf.q, sweep.localCenter));
            return xf;
        }

        /// Compute the distance between two line segments, clamping at the end points if needed.
        /// Follows Ericson 5.1.9 Closest Points of Two Line Segments
        public static b2SegmentDistanceResult b2SegmentDistance(b2Vec2 p1, b2Vec2 q1, b2Vec2 p2, b2Vec2 q2)
        {
            b2SegmentDistanceResult result = new b2SegmentDistanceResult();

            b2Vec2 d1 = b2Sub(q1, p1);
            b2Vec2 d2 = b2Sub(q2, p2);
            b2Vec2 r = b2Sub(p1, p2);
            float dd1 = b2Dot(d1, d1);
            float dd2 = b2Dot(d2, d2);
            float rd1 = b2Dot(r, d1);
            float rd2 = b2Dot(r, d2);

            const float epsSqr = FLT_EPSILON * FLT_EPSILON;

            if (dd1 < epsSqr || dd2 < epsSqr)
            {
                // Handle all degeneracies
                if (dd1 >= epsSqr)
                {
                    // Segment 2 is degenerate
                    result.fraction1 = b2ClampFloat(-rd1 / dd1, 0.0f, 1.0f);
                    result.fraction2 = 0.0f;
                }
                else if (dd2 >= epsSqr)
                {
                    // Segment 1 is degenerate
                    result.fraction1 = 0.0f;
                    result.fraction2 = b2ClampFloat(rd2 / dd2, 0.0f, 1.0f);
                }
                else
                {
                    result.fraction1 = 0.0f;
                    result.fraction2 = 0.0f;
                }
            }
            else
            {
                // Non-degenerate segments
                float d12 = b2Dot(d1, d2);

                float denom = dd1 * dd2 - d12 * d12;

                // Fraction on segment 1
                float f1 = 0.0f;
                if (denom != 0.0f)
                {
                    // not parallel
                    f1 = b2ClampFloat((d12 * rd2 - rd1 * dd2) / denom, 0.0f, 1.0f);
                }

                // Compute point on segment 2 closest to p1 + f1 * d1
                float f2 = (d12 * f1 + rd2) / dd2;

                // Clamping of segment 2 requires a do over on segment 1
                if (f2 < 0.0f)
                {
                    f2 = 0.0f;
                    f1 = b2ClampFloat(-rd1 / dd1, 0.0f, 1.0f);
                }
                else if (f2 > 1.0f)
                {
                    f2 = 1.0f;
                    f1 = b2ClampFloat((d12 - rd1) / dd1, 0.0f, 1.0f);
                }

                result.fraction1 = f1;
                result.fraction2 = f2;
            }

            result.closest1 = b2MulAdd(p1, result.fraction1, d1);
            result.closest2 = b2MulAdd(p2, result.fraction2, d2);
            result.distanceSquared = b2DistanceSquared(result.closest1, result.closest2);
            return result;
        }

        /// Make a proxy for use in GJK and related functions.
        // GJK using Voronoi regions (Christer Ericson) and Barycentric coordinates.
        // todo try not copying
        public static b2ShapeProxy b2MakeProxy(ReadOnlySpan<b2Vec2> vertices, int count, float radius)
        {
            count = b2MinInt(count, B2_MAX_POLYGON_VERTICES);
            b2ShapeProxy proxy = new b2ShapeProxy();
            for (int i = 0; i < count; ++i)
            {
                proxy.points[i] = vertices[i];
            }

            proxy.count = count;
            proxy.radius = radius;
            return proxy;
        }

        // for single
        public static b2ShapeProxy b2MakeProxy(b2Vec2 v1, int count, float radius)
        {
            Debug.Assert(count == 1);

            Span<b2Vec2> vertices = stackalloc b2Vec2[1];
            vertices[0] = v1;
            return b2MakeProxy(vertices, count, radius);
        }

        public static b2ShapeProxy b2MakeProxy(b2Vec2 v1, b2Vec2 v2, int count, float radius)
        {
            Debug.Assert(count == 2);

            Span<b2Vec2> vertices = stackalloc b2Vec2[2];
            vertices[0] = v1;
            vertices[1] = v2;
            return b2MakeProxy(vertices, count, radius);
        }


        public static b2Vec2 b2Weight2(float a1, b2Vec2 w1, float a2, b2Vec2 w2)
        {
            return new b2Vec2(a1 * w1.x + a2 * w2.x, a1 * w1.y + a2 * w2.y);
        }

        public static b2Vec2 b2Weight3(float a1, b2Vec2 w1, float a2, b2Vec2 w2, float a3, b2Vec2 w3)
        {
            return new b2Vec2(a1 * w1.x + a2 * w2.x + a3 * w3.x, a1 * w1.y + a2 * w2.y + a3 * w3.y);
        }

        public static int b2FindSupport(ref b2ShapeProxy proxy, b2Vec2 direction)
        {
            int bestIndex = 0;
            float bestValue = b2Dot(proxy.points[0], direction);
            for (int i = 1; i < proxy.count; ++i)
            {
                float value = b2Dot(proxy.points[i], direction);
                if (value > bestValue)
                {
                    bestIndex = i;
                    bestValue = value;
                }
            }

            return bestIndex;
        }

        static b2Simplex b2MakeSimplexFromCache(ref b2SimplexCache cache, ref b2ShapeProxy proxyA, b2Transform transformA, ref b2ShapeProxy proxyB, b2Transform transformB)
        {
            Debug.Assert(cache.count <= 3);
            b2Simplex s = new b2Simplex();

            // Copy data from cache.
            s.count = cache.count;

            b2SimplexVertex[] vertices = { s.v1, s.v2, s.v3 };
            for (int i = 0; i < s.count; ++i)
            {
                b2SimplexVertex v = vertices[i];
                v.indexA = cache.indexA[i];
                v.indexB = cache.indexB[i];
                b2Vec2 wALocal = proxyA.points[v.indexA];
                b2Vec2 wBLocal = proxyB.points[v.indexB];
                v.wA = b2TransformPoint(transformA, wALocal);
                v.wB = b2TransformPoint(transformB, wBLocal);
                v.w = b2Sub(v.wB, v.wA);

                // invalid
                v.a = -1.0f;
            }

            // If the cache is empty or invalid ...
            if (s.count == 0)
            {
                b2SimplexVertex v = vertices[0];
                v.indexA = 0;
                v.indexB = 0;
                b2Vec2 wALocal = proxyA.points[0];
                b2Vec2 wBLocal = proxyB.points[0];
                v.wA = b2TransformPoint(transformA, wALocal);
                v.wB = b2TransformPoint(transformB, wBLocal);
                v.w = b2Sub(v.wB, v.wA);
                v.a = 1.0f;
                s.count = 1;
            }

            return s;
        }

        public static void b2MakeSimplexCache(ref b2SimplexCache cache, b2Simplex simplex)
        {
            cache.count = (ushort)simplex.count;
            b2SimplexVertex[] vertices = { simplex.v1, simplex.v2, simplex.v3 };
            for (int i = 0; i < simplex.count; ++i)
            {
                cache.indexA[i] = (byte)vertices[i].indexA;
                cache.indexB[i] = (byte)vertices[i].indexB;
            }
        }

        // Compute the search direction from the current simplex.
        // This is the vector pointing from the closest point on the simplex
        // to the origin.
        // A more accurate search direction can be computed by using the normal
        // vector of the simplex. For example, the normal vector of a line segment
        // can be computed more accurately because it does not involve barycentric
        // coordinates.
        public static b2Vec2 b2ComputeSimplexSearchDirection(b2Simplex simplex)
        {
            switch (simplex.count)
            {
                case 1:
                    return b2Neg(simplex.v1.w);

                case 2:
                {
                    b2Vec2 e12 = b2Sub(simplex.v2.w, simplex.v1.w);
                    float sgn = b2Cross(e12, b2Neg(simplex.v1.w));
                    if (sgn > 0.0f)
                    {
                        // Origin is left of e12.
                        return b2LeftPerp(e12);
                    }
                    else
                    {
                        // Origin is right of e12.
                        return b2RightPerp(e12);
                    }
                }

                default:
                    Debug.Assert(false);
                    return b2Vec2_zero;
            }
        }

        public static b2Vec2 b2ComputeSimplexClosestPoint(b2Simplex s)
        {
            switch (s.count)
            {
                case 0:
                    Debug.Assert(false);
                    return b2Vec2_zero;

                case 1:
                    return s.v1.w;

                case 2:
                    return b2Weight2(s.v1.a, s.v1.w, s.v2.a, s.v2.w);

                case 3:
                    return b2Vec2_zero;

                default:
                    Debug.Assert(false);
                    return b2Vec2_zero;
            }
        }

        public static void b2ComputeSimplexWitnessPoints(ref b2Vec2 a, ref b2Vec2 b, b2Simplex s)
        {
            switch (s.count)
            {
                case 0:
                    Debug.Assert(false);
                    break;

                case 1:
                    a = s.v1.wA;
                    b = s.v1.wB;
                    break;

                case 2:
                    a = b2Weight2(s.v1.a, s.v1.wA, s.v2.a, s.v2.wA);
                    b = b2Weight2(s.v1.a, s.v1.wB, s.v2.a, s.v2.wB);
                    break;

                case 3:
                    a = b2Weight3(s.v1.a, s.v1.wA, s.v2.a, s.v2.wA, s.v3.a, s.v3.wA);
                    // TODO_ERIN why are these not equal?
                    //*b = b2Weight3(s.v1.a, s.v1.wB, s.v2.a, s.v2.wB, s.v3.a, s.v3.wB);
                    b = a;
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        // Solve a line segment using barycentric coordinates.
        //
        // p = a1 * w1 + a2 * w2
        // a1 + a2 = 1
        //
        // The vector from the origin to the closest point on the line is
        // perpendicular to the line.
        // e12 = w2 - w1
        // dot(p, e) = 0
        // a1 * dot(w1, e) + a2 * dot(w2, e) = 0
        //
        // 2-by-2 linear system
        // [1      1     ][a1] = [1]
        // [w1.e12 w2.e12][a2] = [0]
        //
        // Define
        // d12_1 =  dot(w2, e12)
        // d12_2 = -dot(w1, e12)
        // d12 = d12_1 + d12_2
        //
        // Solution
        // a1 = d12_1 / d12
        // a2 = d12_2 / d12
        public static void b2SolveSimplex2(b2Simplex s)
        {
            b2Vec2 w1 = s.v1.w;
            b2Vec2 w2 = s.v2.w;
            b2Vec2 e12 = b2Sub(w2, w1);

            // w1 region
            float d12_2 = -b2Dot(w1, e12);
            if (d12_2 <= 0.0f)
            {
                // a2 <= 0, so we clamp it to 0
                s.v1.a = 1.0f;
                s.count = 1;
                return;
            }

            // w2 region
            float d12_1 = b2Dot(w2, e12);
            if (d12_1 <= 0.0f)
            {
                // a1 <= 0, so we clamp it to 0
                s.v2.a = 1.0f;
                s.count = 1;
                s.v1 = s.v2;
                return;
            }

            // Must be in e12 region.
            float inv_d12 = 1.0f / (d12_1 + d12_2);
            s.v1.a = d12_1 * inv_d12;
            s.v2.a = d12_2 * inv_d12;
            s.count = 2;
        }

        public static void b2SolveSimplex3(b2Simplex s)
        {
            b2Vec2 w1 = s.v1.w;
            b2Vec2 w2 = s.v2.w;
            b2Vec2 w3 = s.v3.w;

            // Edge12
            // [1      1     ][a1] = [1]
            // [w1.e12 w2.e12][a2] = [0]
            // a3 = 0
            b2Vec2 e12 = b2Sub(w2, w1);
            float w1e12 = b2Dot(w1, e12);
            float w2e12 = b2Dot(w2, e12);
            float d12_1 = w2e12;
            float d12_2 = -w1e12;

            // Edge13
            // [1      1     ][a1] = [1]
            // [w1.e13 w3.e13][a3] = [0]
            // a2 = 0
            b2Vec2 e13 = b2Sub(w3, w1);
            float w1e13 = b2Dot(w1, e13);
            float w3e13 = b2Dot(w3, e13);
            float d13_1 = w3e13;
            float d13_2 = -w1e13;

            // Edge23
            // [1      1     ][a2] = [1]
            // [w2.e23 w3.e23][a3] = [0]
            // a1 = 0
            b2Vec2 e23 = b2Sub(w3, w2);
            float w2e23 = b2Dot(w2, e23);
            float w3e23 = b2Dot(w3, e23);
            float d23_1 = w3e23;
            float d23_2 = -w2e23;

            // Triangle123
            float n123 = b2Cross(e12, e13);

            float d123_1 = n123 * b2Cross(w2, w3);
            float d123_2 = n123 * b2Cross(w3, w1);
            float d123_3 = n123 * b2Cross(w1, w2);

            // w1 region
            if (d12_2 <= 0.0f && d13_2 <= 0.0f)
            {
                s.v1.a = 1.0f;
                s.count = 1;
                return;
            }

            // e12
            if (d12_1 > 0.0f && d12_2 > 0.0f && d123_3 <= 0.0f)
            {
                float inv_d12 = 1.0f / (d12_1 + d12_2);
                s.v1.a = d12_1 * inv_d12;
                s.v2.a = d12_2 * inv_d12;
                s.count = 2;
                return;
            }

            // e13
            if (d13_1 > 0.0f && d13_2 > 0.0f && d123_2 <= 0.0f)
            {
                float inv_d13 = 1.0f / (d13_1 + d13_2);
                s.v1.a = d13_1 * inv_d13;
                s.v3.a = d13_2 * inv_d13;
                s.count = 2;
                s.v2 = s.v3;
                return;
            }

            // w2 region
            if (d12_1 <= 0.0f && d23_2 <= 0.0f)
            {
                s.v2.a = 1.0f;
                s.count = 1;
                s.v1 = s.v2;
                return;
            }

            // w3 region
            if (d13_1 <= 0.0f && d23_1 <= 0.0f)
            {
                s.v3.a = 1.0f;
                s.count = 1;
                s.v1 = s.v3;
                return;
            }

            // e23
            if (d23_1 > 0.0f && d23_2 > 0.0f && d123_1 <= 0.0f)
            {
                float inv_d23 = 1.0f / (d23_1 + d23_2);
                s.v2.a = d23_1 * inv_d23;
                s.v3.a = d23_2 * inv_d23;
                s.count = 2;
                s.v1 = s.v3;
                return;
            }

            // Must be in triangle123
            float inv_d123 = 1.0f / (d123_1 + d123_2 + d123_3);
            s.v1.a = d123_1 * inv_d123;
            s.v2.a = d123_2 * inv_d123;
            s.v3.a = d123_3 * inv_d123;
            s.count = 3;
        }

        /// Compute the closest points between two shapes represented as point clouds.
        /// b2SimplexCache cache is input/output. On the first call set b2SimplexCache.count to zero.
        /// The underlying GJK algorithm may be debugged by passing in debug simplexes and capacity. You may pass in NULL and 0 for these.
        public static b2DistanceOutput b2ShapeDistance(ref b2SimplexCache cache, b2DistanceInput input, b2Simplex[] simplexes, int simplexCapacity)
        {
            b2DistanceOutput output = new b2DistanceOutput();

            ref b2ShapeProxy proxyA = ref input.proxyA;
            ref b2ShapeProxy proxyB = ref input.proxyB;

            b2Transform transformA = input.transformA;
            b2Transform transformB = input.transformB;

            // Initialize the simplex.
            b2Simplex simplex = b2MakeSimplexFromCache(ref cache, ref proxyA, transformA, ref proxyB, transformB);

            int simplexIndex = 0;
            if (simplexes != null && simplexIndex < simplexCapacity)
            {
                simplexes[simplexIndex] = simplex;
                simplexIndex += 1;
            }

            // Get simplex vertices as an array.
            b2SimplexVertex[] vertices = { simplex.v1, simplex.v2, simplex.v3 };
            const int k_maxIters = 20;

            // These store the vertices of the last simplex so that we can check for duplicates and prevent cycling.
            int[] saveA = new int[3], saveB = new int[3];

            // Main iteration loop.
            int iter = 0;
            while (iter < k_maxIters)
            {
                // Copy simplex so we can identify duplicates.
                int saveCount = simplex.count;
                for (int i = 0; i < saveCount; ++i)
                {
                    saveA[i] = vertices[i].indexA;
                    saveB[i] = vertices[i].indexB;
                }

                switch (simplex.count)
                {
                    case 1:
                        break;

                    case 2:
                        b2SolveSimplex2(simplex);
                        break;

                    case 3:
                        b2SolveSimplex3(simplex);
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }

                // If we have 3 points, then the origin is in the corresponding triangle.
                if (simplex.count == 3)
                {
                    break;
                }

                if (simplexes != null && simplexIndex < simplexCapacity)
                {
                    simplexes[simplexIndex] = simplex;
                    simplexIndex += 1;
                }

                // Get search direction.
                b2Vec2 d = b2ComputeSimplexSearchDirection(simplex);

                // Ensure the search direction is numerically fit.
                if (b2Dot(d, d) < FLT_EPSILON * FLT_EPSILON)
                {
                    // The origin is probably contained by a line segment
                    // or triangle. Thus the shapes are overlapped.

                    // We can't return zero here even though there may be overlap.
                    // In case the simplex is a point, segment, or triangle it is difficult
                    // to determine if the origin is contained in the CSO or very close to it.
                    break;
                }

                // Compute a tentative new simplex vertex using support points.
                // support = support(b, d) - support(a, -d)
                b2SimplexVertex vertex = vertices[simplex.count];
                vertex.indexA = b2FindSupport(ref proxyA, b2InvRotateVector(transformA.q, b2Neg(d)));
                vertex.wA = b2TransformPoint(transformA, proxyA.points[vertex.indexA]);
                vertex.indexB = b2FindSupport(ref proxyB, b2InvRotateVector(transformB.q, d));
                vertex.wB = b2TransformPoint(transformB, proxyB.points[vertex.indexB]);
                vertex.w = b2Sub(vertex.wB, vertex.wA);

                // Iteration count is equated to the number of support point calls.
                ++iter;

                // Check for duplicate support points. This is the main termination criteria.
                bool duplicate = false;
                for (int i = 0; i < saveCount; ++i)
                {
                    if (vertex.indexA == saveA[i] && vertex.indexB == saveB[i])
                    {
                        duplicate = true;
                        break;
                    }
                }

                // If we found a duplicate support point we must exit to avoid cycling.
                if (duplicate)
                {
                    break;
                }

                // New vertex is ok and needed.
                ++simplex.count;
            }

            if (simplexes != null && simplexIndex < simplexCapacity)
            {
                simplexes[simplexIndex] = simplex;
                simplexIndex += 1;
            }

            // Prepare output
            b2ComputeSimplexWitnessPoints(ref output.pointA, ref output.pointB, simplex);
            output.distance = b2Distance(output.pointA, output.pointB);
            output.iterations = iter;
            output.simplexCount = simplexIndex;

            // Cache the simplex
            b2MakeSimplexCache(ref cache, simplex);

            // Apply radii if requested
            if (input.useRadii)
            {
                if (output.distance < FLT_EPSILON)
                {
                    // Shapes are too close to safely compute normal
                    b2Vec2 p = new b2Vec2(0.5f * (output.pointA.x + output.pointB.x), 0.5f * (output.pointA.y + output.pointB.y));
                    output.pointA = p;
                    output.pointB = p;
                    output.distance = 0.0f;
                }
                else
                {
                    // Keep closest points on perimeter even if overlapped, this way
                    // the points move smoothly.
                    float rA = proxyA.radius;
                    float rB = proxyB.radius;
                    output.distance = b2MaxFloat(0.0f, output.distance - rA - rB);
                    b2Vec2 normal = b2Normalize(b2Sub(output.pointB, output.pointA));
                    b2Vec2 offsetA = new b2Vec2(rA * normal.x, rA * normal.y);
                    b2Vec2 offsetB = new b2Vec2(rB * normal.x, rB * normal.y);
                    output.pointA = b2Add(output.pointA, offsetA);
                    output.pointB = b2Sub(output.pointB, offsetB);
                }
            }

            return output;
        }

        /// Perform a linear shape cast of shape B moving and shape A fixed. Determines the hit point, normal, and translation fraction.
        // GJK-raycast
        // Algorithm by Gino van den Bergen.
        // "Smooth Mesh Contacts with GJK" in Game Physics Pearls. 2010
        // todo this is failing when used to raycast a box
        // todo this converges slowly with a radius
        public static b2CastOutput b2ShapeCast(b2ShapeCastPairInput input)
        {
            b2CastOutput output = new b2CastOutput();
            output.fraction = input.maxFraction;

            b2ShapeProxy proxyA = input.proxyA;

            b2Transform xfA = input.transformA;
            b2Transform xfB = input.transformB;
            b2Transform xf = b2InvMulTransforms(xfA, xfB);

            // Put proxyB in proxyA's frame to reduce round-off error
            b2ShapeProxy proxyB = new b2ShapeProxy();
            proxyB.count = input.proxyB.count;
            proxyB.radius = input.proxyB.radius;
            Debug.Assert(proxyB.count <= B2_MAX_POLYGON_VERTICES);

            for (int i = 0; i < proxyB.count; ++i)
            {
                proxyB.points[i] = b2TransformPoint(xf, input.proxyB.points[i]);
            }

            float radius = proxyA.radius + proxyB.radius;

            b2Vec2 r = b2RotateVector(xf.q, input.translationB);
            float lambda = 0.0f;
            float maxFraction = input.maxFraction;

            // Initial simplex
            b2Simplex simplex = new b2Simplex();
            simplex.count = 0;

            // Get simplex vertices as an array.
            b2SimplexVertex[] vertices = { simplex.v1, simplex.v2, simplex.v3 };

            // Get an initial point in A - B
            int indexA = b2FindSupport(ref proxyA, b2Neg(r));
            b2Vec2 wA = proxyA.points[indexA];
            int indexB = b2FindSupport(ref proxyB, r);
            b2Vec2 wB = proxyB.points[indexB];
            b2Vec2 v = b2Sub(wA, wB);

            // Sigma is the target distance between proxies
            float linearSlop = B2_LINEAR_SLOP;
            float sigma = b2MaxFloat(linearSlop, radius - linearSlop);

            // Main iteration loop.
            const int k_maxIters = 20;
            int iter = 0;
            while (iter < k_maxIters && b2Length(v) > sigma + 0.5f * linearSlop)
            {
                Debug.Assert(simplex.count < 3);

                output.iterations += 1;

                // Support in direction -v (A - B)
                indexA = b2FindSupport(ref proxyA, b2Neg(v));
                wA = proxyA.points[indexA];
                indexB = b2FindSupport(ref proxyB, v);
                wB = proxyB.points[indexB];
                b2Vec2 p = b2Sub(wA, wB);

                // -v is a normal at p, normalize to work with sigma
                v = b2Normalize(v);

                // Intersect ray with plane
                float vp = b2Dot(v, p);
                float vr = b2Dot(v, r);
                if (vp - sigma > lambda * vr)
                {
                    if (vr <= 0.0f)
                    {
                        // miss
                        return output;
                    }

                    lambda = (vp - sigma) / vr;
                    if (lambda > maxFraction)
                    {
                        // too far
                        return output;
                    }

                    // reset the simplex
                    simplex.count = 0;
                }

                // Reverse simplex since it works with B - A.
                // Shift by lambda * r because we want the closest point to the current clip point.
                // Note that the support point p is not shifted because we want the plane equation
                // to be formed in unshifted space.
                b2SimplexVertex vertex = vertices[simplex.count];
                vertex.indexA = indexB;
                vertex.wA = new b2Vec2(wB.x + lambda * r.x, wB.y + lambda * r.y);
                vertex.indexB = indexA;
                vertex.wB = wA;
                vertex.w = b2Sub(vertex.wB, vertex.wA);
                vertex.a = 1.0f;
                simplex.count += 1;

                switch (simplex.count)
                {
                    case 1:
                        break;

                    case 2:
                        b2SolveSimplex2(simplex);
                        break;

                    case 3:
                        b2SolveSimplex3(simplex);
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }

                // If we have 3 points, then the origin is in the corresponding triangle.
                if (simplex.count == 3)
                {
                    // Overlap
                    return output;
                }

                // Get search direction.
                // todo use more accurate segment perpendicular
                v = b2ComputeSimplexClosestPoint(simplex);

                // Iteration count is equated to the number of support point calls.
                ++iter;
            }

            if (iter == 0 || lambda == 0.0f)
            {
                // Initial overlap
                return output;
            }

            // Prepare output.
            b2Vec2 pointA = new b2Vec2(), pointB = new b2Vec2();
            b2ComputeSimplexWitnessPoints(ref pointB, ref pointA, simplex);

            b2Vec2 n = b2Normalize(b2Neg(v));
            b2Vec2 point = new b2Vec2(pointA.x + proxyA.radius * n.x, pointA.y + proxyA.radius * n.y);

            output.point = b2TransformPoint(xfA, point);
            output.normal = b2RotateVector(xfA.q, n);
            output.fraction = lambda;
            output.iterations = iter;
            output.hit = true;
            return output;
        }



        public static b2SeparationFunction b2MakeSeparationFunction(ref b2SimplexCache cache, ref b2ShapeProxy proxyA, b2Sweep sweepA, ref b2ShapeProxy proxyB, b2Sweep sweepB, float t1)
        {
            b2SeparationFunction f = new b2SeparationFunction();

            // TODO: @ikpil, check!!
            f.proxyA = proxyA;
            f.proxyB = proxyB;
            int count = cache.count;
            Debug.Assert(0 < count && count < 3);

            f.sweepA = sweepA.Clone();
            f.sweepB = sweepB.Clone();

            b2Transform xfA = b2GetSweepTransform(sweepA, t1);
            b2Transform xfB = b2GetSweepTransform(sweepB, t1);

            if (count == 1)
            {
                f.type = b2SeparationType.b2_pointsType;
                b2Vec2 localPointA = proxyA.points[cache.indexA[0]];
                b2Vec2 localPointB = proxyB.points[cache.indexB[0]];
                b2Vec2 pointA = b2TransformPoint(xfA, localPointA);
                b2Vec2 pointB = b2TransformPoint(xfB, localPointB);
                f.axis = b2Normalize(b2Sub(pointB, pointA));
                f.localPoint = b2Vec2_zero;
                return f;
            }

            if (cache.indexA[0] == cache.indexA[1])
            {
                // Two points on B and one on A.
                f.type = b2SeparationType.b2_faceBType;
                b2Vec2 localPointB1 = proxyB.points[cache.indexB[0]];
                b2Vec2 localPointB2 = proxyB.points[cache.indexB[1]];

                f.axis = b2CrossVS(b2Sub(localPointB2, localPointB1), 1.0f);
                f.axis = b2Normalize(f.axis);
                b2Vec2 normal = b2RotateVector(xfB.q, f.axis);

                f.localPoint = new b2Vec2(0.5f * (localPointB1.x + localPointB2.x), 0.5f * (localPointB1.y + localPointB2.y));
                b2Vec2 pointB = b2TransformPoint(xfB, f.localPoint);

                b2Vec2 localPointA = proxyA.points[cache.indexA[0]];
                b2Vec2 pointA = b2TransformPoint(xfA, localPointA);

                float s = b2Dot(b2Sub(pointA, pointB), normal);
                if (s < 0.0f)
                {
                    f.axis = b2Neg(f.axis);
                }

                return f;
            }

            {
                // Two points on A and one or two points on B.
                f.type = b2SeparationType.b2_faceAType;
                b2Vec2 localPointA1 = proxyA.points[cache.indexA[0]];
                b2Vec2 localPointA2 = proxyA.points[cache.indexA[1]];

                f.axis = b2CrossVS(b2Sub(localPointA2, localPointA1), 1.0f);
                f.axis = b2Normalize(f.axis);
                b2Vec2 normal = b2RotateVector(xfA.q, f.axis);

                f.localPoint = new b2Vec2(0.5f * (localPointA1.x + localPointA2.x), 0.5f * (localPointA1.y + localPointA2.y));
                b2Vec2 pointA = b2TransformPoint(xfA, f.localPoint);

                b2Vec2 localPointB = proxyB.points[cache.indexB[0]];
                b2Vec2 pointB = b2TransformPoint(xfB, localPointB);

                float s = b2Dot(b2Sub(pointB, pointA), normal);
                if (s < 0.0f)
                {
                    f.axis = b2Neg(f.axis);
                }

                return f;
            }
        }

        public static float b2FindMinSeparation(ref b2SeparationFunction f, ref int indexA, ref int indexB, float t)
        {
            b2Transform xfA = b2GetSweepTransform(f.sweepA, t);
            b2Transform xfB = b2GetSweepTransform(f.sweepB, t);

            switch (f.type)
            {
                case b2SeparationType.b2_pointsType:
                {
                    b2Vec2 axisA = b2InvRotateVector(xfA.q, f.axis);
                    b2Vec2 axisB = b2InvRotateVector(xfB.q, b2Neg(f.axis));

                    indexA = b2FindSupport(ref f.proxyA, axisA);
                    indexB = b2FindSupport(ref f.proxyB, axisB);

                    b2Vec2 localPointA = f.proxyA.points[indexA];
                    b2Vec2 localPointB = f.proxyB.points[indexB];

                    b2Vec2 pointA = b2TransformPoint(xfA, localPointA);
                    b2Vec2 pointB = b2TransformPoint(xfB, localPointB);

                    float separation = b2Dot(b2Sub(pointB, pointA), f.axis);
                    return separation;
                }

                case b2SeparationType.b2_faceAType:
                {
                    b2Vec2 normal = b2RotateVector(xfA.q, f.axis);
                    b2Vec2 pointA = b2TransformPoint(xfA, f.localPoint);

                    b2Vec2 axisB = b2InvRotateVector(xfB.q, b2Neg(normal));

                    indexA = -1;
                    indexB = b2FindSupport(ref f.proxyB, axisB);

                    b2Vec2 localPointB = f.proxyB.points[indexB];
                    b2Vec2 pointB = b2TransformPoint(xfB, localPointB);

                    float separation = b2Dot(b2Sub(pointB, pointA), normal);
                    return separation;
                }

                case b2SeparationType.b2_faceBType:
                {
                    b2Vec2 normal = b2RotateVector(xfB.q, f.axis);
                    b2Vec2 pointB = b2TransformPoint(xfB, f.localPoint);

                    b2Vec2 axisA = b2InvRotateVector(xfA.q, b2Neg(normal));

                    indexB = -1;
                    indexA = b2FindSupport(ref f.proxyA, axisA);

                    b2Vec2 localPointA = f.proxyA.points[indexA];
                    b2Vec2 pointA = b2TransformPoint(xfA, localPointA);

                    float separation = b2Dot(b2Sub(pointA, pointB), normal);
                    return separation;
                }

                default:
                    Debug.Assert(false);
                    indexA = -1;
                    indexB = -1;
                    return 0.0f;
            }
        }

        //
        static float b2EvaluateSeparation(b2SeparationFunction f, int indexA, int indexB, float t)
        {
            b2Transform xfA = b2GetSweepTransform(f.sweepA, t);
            b2Transform xfB = b2GetSweepTransform(f.sweepB, t);

            switch (f.type)
            {
                case b2SeparationType.b2_pointsType:
                {
                    b2Vec2 localPointA = f.proxyA.points[indexA];
                    b2Vec2 localPointB = f.proxyB.points[indexB];

                    b2Vec2 pointA = b2TransformPoint(xfA, localPointA);
                    b2Vec2 pointB = b2TransformPoint(xfB, localPointB);

                    float separation = b2Dot(b2Sub(pointB, pointA), f.axis);
                    return separation;
                }

                case b2SeparationType.b2_faceAType:
                {
                    b2Vec2 normal = b2RotateVector(xfA.q, f.axis);
                    b2Vec2 pointA = b2TransformPoint(xfA, f.localPoint);

                    b2Vec2 localPointB = f.proxyB.points[indexB];
                    b2Vec2 pointB = b2TransformPoint(xfB, localPointB);

                    float separation = b2Dot(b2Sub(pointB, pointA), normal);
                    return separation;
                }

                case b2SeparationType.b2_faceBType:
                {
                    b2Vec2 normal = b2RotateVector(xfB.q, f.axis);
                    b2Vec2 pointB = b2TransformPoint(xfB, f.localPoint);

                    b2Vec2 localPointA = f.proxyA.points[indexA];
                    b2Vec2 pointA = b2TransformPoint(xfA, localPointA);

                    float separation = b2Dot(b2Sub(pointA, pointB), normal);
                    return separation;
                }

                default:
                    Debug.Assert(false);
                    return 0.0f;
            }
        }

        /// Compute the upper bound on time before two shapes penetrate. Time is represented as
        /// a fraction between [0,tMax]. This uses a swept separating axis and may miss some intermediate,
        /// non-tunneling collisions. If you change the time interval, you should call this function
        /// again.
        // CCD via the local separating axis method. This seeks progression
        // by computing the largest time at which separation is maintained.
        public static b2TOIOutput b2TimeOfImpact(b2TOIInput input)
        {
#if B2_SNOOP_TOI_COUNTERS
        ulong ticks = b2GetTicks();
        ++b2_toiCalls;
#endif

            b2TOIOutput output = new b2TOIOutput();
            output.state = b2TOIState.b2_toiStateUnknown;
            output.fraction = input.maxFraction;

            b2Sweep sweepA = input.sweepA;
            b2Sweep sweepB = input.sweepB;
            Debug.Assert(b2IsNormalized(sweepA.q1) && b2IsNormalized(sweepA.q2));
            Debug.Assert(b2IsNormalized(sweepB.q1) && b2IsNormalized(sweepB.q2));

            // todo_erin
            // c1 can be at the origin yet the points are far away
            // b2Vec2 origin = b2Add(sweepA.c1, input.proxyA.points[0]);

            ref b2ShapeProxy proxyA = ref input.proxyA;
            ref b2ShapeProxy proxyB = ref input.proxyB;

            float tMax = input.maxFraction;

            float totalRadius = proxyA.radius + proxyB.radius;
            // todo_erin consider different target
            // float target = b2MaxFloat( B2_LINEAR_SLOP, totalRadius );
            float target = b2MaxFloat(B2_LINEAR_SLOP, totalRadius - B2_LINEAR_SLOP);
            float tolerance = 0.25f * B2_LINEAR_SLOP;
            Debug.Assert(target > tolerance);

            float t1 = 0.0f;
            const int k_maxIterations = 20;
            int distanceIterations = 0;

            // Prepare input for distance query.
            b2SimplexCache cache = new b2SimplexCache();
            b2DistanceInput distanceInput = new b2DistanceInput();
            distanceInput.proxyA = input.proxyA;
            distanceInput.proxyB = input.proxyB;
            distanceInput.useRadii = false;

            // The outer loop progressively attempts to compute new separating axes.
            // This loop terminates when an axis is repeated (no progress is made).
            for (;;)
            {
                b2Transform xfA = b2GetSweepTransform(sweepA, t1);
                b2Transform xfB = b2GetSweepTransform(sweepB, t1);

                // Get the distance between shapes. We can also use the results
                // to get a separating axis.
                distanceInput.transformA = xfA;
                distanceInput.transformB = xfB;
                b2DistanceOutput distanceOutput = b2ShapeDistance(ref cache, distanceInput, null, 0);

                distanceIterations += 1;
#if B2_SNOOP_TOI_COUNTERS
            b2_toiDistanceIterations += 1;
#endif

                // If the shapes are overlapped, we give up on continuous collision.
                if (distanceOutput.distance <= 0.0f)
                {
                    // Failure!
                    output.state = b2TOIState.b2_toiStateOverlapped;
#if B2_SNOOP_TOI_COUNTERS
                b2_toiOverlappedCount += 1;
#endif
                    output.fraction = 0.0f;
                    break;
                }

                if (distanceOutput.distance <= target + tolerance)
                {
                    // Victory!
                    output.state = b2TOIState.b2_toiStateHit;
#if B2_SNOOP_TOI_COUNTERS
                b2_toiHitCount += 1;
#endif
                    output.fraction = t1;
                    break;
                }

                // Initialize the separating axis.
                b2SeparationFunction fcn = b2MakeSeparationFunction(ref cache, ref proxyA, sweepA, ref proxyB, sweepB, t1);
#if ZERO_DEFINE
                    // Dump the curve seen by the root finder
                    {
                        const int N = 100;
                        float dx = 1.0f / N;
                        float xs[N + 1];
                        float fs[N + 1];
            
                        float x = 0.0f;
            
                        for (int i = 0; i <= N; ++i)
                        {
                            sweepA.GetTransform(&xfA, x);
                            sweepB.GetTransform(&xfB, x);
                            float f = fcn.Evaluate(xfA, xfB) - target;
            
                            Console.Write("%g %g\n", x, f);
            
                            xs[i] = x;
                            fs[i] = f;
            
                            x += dx;
                        }
                    }
#endif

                // Compute the TOI on the separating axis. We do this by successively
                // resolving the deepest point. This loop is bounded by the number of vertices.
                bool done = false;
                float t2 = tMax;
                int pushBackIterations = 0;
                for (;;)
                {
                    // Find the deepest point at t2. Store the witness point indices.
                    int indexA = 0, indexB = 0;
                    float s2 = b2FindMinSeparation(ref fcn, ref indexA, ref indexB, t2);

                    // Is the final configuration separated?
                    if (s2 > target + tolerance)
                    {
                        // Victory!
                        output.state = b2TOIState.b2_toiStateSeparated;
#if B2_SNOOP_TOI_COUNTERS
                    b2_toiSeparatedCount += 1;
#endif
                        output.fraction = tMax;
                        done = true;
                        break;
                    }

                    // Has the separation reached tolerance?
                    if (s2 > target - tolerance)
                    {
                        // Advance the sweeps
                        t1 = t2;
                        break;
                    }

                    // Compute the initial separation of the witness points.
                    float s1 = b2EvaluateSeparation(fcn, indexA, indexB, t1);

                    // Check for initial overlap. This might happen if the root finder
                    // runs out of iterations.
                    if (s1 < target - tolerance)
                    {
                        output.state = b2TOIState.b2_toiStateFailed;
#if B2_SNOOP_TOI_COUNTERS
                    b2_toiFailedCount += 1;
#endif
                        output.fraction = t1;
                        done = true;
                        break;
                    }

                    // Check for touching
                    if (s1 <= target + tolerance)
                    {
                        // Victory! t1 should hold the TOI (could be 0.0).
                        output.state = b2TOIState.b2_toiStateHit;
#if B2_SNOOP_TOI_COUNTERS
                    b2_toiHitCount += 1;
#endif
                        output.fraction = t1;
                        done = true;
                        break;
                    }

                    // Compute 1D root of: f(x) - target = 0
                    int rootIterationCount = 0;
                    float a1 = t1, a2 = t2;
                    for (;;)
                    {
                        // Use a mix of the secant rule and bisection.
                        float t;
                        if (0 != (rootIterationCount & 1))
                        {
                            // Secant rule to improve convergence.
                            t = a1 + (target - s1) * (a2 - a1) / (s2 - s1);
                        }
                        else
                        {
                            // Bisection to guarantee progress.
                            t = 0.5f * (a1 + a2);
                        }

                        rootIterationCount += 1;

#if B2_SNOOP_TOI_COUNTERS
                    ++b2_toiRootIterations;
#endif

                        float s = b2EvaluateSeparation(fcn, indexA, indexB, t);

                        if (b2AbsFloat(s - target) < tolerance)
                        {
                            // t2 holds a tentative value for t1
                            t2 = t;
                            break;
                        }

                        // Ensure we continue to bracket the root.
                        if (s > target)
                        {
                            a1 = t;
                            s1 = s;
                        }
                        else
                        {
                            a2 = t;
                            s2 = s;
                        }

                        if (rootIterationCount == 50)
                        {
                            break;
                        }
                    }

#if B2_SNOOP_TOI_COUNTERS
                b2_toiMaxRootIterations = b2MaxInt( b2_toiMaxRootIterations, rootIterationCount );
#endif

                    pushBackIterations += 1;

                    if (pushBackIterations == B2_MAX_POLYGON_VERTICES)
                    {
                        break;
                    }
                }

                if (done)
                {
                    break;
                }

                if (distanceIterations == k_maxIterations)
                {
                    // Root finder got stuck. Semi-victory.
                    output.state = b2TOIState.b2_toiStateFailed;
#if B2_SNOOP_TOI_COUNTERS
                b2_toiFailedCount += 1;
#endif
                    output.fraction = t1;
                    break;
                }
            }

#if B2_SNOOP_TOI_COUNTERS
        b2_toiMaxDistanceIterations = b2MaxInt( b2_toiMaxDistanceIterations, distanceIterations );

        float time = b2GetMilliseconds( ticks );
        b2_toiMaxTime = b2MaxFloat( b2_toiMaxTime, time );
        b2_toiTime += time;
#endif

            return output;
        }
    }
}