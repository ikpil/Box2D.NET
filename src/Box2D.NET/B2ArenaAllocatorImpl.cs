// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using static Box2D.NET.B2Cores;

namespace Box2D.NET
{
    // This is a stack-like arena allocator used for fast per step allocations.
    // You must nest allocate/free pairs. The code will Debug.Assert
    // if you try to interleave multiple allocate/free pairs.
    // This allocator uses the heap if space is insufficient.
    // I could remove the need to free entries individually.
    public class B2ArenaAllocatorImpl<T> : IB2ArenaAllocatable where T : new()
    {
        public ArraySegment<T> data;
        public int capacity { get; set; }
        public int index { get; set; }
        public int allocation { get; set; }
        public int maxAllocation { get; set; }

        public B2Array<B2ArenaEntry<T>> entries;

        public int Grow()
        {
            // Stack must not be in use
            Debug.Assert(allocation == 0);

            if (maxAllocation > capacity)
            {
                b2Free(data.Array, capacity);
                capacity = maxAllocation + maxAllocation / 2;
                data = b2Alloc<T>(capacity);
            }

            return capacity;
        }
    }
}