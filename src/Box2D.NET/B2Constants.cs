// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Cores;

namespace Box2D.NET
{
    public static class B2Constants
    {
        // Used to detect bad values. Positions greater than about 16km will have precision
        // problems, so 100km as a limit should be fine in all cases.
        public static float B2_HUGE => (100000.0f * b2_lengthUnitsPerMeter);

        // Maximum parallel workers. Used to size some static arrays.
        public const int B2_MAX_WORKERS = 64;

        // Maximum number of colors in the constraint graph. Constraints that cannot
        // find a color are added to the overflow set which are solved single-threaded.
        public const int B2_GRAPH_COLOR_COUNT = 12;

        // A small length used as a collision and constraint tolerance. Usually it is
        // chosen to be numerically significant, but visually insignificant. In meters.
        // Normally this is 0.5cm.
        // @warning modifying this can have a significant impact on stability
        public static float B2_LINEAR_SLOP => (0.005f * b2_lengthUnitsPerMeter);

        // Maximum number of simultaneous worlds that can be allocated
        public const int B2_MAX_WORLDS = 128;

        // The maximum rotation of a body per time step. This limit is very large and is used
        // to prevent numerical problems. You shouldn't need to adjust this.
        // @warning increasing this to 0.5f * b2_pi or greater will break continuous collision.
        public static readonly float B2_MAX_ROTATION = (0.25f * B2MathFunction.B2_PI);

        // Box2D uses limited speculative collision. This reduces jitter.
        // Normally this is 2cm.
        // @warning modifying this can have a significant impact on performance and stability
        public static readonly float B2_SPECULATIVE_DISTANCE = (4.0f * B2_LINEAR_SLOP);

        // This is used to fatten AABBs in the dynamic tree. This allows proxies
        // to move by a small amount without triggering a tree adjustment. This is in meters.
        // Normally this is 5cm.
        // @warning modifying this can have a significant impact on performance
        public static float B2_AABB_MARGIN => (0.05f * b2_lengthUnitsPerMeter);

        // The time that a body must be still before it will go to sleep. In seconds.
        public const float B2_TIME_TO_SLEEP = 0.5f;
        
        // solver
        public const int B2_MAX_CONTINUOUS_SENSOR_HITS = 8;
        
        // collision
        // -----------------------------------------------------------------------------------------------------------------
        /// The maximum number of vertices on a convex polygon. Changing this affects performance even if you
        /// don't use more vertices.
        public const int B2_MAX_POLYGON_VERTICES = 8;


        // types
        public const ulong B2_DEFAULT_CATEGORY_BITS = 1;
        public const ulong B2_DEFAULT_MASK_BITS = ulong.MaxValue;

        // core
        public const int B2_NULL_INDEX = -1;

        // Use 32 byte alignment for everything. Works with 256bit SIMD.
        public const int B2_ALIGNMENT = 32;

        // Use to validate definitions. Do not take my cookie.
        public const int B2_SECRET_COOKIE = 1152023;


        // @base
        /// Simple djb2 hash function for determinism testing
        public const int B2_HASH_INIT = 5381;
    }
}