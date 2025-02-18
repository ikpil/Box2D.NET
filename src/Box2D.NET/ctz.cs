// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    public static class ctz
    {
        // uint에 대해 trailing zero count (CTZ)
        public static uint b2CTZ32(uint block)
        {
            return (uint)BitOperations.TrailingZeroCount(block);
        }

        // uint에 대해 leading zero count (CLZ)
        public static uint b2CLZ32(uint value)
        {
            return (uint)BitOperations.LeadingZeroCount(value);
        }

        // ulong에 대해 trailing zero count (CTZ)
        public static uint b2CTZ64(ulong block)
        {
            return (uint)BitOperations.TrailingZeroCount(block);
        }

        public static bool b2IsPowerOf2(int x)
        {
            return (x & (x - 1)) == 0;
        }

        public static int b2BoundingPowerOf2(int x)
        {
            if (x <= 1)
            {
                return 1;
            }

            return 32 - (int)b2CLZ32((uint)x - 1);
        }

        public static int b2RoundUpPowerOf2(int x)
        {
            if (x <= 1)
            {
                return 1;
            }

            return 1 << (32 - (int)b2CLZ32((uint)x - 1));
        }
    }
}