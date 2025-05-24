// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Buffers;

namespace Box2D.NET
{
    public static class B2BitSets
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2SetBit(ref B2BitSet bitSet, int bitIndex)
        {
            int blockIndex = bitIndex / 64;
            B2_ASSERT(blockIndex < bitSet.blockCount);
            bitSet.bits[blockIndex] |= ((ulong)1 << (bitIndex % 64));
        }

        public static void b2SetBitGrow(ref B2BitSet bitSet, int bitIndex)
        {
            int blockIndex = bitIndex / 64;
            if (blockIndex >= bitSet.blockCount)
            {
                b2GrowBitSet(ref bitSet, blockIndex + 1);
            }

            bitSet.bits[blockIndex] |= ((ulong)1 << (int)(bitIndex % 64));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2ClearBit(ref B2BitSet bitSet, uint bitIndex)
        {
            uint blockIndex = bitIndex / 64;
            if (blockIndex >= bitSet.blockCount)
            {
                return;
            }

            bitSet.bits[blockIndex] &= ~((ulong)1 << (int)(bitIndex % 64));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2GetBit(ref B2BitSet bitSet, int bitIndex)
        {
            int blockIndex = bitIndex / 64;
            if (blockIndex >= bitSet.blockCount)
            {
                return false;
            }

            return (bitSet.bits[blockIndex] & ((ulong)1 << (int)(bitIndex % 64))) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2GetBitSetBytes(ref B2BitSet bitSet)
        {
            return bitSet.blockCapacity * sizeof(ulong);
        }

        public static B2BitSet b2CreateBitSet(int bitCapacity)
        {
            B2BitSet bitSet = new B2BitSet();
            b2CreateBitSet(ref bitSet, bitCapacity);
            return bitSet;
        }

        // fix
        public static void b2CreateBitSet(ref B2BitSet bitSet, int bitCapacity)
        {
            bitSet.blockCapacity = (bitCapacity + sizeof(ulong) * 8 - 1) / (sizeof(ulong) * 8);
            bitSet.blockCount = 0;
            bitSet.bits = b2Alloc<ulong>(bitSet.blockCapacity);
            //memset( bitSet.bits, 0, bitSet.blockCapacity * sizeof( ulong ) );
            Array.Fill(bitSet.bits, 0UL);
        }

        public static void b2DestroyBitSet(ref B2BitSet bitSet)
        {
            b2Free(bitSet.bits, bitSet.blockCapacity);
            bitSet.blockCapacity = 0;
            bitSet.blockCount = 0;
            bitSet.bits = null;
        }

        public static void b2SetBitCountAndClear(ref B2BitSet bitSet, int bitCount)
        {
            int blockCount = (bitCount + sizeof(ulong) * 8 - 1) / (sizeof(ulong) * 8);
            if (bitSet.blockCapacity < blockCount)
            {
                b2DestroyBitSet(ref bitSet);
                int newBitCapacity = bitCount + (bitCount >> 1);
                // @ikpil - reuse!
                b2CreateBitSet(ref bitSet, newBitCapacity);
            }

            bitSet.blockCount = blockCount;
            //memset( bitSet->bits, 0, bitSet->blockCount * sizeof( ulong ) );
            Array.Fill(bitSet.bits, 0UL, 0, bitSet.blockCount);
        }

        public static void b2GrowBitSet(ref B2BitSet bitSet, int blockCount)
        {
            if (blockCount <= bitSet.blockCount)
            {
                throw new InvalidOperationException($"blockCount must be greater than the current blockCount - request({blockCount}) blockCount({bitSet.blockCount})");
            }

            if (blockCount > bitSet.blockCapacity)
            {
                int oldCapacity = bitSet.blockCapacity;
                bitSet.blockCapacity = blockCount + blockCount / 2;
                ulong[] newBits = b2Alloc<ulong>(bitSet.blockCapacity);
                //memset( newBits, 0, bitSet->blockCapacity * sizeof( ulong ) );
                Array.Fill(newBits, 0UL, 0, bitSet.blockCapacity);
                B2_ASSERT(bitSet.bits != null);
                //memcpy( newBits, bitSet->bits, oldCapacity * sizeof( ulong ) );
                Array.Copy(bitSet.bits, newBits, oldCapacity);
                b2Free(bitSet.bits, oldCapacity);
                bitSet.bits = newBits;
            }

            bitSet.blockCount = blockCount;
        }

        public static void b2InPlaceUnion(ref B2BitSet setA, ref B2BitSet setB)
        {
            if (setA.blockCount != setB.blockCount)
            {
                throw new InvalidOperationException($"cannot perform union: setA.blockCount ({setA.blockCount}) != setB.blockCount ({setB.blockCount})");
            }

            int blockCount = setA.blockCount;
            for (uint i = 0; i < blockCount; ++i)
            {
                setA.bits[i] |= setB.bits[i];
            }
        }
    }
}