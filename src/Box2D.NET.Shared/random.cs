// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT
using System;
using Box2D.NET.Primitives;
using static Box2D.NET.constants;
using static Box2D.NET.geometry;
using static Box2D.NET.hull;


namespace Box2D.NET.Shared
{
    public static class random
    {
        // Global seed for simple random number generator.
        public static uint g_seed = RAND_SEED;
        public const int RAND_LIMIT = 32767;
        public const int RAND_SEED = 12345;

        // Simple random number generator. Using this instead of rand() for cross platform determinism.
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
        public static float RandomIntRange(int lo, int hi)
        {
            return lo + RandomInt() % (hi - lo + 1);
        }

        // Random number in range [-1,1]
        public static float RandomFloat()
        {
            float r = (float)(RandomInt() & (RAND_LIMIT));
            r /= RAND_LIMIT;
            r = 2.0f * r - 1.0f;
            return r;
        }

        // Random floating point number in range [lo, hi]
        public static float RandomFloatRange(float lo, float hi)
        {
            float r = (float)(RandomInt() & (RAND_LIMIT));
            r /= RAND_LIMIT;
            r = (hi - lo) * r + lo;
            return r;
        }

        // Random vector with coordinates in range [lo, hi]
        public static b2Vec2 RandomVec2(float lo, float hi)
        {
            b2Vec2 v;
            v.x = RandomFloatRange(lo, hi);
            v.y = RandomFloatRange(lo, hi);
            return v;
        }


        public static b2Polygon RandomPolygon(float extent)
        {
            Span<b2Vec2> points = stackalloc b2Vec2[B2_MAX_POLYGON_VERTICES];
            
            int count = 3 + RandomInt() % 6;
            for (int i = 0; i < count; ++i)
            {
                points[i] = RandomVec2(-extent, extent);
            }

            b2Hull hull = b2ComputeHull(points, count);
            if (hull.count > 0)
            {
                return b2MakePolygon(hull, 0.0f);
            }

            return b2MakeSquare(extent);
        }

    }
}