// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Buffers;

namespace Box2D.NET
{
    public static class B2ArenaAllocators
    {
        public static B2StackAllocator b2CreateStackAllocator(int capacity)
        {
            var allocator = new B2StackAllocator(capacity);
            return allocator;
        }

        public static void b2DestroyStackAllocator(B2StackAllocator allocator)
        {
            var allocs = allocator.AsSpan();
            for (int i = 0; i < allocs.Length; ++i)
            {
                allocs[i].Destroy();
            }
        }

        public static B2Stack<T> b2CreateStack<T>(int capacity) where T : new()
        {
            B2_ASSERT(capacity >= 0);
            B2Stack<T> allocatorImpl = new B2Stack<T>();
            allocatorImpl.capacity = capacity;
            allocatorImpl.data = b2Alloc<T>(capacity);
            allocatorImpl.allocation = 0;
            allocatorImpl.maxAllocation = 0;
            allocatorImpl.index = 0;
            allocatorImpl.entries = b2Array_Create<B2StackEntry<T>>(capacity);
            return allocatorImpl;
        }

        public static ArraySegment<T> b2StackAlloc<T>(B2StackAllocator allocator, int size, string name) where T : new()
        {
            var alloc = allocator.GetOrCreateFor<T>();
            // ensure allocation is 32 byte aligned to support 256-bit SIMD
            int size32 = ((size - 1) | 0x1F) + 1;

            B2StackEntry<T> entry = new B2StackEntry<T>();
            entry.size = size32;
            entry.name = name;
            if (alloc.index + size32 > alloc.capacity)
            {
                // fall back to the heap (undesirable)
                entry.data = b2Alloc<T>(size32);
                entry.usedMalloc = true;

                //B2_ASSERT(((uintptr_t)entry.data & 0x1F) == 0);
            }
            else
            {
                entry.data = alloc.data.Slice(alloc.index, size32);
                entry.usedMalloc = false;
                alloc.index += size32;

                //B2_ASSERT(((uintptr_t)entry.data & 0x1F) == 0);
            }

            alloc.allocation += size32;
            if (alloc.allocation > alloc.maxAllocation)
            {
                alloc.maxAllocation = alloc.allocation;
            }

            b2Array_Push(ref alloc.entries, entry);
            return entry.data;
        }

        public static void b2StackFree<T>(B2StackAllocator allocator, ArraySegment<T> mem) where T : new()
        {
            var alloc = allocator.GetOrCreateFor<T>();
            int entryCount = alloc.entries.count;
            B2_ASSERT(entryCount > 0);
            ref B2StackEntry<T> entry = ref alloc.entries.data[entryCount - 1];
            B2_ASSERT(mem == entry.data);
            if (entry.usedMalloc)
            {
                b2Free(mem.Array, entry.size);
            }
            else
            {
                alloc.index -= entry.size;
            }

            alloc.allocation -= entry.size;
            b2Array_Pop(ref alloc.entries);
        }
        // Grow the stack based on usage
        public static void b2GrowStack(B2StackAllocator allocator)
        {
            var allocSpan = allocator.AsSpan();

            for (int i = 0; i < allocSpan.Length; ++i)
            {
                var alloc = allocSpan[i];
                alloc.Grow();
            }
        }

        // Grow the stack based on usage
        public static int b2GetStackCapacity(B2StackAllocator allocator)
        {
            int capacity = 0;
            var allocSpan = allocator.AsSpan();
            for (int i = 0; i < allocSpan.Length; ++i)
            {
                capacity += allocSpan[i].maxAllocation;
            }

            return capacity;
        }

        public static int b2GetStackAllocation(B2StackAllocator allocator)
        {
            int allocation = 0;
            var allocSpan = allocator.AsSpan();
            for (int i = 0; i < allocSpan.Length; ++i)
            {
                allocation += allocSpan[i].allocation;
            }

            return allocation;
        }

        public static int b2GetMaxStackAllocation(B2StackAllocator allocator)
        {
            int maxAllocation = 0;
            var allocSpan = allocator.AsSpan();
            for (int i = 0; i < allocSpan.Length; ++i)
            {
                maxAllocation += allocSpan[i].maxAllocation;
            }

            return maxAllocation;
        }
    }
}
