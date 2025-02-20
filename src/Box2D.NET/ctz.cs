// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public static class ctz
    {
        // uint에 대해 trailing zero count (CTZ)
        public static uint b2CTZ32(uint block)
        {
            if (block == 0) return 32;
            uint count = 0;
            while ((block & 1) == 0)
            {
                count++;
                block >>= 1;
            }
            return count;
        }

        // uint에 대해 leading zero count (CLZ)
        public static uint b2CLZ32(uint value)
        {
            if (value == 0) return 32;
            uint count = 0;
            uint mask = 1u << 31;
            while ((value & mask) == 0)
            {
                count++;
                mask >>= 1;
            }
            return count;
        }

        // ulong에 대해 trailing zero count (CTZ)
        public static uint b2CTZ64(ulong block)
        {
            if (block == 0) return 64;
            uint count = 0;
            while ((block & 1) == 0)
            {
                count++;
                block >>= 1;
            }
            return count;
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