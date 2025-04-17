// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2MathFunction;


namespace Box2D.NET.Shared
{
    public static class RandomSupports
    {
        // Global seed for simple random number generator.
        public static uint g_seed = RAND_SEED;
        public const int RAND_LIMIT = 32767;
        public const int RAND_SEED = 12345;

        // Simple random number generator. Using this instead of rand() for cross-platform determinism.
        public static int RandomInt()
        {
            // XorShift32 algorithm
            uint x = g_seed;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            g_seed = x;

            // Map the 32-bit value to the range 0 to RAND_LIMIT
            return (int)(x % (RAND_LIMIT + 1));
        }

        // Random integer in range [lo, hi]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RandomIntRange(int lo, int hi)
        {
            return lo + RandomInt() % (hi - lo + 1);
        }

        // Random number in range [-1,1]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RandomFloat()
        {
            float r = (float)(RandomInt() & (RAND_LIMIT));
            r /= RAND_LIMIT;
            r = 2.0f * r - 1.0f;
            return r;
        }

        // Random floating point number in range [lo, hi]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RandomFloatRange(float lo, float hi)
        {
            float r = (float)(RandomInt() & (RAND_LIMIT));
            r /= RAND_LIMIT;
            r = (hi - lo) * r + lo;
            return r;
        }

        // Random vector with coordinates in range [lo, hi]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2 RandomVec2(float lo, float hi)
        {
            B2Vec2 v;
            v.X = RandomFloatRange(lo, hi);
            v.Y = RandomFloatRange(lo, hi);
            return v;
        }

        // Random rotation with angle in range [-pi, pi]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Rot RandomRot()
        {
            float angle = RandomFloatRange(-B2_PI, B2_PI);
            return b2MakeRot(angle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Polygon RandomPolygon(float extent)
        {
            Span<B2Vec2> points = stackalloc B2Vec2[B2_MAX_POLYGON_VERTICES];

            int count = 3 + RandomInt() % 6;
            for (int i = 0; i < count; ++i)
            {
                points[i] = RandomVec2(-extent, extent);
            }

            B2Hull hull = b2ComputeHull(points, count);
            if (hull.count > 0)
            {
                return b2MakePolygon(ref hull, 0.0f);
            }

            return b2MakeSquare(extent);
        }
    }
}