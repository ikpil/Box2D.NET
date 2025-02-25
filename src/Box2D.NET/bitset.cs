// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.core;

namespace Box2D.NET
{
    public static class bitset
    {
        public static void b2SetBit(b2BitSet bitSet, int bitIndex)
        {
            int blockIndex = bitIndex / 64;
            Debug.Assert(blockIndex < bitSet.blockCount);
            bitSet.bits[blockIndex] |= ((ulong)1 << (bitIndex % 64));
        }

        public static void b2SetBitGrow(b2BitSet bitSet, int bitIndex)
        {
            int blockIndex = bitIndex / 64;
            if (blockIndex >= bitSet.blockCount)
            {
                b2GrowBitSet(bitSet, blockIndex + 1);
            }

            bitSet.bits[blockIndex] |= ((ulong)1 << (int)(bitIndex % 64));
        }

        public static void b2ClearBit(b2BitSet bitSet, uint bitIndex)
        {
            uint blockIndex = bitIndex / 64;
            if (blockIndex >= bitSet.blockCount)
            {
                return;
            }

            bitSet.bits[blockIndex] &= ~((ulong)1 << (int)(bitIndex % 64));
        }

        public static bool b2GetBit(b2BitSet bitSet, int bitIndex)
        {
            int blockIndex = bitIndex / 64;
            if (blockIndex >= bitSet.blockCount)
            {
                return false;
            }

            return (bitSet.bits[blockIndex] & ((ulong)1 << (int)(bitIndex % 64))) != 0;
        }

        public static int b2GetBitSetBytes(b2BitSet bitSet)
        {
            return (int)(bitSet.blockCapacity * sizeof(ulong));
        }


        public static b2BitSet b2CreateBitSet(int bitCapacity)
        {
            b2BitSet bitSet = new b2BitSet();

            bitSet.blockCapacity = (bitCapacity + sizeof(ulong) * 8 - 1) / (sizeof(ulong) * 8);
            bitSet.blockCount = 0;
            bitSet.bits = b2Alloc<ulong>(bitSet.blockCapacity);
            //memset( bitSet.bits, 0, bitSet.blockCapacity * sizeof( ulong ) );
            Array.Fill(bitSet.bits, 0UL);
            return bitSet;
        }

        public static void b2DestroyBitSet(b2BitSet bitSet)
        {
            b2Free(bitSet.bits, bitSet.blockCapacity * sizeof(ulong));
            bitSet.blockCapacity = 0;
            bitSet.blockCount = 0;
            bitSet.bits = null;
        }

        public static void b2SetBitCountAndClear(b2BitSet bitSet, int bitCount)
        {
            int blockCount = (bitCount + sizeof(ulong) * 8 - 1) / (sizeof(ulong) * 8);
            if (bitSet.blockCapacity < blockCount)
            {
                b2DestroyBitSet(bitSet);
                int newBitCapacity = bitCount + (bitCount >> 1);
                bitSet = b2CreateBitSet(newBitCapacity);
            }

            bitSet.blockCount = blockCount;
            //memset( bitSet->bits, 0, bitSet->blockCount * sizeof( ulong ) );
            Array.Fill(bitSet.bits, 0UL, 0, bitSet.blockCount);
        }

        public static void b2GrowBitSet(b2BitSet bitSet, int blockCount)
        {
            Debug.Assert(blockCount > bitSet.blockCount);
            if (blockCount > bitSet.blockCapacity)
            {
                int oldCapacity = bitSet.blockCapacity;
                bitSet.blockCapacity = blockCount + blockCount / 2;
                ulong[] newBits = b2Alloc<ulong>(bitSet.blockCapacity);
                //memset( newBits, 0, bitSet->blockCapacity * sizeof( ulong ) );
                Array.Fill(newBits, 0UL, 0, bitSet.blockCapacity);
                Debug.Assert(bitSet.bits != null);
                //memcpy( newBits, bitSet->bits, oldCapacity * sizeof( ulong ) );
                Array.Copy(bitSet.bits, newBits, oldCapacity);
                b2Free(bitSet.bits, oldCapacity);
                bitSet.bits = newBits;
            }

            bitSet.blockCount = blockCount;
        }

        public static void b2InPlaceUnion(b2BitSet setA, b2BitSet setB)
        {
            Debug.Assert(setA.blockCount == setB.blockCount);
            int blockCount = setA.blockCount;
            for (uint i = 0; i < blockCount; ++i)
            {
                setA.bits[i] |= setB.bits[i];
            }
        }
    }
}
