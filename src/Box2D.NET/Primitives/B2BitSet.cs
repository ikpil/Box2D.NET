﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    // Bit set provides fast operations on large arrays of bits.
    public class B2BitSet
    {
        public ulong[] bits;
        public int blockCapacity;
        public int blockCount;
    }
}
