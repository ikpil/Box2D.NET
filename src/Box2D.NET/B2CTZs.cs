// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;

namespace Box2D.NET
{
    public static class B2CTZs
    {
        // De Bruijn magic constant and lookup table
        private static readonly byte[] SampleMultiplyDeBruijnBitPosition = new byte[32]
        {
            0, 1, 28, 2, 29, 14, 24, 3,
            30, 22, 20, 15, 25, 17, 4, 8,
            31, 27, 13, 23, 21, 19, 16, 7,
            26, 12, 18, 6, 11, 5, 10, 9
        };

        private static readonly byte[] SampleMultiplyDeBruijnBitPosition64 = new byte[64]
        {
            63, 0, 1, 52, 2, 6, 53, 26, 3, 37, 40, 7, 33, 54, 47, 27,
            61, 4, 38, 45, 43, 41, 21, 8, 23, 34, 58, 55, 48, 17, 28, 10,
            62, 51, 5, 25, 36, 39, 32, 46, 60, 44, 42, 20, 22, 57, 16, 9,
            50, 24, 35, 31, 59, 19, 56, 15, 49, 30, 18, 14, 29, 13, 12, 11
        };

        private static readonly byte[] SampleClzTable = new byte[32]
        {
            31, 22, 30, 21, 18, 10, 29, 2,
            20, 17, 15, 13, 9, 6, 28, 1,
            23, 19, 11, 3, 16, 14, 7, 24,
            12, 4, 8, 25, 5, 26, 27, 0
        };


        // uint에 대해 trailing zero count (CTZ)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint b2CTZ32_DeRrujin(uint block)
        {
            if (block == 0) return 32;
        
            // isolate lowest set bit and multiply
            uint idx = (uint)((block & -block) * 0x077CB531u) >> 27;
            return SampleMultiplyDeBruijnBitPosition[idx];
        }

        // uint에 대해 leading zero count (CLZ)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint b2CLZ32(uint block)
        {
            if (block == 0) return 32;

            block |= block >> 1;
            block |= block >> 2;
            block |= block >> 4;
            block |= block >> 8;
            block |= block >> 16;

            uint idx = (block * 0x07C4ACDDu) >> 27;
            return SampleClzTable[idx];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint b2CTZ64_DeBrujin(ulong x)
        {
            if (x == 0)
                return 64;
        
            // x & -x : ulong 음수 연산 불가 → ~x + 1 사용
            ulong isolated = x & (~x + 1UL);
        
            int index = (int)((isolated * 0x045FBAC7992A70DAUL) >> 58);
        
            return SampleMultiplyDeBruijnBitPosition64[index];
        }
        
        // uint에 대해 trailing zero count (CTZ)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint b2CLZ32_Old(uint value)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2PopCount64(ulong block)
        {
            int count = 0;
            while (block != 0)
            {
                count += (int)(block & 1);
                block >>= 1;
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2IsPowerOf2(int x)
        {
            return (x & (x - 1)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2BoundingPowerOf2(int x)
        {
            if (x <= 1)
            {
                return 1;
            }

            return 32 - (int)b2CLZ32((uint)x - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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