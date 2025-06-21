// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Timers;

namespace Box2D.NET
{
    public static class B2Distances
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
        public static B2Transform b2GetSweepTransform(ref B2Sweep sweep, float time)
        {
            // https://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
            B2Transform xf;
            xf.p = b2Add(b2MulSV(1.0f - time, sweep.c1), b2MulSV(time, sweep.c2));

            B2Rot q = new B2Rot(
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
        public static B2SegmentDistanceResult b2SegmentDistance(B2Vec2 p1, B2Vec2 q1, B2Vec2 p2, B2Vec2 q2)
        {
            B2SegmentDistanceResult result = new B2SegmentDistanceResult();

            B2Vec2 d1 = b2Sub(q1, p1);
            B2Vec2 d2 = b2Sub(q2, p2);
            B2Vec2 r = b2Sub(p1, p2);
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

                float denominator = dd1 * dd2 - d12 * d12;

                // Fraction on segment 1
                float f1 = 0.0f;
                if (denominator != 0.0f)
                {
                    // not parallel
                    f1 = b2ClampFloat((d12 * rd2 - rd1 * dd2) / denominator, 0.0f, 1.0f);
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

        /// Make a proxy for use in overlap, shape cast, and related functions. This is a deep copy of the points.
        /// Make a proxy for use in GJK and related functions.
        // GJK using Voronoi regions (Christer Ericson) and Barycentric coordinates.
        // todo try not copying
        public static B2ShapeProxy b2MakeProxy(ReadOnlySpan<B2Vec2> points, int count, float radius)
        {
            count = b2MinInt(count, B2_MAX_POLYGON_VERTICES);
            B2ShapeProxy proxy = new B2ShapeProxy();
            for (int i = 0; i < count; ++i)
            {
                proxy.points[i] = points[i];
            }

            proxy.count = count;
            proxy.radius = radius;
            return proxy;
        }

        /// Make a proxy with a transform. This is a deep copy of the points.
        public static B2ShapeProxy b2MakeOffsetProxy(ReadOnlySpan<B2Vec2> points, int count, float radius, B2Vec2 position, B2Rot rotation)
        {
            count = b2MinInt(count, B2_MAX_POLYGON_VERTICES);
            B2Transform transform = new B2Transform(position, rotation);

            B2ShapeProxy proxy = new B2ShapeProxy();
            for (int i = 0; i < count; ++i)
            {
                proxy.points[i] = b2TransformPoint(ref transform, points[i]);
            }

            proxy.count = count;
            proxy.radius = radius;
            return proxy;
        }


        // for single
        public static B2ShapeProxy b2MakeProxy(B2Vec2 v1, int count, float radius)
        {
            B2_ASSERT(count == 1);

            Span<B2Vec2> vertices = stackalloc B2Vec2[1];
            vertices[0] = v1;
            return b2MakeProxy(vertices, count, radius);
        }

        public static B2ShapeProxy b2MakeProxy(B2Vec2 v1, B2Vec2 v2, int count, float radius)
        {
            B2_ASSERT(count == 2);

            Span<B2Vec2> vertices = stackalloc B2Vec2[2];
            vertices[0] = v1;
            vertices[1] = v2;
            return b2MakeProxy(vertices, count, radius);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Weight2(float a1, B2Vec2 w1, float a2, B2Vec2 w2)
        {
            return new B2Vec2(a1 * w1.X + a2 * w2.X, a1 * w1.Y + a2 * w2.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 b2Weight3(float a1, B2Vec2 w1, float a2, B2Vec2 w2, float a3, B2Vec2 w3)
        {
            return new B2Vec2(a1 * w1.X + a2 * w2.X + a3 * w3.X, a1 * w1.Y + a2 * w2.Y + a3 * w3.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2FindSupport(ref B2ShapeProxy proxy, B2Vec2 direction)
        {
            Span<B2Vec2> points = proxy.points.AsSpan();
            int count = proxy.count;

            int bestIndex = 0;
            float bestValue = b2Dot(points[0], direction);
            for (int i = 1; i < count; ++i)
            {
                float value = b2Dot(points[i], direction);
                if (value > bestValue)
                {
                    bestIndex = i;
                    bestValue = value;
                }
            }

            return bestIndex;
        }

        public static B2Simplex b2MakeSimplexFromCache(ref B2SimplexCache cache, ref B2ShapeProxy proxyA, ref B2ShapeProxy proxyB)
        {
            B2_ASSERT(cache.count <= 3);
            B2Simplex s = new B2Simplex();

            // Copy data from cache.
            s.count = cache.count;

            Span<B2SimplexVertex> vertices = s.AsSpan();
            for (int i = 0; i < s.count; ++i)
            {
                ref B2SimplexVertex v = ref vertices[i];
                v.indexA = cache.indexA[i];
                v.indexB = cache.indexB[i];
                v.wA = proxyA.points[v.indexA];
                v.wB = proxyB.points[v.indexB];
                v.w = b2Sub(v.wA, v.wB);

                // invalid
                v.a = -1.0f;
            }

            // If the cache is empty or invalid ...
            if (s.count == 0)
            {
                ref B2SimplexVertex v = ref vertices[0];
                v.indexA = 0;
                v.indexB = 0;
                v.wA = proxyA.points[0];
                v.wB = proxyB.points[0];
                v.w = b2Sub(v.wA, v.wB);
                v.a = 1.0f;
                s.count = 1;
            }

            return s;
        }

        public static void b2MakeSimplexCache(ref B2SimplexCache cache, ref B2Simplex simplex)
        {
            cache.count = (ushort)simplex.count;
            Span<B2SimplexVertex> vertices = simplex.AsSpan();
            for (int i = 0; i < simplex.count; ++i)
            {
                cache.indexA[i] = (byte)vertices[i].indexA;
                cache.indexB[i] = (byte)vertices[i].indexB;
            }
        }

        public static void b2ComputeSimplexWitnessPoints(ref B2Vec2 a, ref B2Vec2 b, ref B2Simplex s)
        {
            switch (s.count)
            {
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
                    // todo why are these not equal?
                    //*b = b2Weight3(s.v1.a, s.v1.wB, s.v2.a, s.v2.wB, s.v3.a, s.v3.wB);
                    b = a;
                    break;

                default:
                    a = b2Vec2_zero;
                    b = b2Vec2_zero;
                    B2_ASSERT(false);
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
        //
        // returns a vector that points towards the origin
        public static B2Vec2 b2SolveSimplex2(ref B2Simplex s)
        {
            B2Vec2 w1 = s.v1.w;
            B2Vec2 w2 = s.v2.w;
            B2Vec2 e12 = b2Sub(w2, w1);

            // w1 region
            float d12_2 = -b2Dot(w1, e12);
            if (d12_2 <= 0.0f)
            {
                // a2 <= 0, so we clamp it to 0
                s.v1.a = 1.0f;
                s.count = 1;
                return b2Neg(w1);
            }

            // w2 region
            float d12_1 = b2Dot(w2, e12);
            if (d12_1 <= 0.0f)
            {
                // a1 <= 0, so we clamp it to 0
                s.v2.a = 1.0f;
                s.count = 1;
                s.v1 = s.v2;
                return b2Neg(w2);
            }

            // Must be in e12 region.
            float inv_d12 = 1.0f / (d12_1 + d12_2);
            s.v1.a = d12_1 * inv_d12;
            s.v2.a = d12_2 * inv_d12;
            s.count = 2;
            return b2CrossSV(b2Cross(b2Add(w1, w2), e12), e12);
        }

        public static B2Vec2 b2SolveSimplex3(ref B2Simplex s)
        {
            B2Vec2 w1 = s.v1.w;
            B2Vec2 w2 = s.v2.w;
            B2Vec2 w3 = s.v3.w;

            // Edge12
            // [1      1     ][a1] = [1]
            // [w1.e12 w2.e12][a2] = [0]
            // a3 = 0
            B2Vec2 e12 = b2Sub(w2, w1);
            float w1e12 = b2Dot(w1, e12);
            float w2e12 = b2Dot(w2, e12);
            float d12_1 = w2e12;
            float d12_2 = -w1e12;

            // Edge13
            // [1      1     ][a1] = [1]
            // [w1.e13 w3.e13][a3] = [0]
            // a2 = 0
            B2Vec2 e13 = b2Sub(w3, w1);
            float w1e13 = b2Dot(w1, e13);
            float w3e13 = b2Dot(w3, e13);
            float d13_1 = w3e13;
            float d13_2 = -w1e13;

            // Edge23
            // [1      1     ][a2] = [1]
            // [w2.e23 w3.e23][a3] = [0]
            // a1 = 0
            B2Vec2 e23 = b2Sub(w3, w2);
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
                return b2Neg(w1);
            }

            // e12
            if (d12_1 > 0.0f && d12_2 > 0.0f && d123_3 <= 0.0f)
            {
                float inv_d12 = 1.0f / (d12_1 + d12_2);
                s.v1.a = d12_1 * inv_d12;
                s.v2.a = d12_2 * inv_d12;
                s.count = 2;
                return b2CrossSV(b2Cross(b2Add(w1, w2), e12), e12);
            }

            // e13
            if (d13_1 > 0.0f && d13_2 > 0.0f && d123_2 <= 0.0f)
            {
                float inv_d13 = 1.0f / (d13_1 + d13_2);
                s.v1.a = d13_1 * inv_d13;
                s.v3.a = d13_2 * inv_d13;
                s.count = 2;
                s.v2 = s.v3;
                return b2CrossSV(b2Cross(b2Add(w1, w3), e13), e13);
            }

            // w2 region
            if (d12_1 <= 0.0f && d23_2 <= 0.0f)
            {
                s.v2.a = 1.0f;
                s.count = 1;
                s.v1 = s.v2;
                return b2Neg(w2);
            }

            // w3 region
            if (d13_1 <= 0.0f && d23_1 <= 0.0f)
            {
                s.v3.a = 1.0f;
                s.count = 1;
                s.v1 = s.v3;
                return b2Neg(w3);
            }

            // e23
            if (d23_1 > 0.0f && d23_2 > 0.0f && d123_1 <= 0.0f)
            {
                float inv_d23 = 1.0f / (d23_1 + d23_2);
                s.v2.a = d23_1 * inv_d23;
                s.v3.a = d23_2 * inv_d23;
                s.count = 2;
                s.v1 = s.v3;
                return b2CrossSV(b2Cross(b2Add(w2, w3), e23), e23);
            }

            // Must be in triangle123
            float inv_d123 = 1.0f / (d123_1 + d123_2 + d123_3);
            s.v1.a = d123_1 * inv_d123;
            s.v2.a = d123_2 * inv_d123;
            s.v3.a = d123_3 * inv_d123;
            s.count = 3;

            // No search direction
            return b2Vec2_zero;
        }

        /// Compute the closest points between two shapes represented as point clouds.
        /// b2SimplexCache cache is input/output. On the first call set b2SimplexCache.count to zero.
        /// The underlying GJK algorithm may be debugged by passing in debug simplexes and capacity. You may pass in NULL and 0 for these.
        // Uses GJK for computing the distance between convex shapes.
        // https://box2d.org/files/ErinCatto_GJK_GDC2010.pdf
        // I spent time optimizing this and could find no further significant gains 3/30/2025
        public static B2DistanceOutput b2ShapeDistance(ref B2DistanceInput input, ref B2SimplexCache cache, B2Simplex[] simplexes, int simplexCapacity)
        {
            B2_UNUSED(simplexes, simplexCapacity);
            B2_ASSERT(input.proxyA.count > 0 && input.proxyB.count > 0);
            B2_ASSERT(input.proxyA.radius >= 0.0f);
            B2_ASSERT(input.proxyB.radius >= 0.0f);

            B2DistanceOutput output = new B2DistanceOutput();

            ref B2ShapeProxy proxyA = ref input.proxyA;

            // Get proxyB in frame A to avoid further transforms in the main loop.
            // This is still a performance gain at 8 points.
            B2ShapeProxy localProxyB = new B2ShapeProxy();
            {
                B2Transform transform = b2InvMulTransforms(input.transformA, input.transformB);
                localProxyB.count = input.proxyB.count;
                localProxyB.radius = input.proxyB.radius;
                for (int i = 0; i < localProxyB.count; ++i)
                {
                    localProxyB.points[i] = b2TransformPoint(ref transform, input.proxyB.points[i]);
                }
            }

            // Initialize the simplex.
            B2Simplex simplex = b2MakeSimplexFromCache(ref cache, ref proxyA, ref localProxyB);

            int simplexIndex = 0;
            if (simplexes != null && simplexIndex < simplexCapacity)
            {
                simplexes[simplexIndex] = simplex;
                simplexIndex += 1;
            }

            // Get simplex vertices as an array.
            Span<B2SimplexVertex> vertices = simplex.AsSpan();

            B2Vec2 nonUnitNormal = b2Vec2_zero;

            // These store the vertices of the last simplex so that we can check for duplicates and prevent cycling.
            Span<int> saveA = stackalloc int[3];
            Span<int> saveB = stackalloc int[3];

            // Main iteration loop. All computations are done in frame A.
            const int maxIterations = 20;
            int iteration = 0;
            while (iteration < maxIterations)
            {
                // Copy simplex so we can identify duplicates.
                int saveCount = simplex.count;
                for (int i = 0; i < saveCount; ++i)
                {
                    saveA[i] = vertices[i].indexA;
                    saveB[i] = vertices[i].indexB;
                }

                B2Vec2 d = new B2Vec2();
                switch (simplex.count)
                {
                    case 1:
                        d = b2Neg(simplex.v1.w);
                        break;

                    case 2:
                        d = b2SolveSimplex2(ref simplex);
                        break;

                    case 3:
                        d = b2SolveSimplex3(ref simplex);
                        break;

                    default:
                        B2_ASSERT(false);
                        break;
                }

                // If we have 3 points, then the origin is in the corresponding triangle.
                if (simplex.count == 3)
                {
                    // Overlap
                    B2Vec2 localPointA = new B2Vec2(), localPointB = new B2Vec2();
                    b2ComputeSimplexWitnessPoints(ref localPointA, ref localPointB, ref simplex);
                    output.pointA = b2TransformPoint(ref input.transformA, localPointA);
                    output.pointB = b2TransformPoint(ref input.transformA, localPointB);
                    return output;
                }

#if DEBUG
                if (simplexes != null && simplexIndex < simplexCapacity)
                {
                    simplexes[simplexIndex] = simplex;
                    simplexIndex += 1;
                }
#endif


                // Ensure the search direction is numerically fit.
                if (b2Dot(d, d) < FLT_EPSILON * FLT_EPSILON)
                {
                    // This is unlikely but could lead to bad cycling.
                    // The branch predictor seems to make this check have low cost.

                    // The origin is probably contained by a line segment
                    // or triangle. Thus the shapes are overlapped.

                    // Must return overlap due to invalid normal.
                    B2Vec2 localPointA = new B2Vec2(), localPointB = new B2Vec2();
                    b2ComputeSimplexWitnessPoints(ref localPointA, ref localPointB, ref simplex);
                    output.pointA = b2TransformPoint(ref input.transformA, localPointA);
                    output.pointB = b2TransformPoint(ref input.transformA, localPointB);
                    return output;
                }

                // Save the normal
                nonUnitNormal = d;

                // Compute a tentative new simplex vertex using support points.
                // support = support(a, d) - support(b, -d)
                ref B2SimplexVertex vertex = ref vertices[simplex.count];
                vertex.indexA = b2FindSupport(ref proxyA, d);
                vertex.wA = proxyA.points[vertex.indexA];
                vertex.indexB = b2FindSupport(ref localProxyB, b2Neg(d));
                vertex.wB = localProxyB.points[vertex.indexB];
                vertex.w = b2Sub(vertex.wA, vertex.wB);

                // Iteration count is equated to the number of support point calls.
                ++iteration;

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

                // New vertex is valid and needed.
                simplex.count += 1;
            }

#if DEBUG
            if (simplexes != null && simplexIndex < simplexCapacity)
            {
                simplexes[simplexIndex] = simplex;
                simplexIndex += 1;
            }
#endif

            // Prepare output
            B2Vec2 normal = b2Normalize(nonUnitNormal);
            B2_ASSERT(b2IsNormalized(normal));
            normal = b2RotateVector(input.transformA.q, normal);

            {
                B2Vec2 localPointA = new B2Vec2();
                B2Vec2 localPointB = new B2Vec2();
                b2ComputeSimplexWitnessPoints(ref localPointA, ref localPointB, ref simplex);
                output.normal = normal;
                output.distance = b2Distance(localPointA, localPointB);
                output.pointA = b2TransformPoint(ref input.transformA, localPointA);
                output.pointB = b2TransformPoint(ref input.transformA, localPointB);
                output.iterations = iteration;
                output.simplexCount = simplexIndex;
            }

            // Cache the simplex
            b2MakeSimplexCache(ref cache, ref simplex);

            // Apply radii if requested
            if (input.useRadii && output.distance > 0.1f * B2_LINEAR_SLOP)
            {
                float radiusA = input.proxyA.radius;
                float radiusB = input.proxyB.radius;
                output.distance = b2MaxFloat(0.0f, output.distance - radiusA - radiusB);

                // Keep closest points on perimeter even if overlapped, this way the points move smoothly.
                output.pointA = b2MulAdd(output.pointA, radiusA, normal);
                output.pointB = b2MulSub(output.pointB, radiusB, normal);
            }

            return output;
        }

        /// Perform a linear shape cast of shape B moving and shape A fixed. Determines the hit point, normal, and translation fraction.
        /// Initially touching shapes are treated as a miss.
        // Shape cast using conservative advancement
        public static B2CastOutput b2ShapeCast(ref B2ShapeCastPairInput input)
        {
            // Compute tolerance
            float linearSlop = B2_LINEAR_SLOP;
            float totalRadius = input.proxyA.radius + input.proxyB.radius;
            float target = b2MaxFloat(linearSlop, totalRadius - linearSlop);
            float tolerance = 0.25f * linearSlop;

            B2_ASSERT(target > tolerance);

            // Prepare input for distance query
            B2SimplexCache cache = new B2SimplexCache();

            float fraction = 0.0f;

            B2DistanceInput distanceInput = new B2DistanceInput();
            distanceInput.proxyA = input.proxyA;
            distanceInput.proxyB = input.proxyB;
            distanceInput.transformA = input.transformA;
            distanceInput.transformB = input.transformB;
            distanceInput.useRadii = false;

            B2Vec2 delta2 = input.translationB;
            B2CastOutput output = new B2CastOutput();

            int iteration = 0;
            const int maxIterations = 20;
            for (; iteration < maxIterations; ++iteration)
            {
                output.iterations += 1;

                B2DistanceOutput distanceOutput = b2ShapeDistance(ref distanceInput, ref cache, null, 0);

                if (distanceOutput.distance < target + tolerance)
                {
                    if (iteration == 0)
                    {
                        if (input.canEncroach && distanceOutput.distance > 2.0f * linearSlop)
                        {
                            target = distanceOutput.distance - linearSlop;
                        }
                        else
                        {
                            // Initial overlap
                            output.hit = true;

                            // Compute a common point
                            B2Vec2 c1 = b2MulAdd(distanceOutput.pointA, input.proxyA.radius, distanceOutput.normal);
                            B2Vec2 c2 = b2MulAdd(distanceOutput.pointB, -input.proxyB.radius, distanceOutput.normal);
                            output.point = b2Lerp(c1, c2, 0.5f);
                            return output;
                        }
                    }
                    else
                    {
                        // Regular hit
                        B2_ASSERT(distanceOutput.distance > 0.0f && b2IsNormalized(distanceOutput.normal));
                        output.fraction = fraction;
                        output.point = b2MulAdd(distanceOutput.pointA, input.proxyA.radius, distanceOutput.normal);
                        output.normal = distanceOutput.normal;
                        output.hit = true;
                        return output;
                    }
                }

                B2_ASSERT(distanceOutput.distance > 0.0f);
                B2_ASSERT(b2IsNormalized(distanceOutput.normal));

                // Check if shapes are approaching each other
                float denominator = b2Dot(delta2, distanceOutput.normal);
                if (denominator >= 0.0f)
                {
                    // Miss
                    return output;
                }

                // Advance sweep
                fraction += (target - distanceOutput.distance) / denominator;
                if (fraction >= input.maxFraction)
                {
                    // Miss
                    return output;
                }

                distanceInput.transformB.p = b2MulAdd(input.transformB.p, fraction, delta2);
            }

            // Failure!
            return output;
        }


#if FALSE
        static inline b2Vec2 b2ComputeSimplexClosestPoint( const b2Simplex* s )
        {
            if ( s->count == 1 )
            {
                return s->v1.w;
            }

            if ( s->count == 2 )
            {
                return b2Weight2( s->v1.a, s->v1.w, s->v2.a, s->v2.w );
            }

            return b2Vec2_zero;
        }

        public struct b2ShapeCastData
        {
            b2Simplex simplex;
            b2Vec2 closestA, closestB;
            b2Vec2 normal;
            b2Vec2 p0;
            float fraction;
        } b2ShapeCastData;

        // GJK-raycast
        // Algorithm by Gino van den Bergen.
        // "Smooth Mesh Contacts with GJK" in Game Physics Pearls. 2010
        // This needs the simplex of A - B because the translation is for B and this
        // is how the relative motion works out when both shapes are translating.
        // This is similar to ray vs polygon and involves plane clipping. See b2RayCastPolygon.
        // In this case the polygon is just points and there are no planes. This uses a modified
        // version of GJK to generate planes for clipping.
        // The algorithm works by incrementally building clipping planes using GJK. Once a valid
        // clip plane is found the simplex origin is moved to the current fraction on the ray.
        // This resets the simplex after every clip. Later I should compare performance.
        // However, adapting this to work with encroachment is tricky and confusing because encroachment
        // needs distance.
        // Note: this algorithm is difficult to debug and not worth the effort in my opinion 4/1/2025
        b2CastOutput b2ShapeCastMerged( const b2ShapeCastPairInput* input, b2ShapeCastData* debugData, int debugCapacity )
        {
            B2_UNUSED( debugData, debugCapacity );

            b2CastOutput output = { 0 };
            output.fraction = input->maxFraction;

            b2ShapeProxy proxyA = input->proxyA;

            b2Transform xf = b2InvMulTransforms( input->transformA, input->transformB );

            // Put proxyB in proxyA's frame to reduce round-off error
            b2ShapeProxy proxyB;
            proxyB.count = input->proxyB.count;
            proxyB.radius = input->proxyB.radius;
            B2_ASSERT( proxyB.count <= B2_MAX_POLYGON_VERTICES );

            for ( int i = 0; i < proxyB.count; ++i )
            {
                proxyB.points[i] = b2TransformPoint( xf, input->proxyB.points[i] );
            }

            float radius = proxyA.radius + proxyB.radius;

            b2Vec2 r = b2RotateVector( xf.q, input->translationB );
            float lambda = 0.0f;
            float maxFraction = input->maxFraction;

            // Initial simplex
            b2Simplex simplex;
            simplex.count = 0;

            // Get simplex vertices as an array.
            b2SimplexVertex* vertices[] = { &simplex.v1, &simplex.v2, &simplex.v3 };

            // Get an initial point in A - B
            b2Vec2 wA = proxyA.points[0];
            b2Vec2 wB = proxyB.points[0];
            b2Vec2 v = b2Sub( wA, wB );
            b2Vec2 d = b2Neg( v );

            // Sigma is the target distance between proxies
            const float linearSlop = B2_LINEAR_SLOP;
            const float sigma = b2MaxFloat( linearSlop, radius - linearSlop );
            float tolerance = 0.5f * linearSlop;
            float stolSquared = ( sigma + tolerance ) * ( sigma + tolerance );

            // Main iteration loop.
            const int maxIterations = 20;
            int iteration = 0;
            while ( iteration < maxIterations && b2LengthSquared( v ) > stolSquared )
            {
                B2_ASSERT( simplex.count < 3 );

                // Support in direction d (A - B)
                int indexA = b2FindSupport( &proxyA, d );
                wA = proxyA.points[indexA];
                int indexB = b2FindSupport( &proxyB, b2Neg( d ) );
                wB = proxyB.points[indexB];
                b2Vec2 p0 = b2Sub( wA, wB );

                // d is a normal at p, normalize to work with sigma
                b2Vec2 normal = b2Normalize( d );

                // Intersect ray with plane
                // p = origin + t * r
                // dot(n, p - p0) = sigma
                // dot(n, origin - p0) + t * dot(n, r) = sigma
                // t = ( dot(n, p0) + sigma) / dot(n, r)
                // if t < (dot(n, p0) + sigma) / dot(n, r) then t can be increased
                // or (flipping sign because dot(n,r) < 0)
                // dot(n, p0) + sigma < t * dot(n, r) && dot(n, r) < 0
                float np0 = b2Dot( normal, p0 );
                float nr = b2Dot( normal, r );
                if ( np0 + sigma < lambda * nr )
                {
                    if ( nr >= 0.0f )
                    {
                        // miss
                        return output;
                    }

                    lambda = ( np0 + sigma ) / nr;
                    if ( lambda > maxFraction )
                    {
                        // too far
                        return output;
                    }

                    // reset the simplex
                    simplex.count = 0;
                }

                // Shift by lambda * r because we want the closest point to the current clip point.
                // Note that the support point p is not shifted because we want the plane equation
                // to be formed in un-shifted space.
                b2SimplexVertex* vertex = vertices[simplex.count];
                vertex->indexA = indexB;
                vertex->wA = wA;
                vertex->indexB = indexA;
                vertex->wB = (b2Vec2){ wB.x + lambda * r.x, wB.y + lambda * r.y };
                vertex->w = b2Sub( vertex->wA, vertex->wB );
                vertex->a = 1.0f;
                simplex.count += 1;

                switch ( simplex.count )
                {
                    case 1:
                        d = b2Neg( simplex.v1.w );
                        break;

                    case 2:
                        d = b2SolveSimplex2( &simplex );
                        break;

                    case 3:
                        d = b2SolveSimplex3( &simplex );
                        break;

                    default:
                        B2_ASSERT( false );
                }

        #if DEBUG 
                if ( debugData != NULL && output.iterations < debugCapacity )
                {
                    debugData[output.iterations].simplex = simplex;
                    debugData[output.iterations].normal = normal;
                    debugData[output.iterations].p0 = p0;
                    b2Vec2 cA, cB;
                    b2ComputeSimplexWitnessPoints( &cA, &cB, &simplex );
                    debugData[output.iterations].closestA = cA;
                    debugData[output.iterations].closestB = cB;
                    debugData[output.iterations].fraction = lambda;
                }
        #endif

                output.iterations += 1;

                // If we have 3 points, then the origin is in the corresponding triangle.
                if ( simplex.count == 3 )
                {
                    // Overlap
                    return output;
                }

                // Get distance vector
                v = b2ComputeSimplexClosestPoint( &simplex );

                // Iteration count is equated to the number of support point calls.
                ++iteration;
            }

            if ( iteration == 0 || lambda == 0.0f )
            {
                // Initial overlap
                return output;
            }

            // Prepare output.
            b2Vec2 pointA, pointB;
            b2ComputeSimplexWitnessPoints( &pointB, &pointA, &simplex );

            b2Vec2 n = b2Normalize( b2Neg( v ) );
            b2Vec2 point = { pointA.x + proxyA.radius * n.x, pointA.y + proxyA.radius * n.y };

            output.point = b2TransformPoint( input->transformA, point );
            output.normal = b2RotateVector( input->transformA.q, n );
            output.fraction = lambda;
            output.iterations = iteration;
            output.hit = true;
            return output;
        }
#endif

        public static B2SeparationFunction b2MakeSeparationFunction(ref B2SimplexCache cache, ref B2ShapeProxy proxyA, ref B2Sweep sweepA, ref B2ShapeProxy proxyB, ref B2Sweep sweepB, float t1)
        {
            B2SeparationFunction f = new B2SeparationFunction();

            // TODO: @ikpil, check!!
            f.proxyA = proxyA;
            f.proxyB = proxyB;
            int count = cache.count;
            B2_ASSERT(0 < count && count < 3);

            f.sweepA = sweepA;
            f.sweepB = sweepB;

            B2Transform xfA = b2GetSweepTransform(ref sweepA, t1);
            B2Transform xfB = b2GetSweepTransform(ref sweepB, t1);

            if (count == 1)
            {
                f.type = B2SeparationType.b2_pointsType;
                B2Vec2 localPointA = proxyA.points[cache.indexA[0]];
                B2Vec2 localPointB = proxyB.points[cache.indexB[0]];
                B2Vec2 pointA = b2TransformPoint(ref xfA, localPointA);
                B2Vec2 pointB = b2TransformPoint(ref xfB, localPointB);
                f.axis = b2Normalize(b2Sub(pointB, pointA));
                f.localPoint = b2Vec2_zero;
                return f;
            }

            if (cache.indexA[0] == cache.indexA[1])
            {
                // Two points on B and one on A.
                f.type = B2SeparationType.b2_faceBType;
                B2Vec2 localPointB1 = proxyB.points[cache.indexB[0]];
                B2Vec2 localPointB2 = proxyB.points[cache.indexB[1]];

                f.axis = b2CrossVS(b2Sub(localPointB2, localPointB1), 1.0f);
                f.axis = b2Normalize(f.axis);
                B2Vec2 normal = b2RotateVector(xfB.q, f.axis);

                f.localPoint = new B2Vec2(0.5f * (localPointB1.X + localPointB2.X), 0.5f * (localPointB1.Y + localPointB2.Y));
                B2Vec2 pointB = b2TransformPoint(ref xfB, f.localPoint);

                B2Vec2 localPointA = proxyA.points[cache.indexA[0]];
                B2Vec2 pointA = b2TransformPoint(ref xfA, localPointA);

                float s = b2Dot(b2Sub(pointA, pointB), normal);
                if (s < 0.0f)
                {
                    f.axis = b2Neg(f.axis);
                }

                return f;
            }

            {
                // Two points on A and one or two points on B.
                f.type = B2SeparationType.b2_faceAType;
                B2Vec2 localPointA1 = proxyA.points[cache.indexA[0]];
                B2Vec2 localPointA2 = proxyA.points[cache.indexA[1]];

                f.axis = b2CrossVS(b2Sub(localPointA2, localPointA1), 1.0f);
                f.axis = b2Normalize(f.axis);
                B2Vec2 normal = b2RotateVector(xfA.q, f.axis);

                f.localPoint = new B2Vec2(0.5f * (localPointA1.X + localPointA2.X), 0.5f * (localPointA1.Y + localPointA2.Y));
                B2Vec2 pointA = b2TransformPoint(ref xfA, f.localPoint);

                B2Vec2 localPointB = proxyB.points[cache.indexB[0]];
                B2Vec2 pointB = b2TransformPoint(ref xfB, localPointB);

                float s = b2Dot(b2Sub(pointB, pointA), normal);
                if (s < 0.0f)
                {
                    f.axis = b2Neg(f.axis);
                }

                return f;
            }
        }

        public static float b2FindMinSeparation(ref B2SeparationFunction f, ref int indexA, ref int indexB, float t)
        {
            B2Transform xfA = b2GetSweepTransform(ref f.sweepA, t);
            B2Transform xfB = b2GetSweepTransform(ref f.sweepB, t);

            switch (f.type)
            {
                case B2SeparationType.b2_pointsType:
                {
                    B2Vec2 axisA = b2InvRotateVector(xfA.q, f.axis);
                    B2Vec2 axisB = b2InvRotateVector(xfB.q, b2Neg(f.axis));

                    indexA = b2FindSupport(ref f.proxyA, axisA);
                    indexB = b2FindSupport(ref f.proxyB, axisB);

                    B2Vec2 localPointA = f.proxyA.points[indexA];
                    B2Vec2 localPointB = f.proxyB.points[indexB];

                    B2Vec2 pointA = b2TransformPoint(ref xfA, localPointA);
                    B2Vec2 pointB = b2TransformPoint(ref xfB, localPointB);

                    float separation = b2Dot(b2Sub(pointB, pointA), f.axis);
                    return separation;
                }

                case B2SeparationType.b2_faceAType:
                {
                    B2Vec2 normal = b2RotateVector(xfA.q, f.axis);
                    B2Vec2 pointA = b2TransformPoint(ref xfA, f.localPoint);

                    B2Vec2 axisB = b2InvRotateVector(xfB.q, b2Neg(normal));

                    indexA = -1;
                    indexB = b2FindSupport(ref f.proxyB, axisB);

                    B2Vec2 localPointB = f.proxyB.points[indexB];
                    B2Vec2 pointB = b2TransformPoint(ref xfB, localPointB);

                    float separation = b2Dot(b2Sub(pointB, pointA), normal);
                    return separation;
                }

                case B2SeparationType.b2_faceBType:
                {
                    B2Vec2 normal = b2RotateVector(xfB.q, f.axis);
                    B2Vec2 pointB = b2TransformPoint(ref xfB, f.localPoint);

                    B2Vec2 axisA = b2InvRotateVector(xfA.q, b2Neg(normal));

                    indexB = -1;
                    indexA = b2FindSupport(ref f.proxyA, axisA);

                    B2Vec2 localPointA = f.proxyA.points[indexA];
                    B2Vec2 pointA = b2TransformPoint(ref xfA, localPointA);

                    float separation = b2Dot(b2Sub(pointA, pointB), normal);
                    return separation;
                }

                default:
                    B2_ASSERT(false);
                    indexA = -1;
                    indexB = -1;
                    return 0.0f;
            }
        }

        //
        static float b2EvaluateSeparation(B2SeparationFunction f, int indexA, int indexB, float t)
        {
            B2Transform xfA = b2GetSweepTransform(ref f.sweepA, t);
            B2Transform xfB = b2GetSweepTransform(ref f.sweepB, t);

            switch (f.type)
            {
                case B2SeparationType.b2_pointsType:
                {
                    B2Vec2 localPointA = f.proxyA.points[indexA];
                    B2Vec2 localPointB = f.proxyB.points[indexB];

                    B2Vec2 pointA = b2TransformPoint(ref xfA, localPointA);
                    B2Vec2 pointB = b2TransformPoint(ref xfB, localPointB);

                    float separation = b2Dot(b2Sub(pointB, pointA), f.axis);
                    return separation;
                }

                case B2SeparationType.b2_faceAType:
                {
                    B2Vec2 normal = b2RotateVector(xfA.q, f.axis);
                    B2Vec2 pointA = b2TransformPoint(ref xfA, f.localPoint);

                    B2Vec2 localPointB = f.proxyB.points[indexB];
                    B2Vec2 pointB = b2TransformPoint(ref xfB, localPointB);

                    float separation = b2Dot(b2Sub(pointB, pointA), normal);
                    return separation;
                }

                case B2SeparationType.b2_faceBType:
                {
                    B2Vec2 normal = b2RotateVector(xfB.q, f.axis);
                    B2Vec2 pointB = b2TransformPoint(ref xfB, f.localPoint);

                    B2Vec2 localPointA = f.proxyA.points[indexA];
                    B2Vec2 pointA = b2TransformPoint(ref xfA, localPointA);

                    float separation = b2Dot(b2Sub(pointA, pointB), normal);
                    return separation;
                }

                default:
                    B2_ASSERT(false);
                    return 0.0f;
            }
        }

        /// Compute the upper bound on time before two shapes penetrate. Time is represented as
        /// a fraction between [0,tMax]. This uses a swept separating axis and may miss some intermediate,
        /// non-tunneling collisions. If you change the time interval, you should call this function
        /// again.
        // CCD via the local separating axis method. This seeks progression
        // by computing the largest time at which separation is maintained.
        public static B2TOIOutput b2TimeOfImpact(ref B2TOIInput input)
        {
#if B2_SNOOP_TOI_COUNTERS
            ulong ticks = b2GetTicks();
            ++b2_toiCalls;
#endif

            B2TOIOutput output = new B2TOIOutput();
            output.state = B2TOIState.b2_toiStateUnknown;
            output.fraction = input.maxFraction;

            B2Sweep sweepA = input.sweepA;
            B2Sweep sweepB = input.sweepB;
            B2_ASSERT(b2IsNormalizedRot(sweepA.q1) && b2IsNormalizedRot(sweepA.q2));
            B2_ASSERT(b2IsNormalizedRot(sweepB.q1) && b2IsNormalizedRot(sweepB.q2));

            // todo_erin
            // c1 can be at the origin yet the points are far away
            // B2Vec2 origin = b2Add(sweepA.c1, input.proxyA.points[0]);

            ref B2ShapeProxy proxyA = ref input.proxyA;
            ref B2ShapeProxy proxyB = ref input.proxyB;

            float tMax = input.maxFraction;

            float totalRadius = proxyA.radius + proxyB.radius;
            float target = b2MaxFloat(B2_LINEAR_SLOP, totalRadius - B2_LINEAR_SLOP);
            float tolerance = 0.25f * B2_LINEAR_SLOP;
            B2_ASSERT(target > tolerance);

            float t1 = 0.0f;
            const int k_maxIterations = 20;
            int distanceIterations = 0;

            // Prepare input for distance query.
            B2SimplexCache cache = new B2SimplexCache();
            B2DistanceInput distanceInput = new B2DistanceInput();
            distanceInput.proxyA = input.proxyA;
            distanceInput.proxyB = input.proxyB;
            distanceInput.useRadii = false;

            // The outer loop progressively attempts to compute new separating axes.
            // This loop terminates when an axis is repeated (no progress is made).
            for (;;)
            {
                B2Transform xfA = b2GetSweepTransform(ref sweepA, t1);
                B2Transform xfB = b2GetSweepTransform(ref sweepB, t1);

                // Get the distance between shapes. We can also use the results
                // to get a separating axis.
                distanceInput.transformA = xfA;
                distanceInput.transformB = xfB;
                B2DistanceOutput distanceOutput = b2ShapeDistance(ref distanceInput, ref cache, null, 0);

                // Progressive time of impact. This handles slender geometry well but introduces
                // significant time loss.
                // if (distanceIterations == 0)
                //{
                //	if ( distanceOutput.distance > totalRadius + B2_SPECULATIVE_DISTANCE )
                //	{
                //		target = totalRadius + B2_SPECULATIVE_DISTANCE - tolerance;
                //	}
                //	else
                //	{
                //		target = distanceOutput.distance - 1.5f * tolerance;
                //		target = b2MaxFloat( target, 2.0f * tolerance );
                //	}
                //}

                distanceIterations += 1;
#if B2_SNOOP_TOI_COUNTERS
                b2_toiDistanceIterations += 1;
#endif

                // If the shapes are overlapped, we give up on continuous collision.
                if (distanceOutput.distance <= 0.0f)
                {
                    // Failure!
                    output.state = B2TOIState.b2_toiStateOverlapped;
#if B2_SNOOP_TOI_COUNTERS
                    b2_toiOverlappedCount += 1;
#endif
                    output.fraction = 0.0f;
                    break;
                }

                if (distanceOutput.distance <= target + tolerance)
                {
                    // Victory!
                    output.state = B2TOIState.b2_toiStateHit;
#if B2_SNOOP_TOI_COUNTERS
                    b2_toiHitCount += 1;
#endif
                    // Averaged hit point
                    B2Vec2 pA = b2MulAdd(distanceOutput.pointA, proxyA.radius, distanceOutput.normal);
                    B2Vec2 pB = b2MulAdd(distanceOutput.pointB, -proxyB.radius, distanceOutput.normal);
                    output.point = b2Lerp(pA, pB, 0.5f);
                    output.normal = distanceOutput.normal;
                    output.fraction = t1;
                    break;
                }

                // Initialize the separating axis.
                B2SeparationFunction fcn = b2MakeSeparationFunction(ref cache, ref proxyA, ref sweepA, ref proxyB, ref sweepB, t1);
#if FALSE
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
                        output.state = B2TOIState.b2_toiStateSeparated;
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
                        output.state = B2TOIState.b2_toiStateFailed;
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
                        output.state = B2TOIState.b2_toiStateHit;
#if B2_SNOOP_TOI_COUNTERS
                        b2_toiHitCount += 1;
#endif
                        // Averaged hit point
                        B2Vec2 pA = b2MulAdd(distanceOutput.pointA, proxyA.radius, distanceOutput.normal);
                        B2Vec2 pB = b2MulAdd(distanceOutput.pointB, -proxyB.radius, distanceOutput.normal);
                        output.point = b2Lerp(pA, pB, 0.5f);
                        output.normal = distanceOutput.normal;
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
                    b2_toiMaxRootIterations = b2MaxInt(b2_toiMaxRootIterations, rootIterationCount);
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
                    output.state = B2TOIState.b2_toiStateFailed;
#if B2_SNOOP_TOI_COUNTERS
                    b2_toiFailedCount += 1;
#endif
                    // Averaged hit point
                    B2Vec2 pA = b2MulAdd(distanceOutput.pointA, proxyA.radius, distanceOutput.normal);
                    B2Vec2 pB = b2MulAdd(distanceOutput.pointB, -proxyB.radius, distanceOutput.normal);
                    output.point = b2Lerp(pA, pB, 0.5f);
                    output.normal = distanceOutput.normal;
                    output.fraction = t1;
                    break;
                }
            }

#if B2_SNOOP_TOI_COUNTERS
            b2_toiMaxDistanceIterations = b2MaxInt(b2_toiMaxDistanceIterations, distanceIterations);

            float time = b2GetMilliseconds(ticks);
            b2_toiMaxTime = b2MaxFloat(b2_toiMaxTime, time);
            b2_toiTime += time;
#endif

            return output;
        }
    }
}