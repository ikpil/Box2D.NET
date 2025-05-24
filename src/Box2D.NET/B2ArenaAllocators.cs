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
        public static B2ArenaAllocator b2CreateArenaAllocator(int capacity)
        {
            var allocator = new B2ArenaAllocator(capacity);
            return allocator;
        }

        public static void b2DestroyArenaAllocator(B2ArenaAllocator allocator)
        {
            var allocs = allocator.AsSpan();
            for (int i = 0; i < allocs.Length; ++i)
            {
                allocs[i].Destroy();
            }
        }

        public static B2ArenaAllocatorTyped<T> b2CreateArenaAllocator<T>(int capacity) where T : new()
        {
            B2_ASSERT(capacity >= 0);
            B2ArenaAllocatorTyped<T> allocatorImpl = new B2ArenaAllocatorTyped<T>();
            allocatorImpl.capacity = capacity;
            allocatorImpl.data = b2Alloc<T>(capacity);
            allocatorImpl.allocation = 0;
            allocatorImpl.maxAllocation = 0;
            allocatorImpl.index = 0;
            allocatorImpl.entries = b2Array_Create<B2ArenaEntry<T>>(capacity);
            return allocatorImpl;
        }

        public static ArraySegment<T> b2AllocateArenaItem<T>(B2ArenaAllocator allocator, int size, string name) where T : new()
        {
            var alloc = allocator.GetOrCreateFor<T>();
            // ensure allocation is 32 byte aligned to support 256-bit SIMD
            int size32 = ((size - 1) | 0x1F) + 1;

            B2ArenaEntry<T> entry = new B2ArenaEntry<T>();
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

        public static void b2FreeArenaItem<T>(B2ArenaAllocator allocator, ArraySegment<T> mem) where T : new()
        {
            var alloc = allocator.GetOrCreateFor<T>();
            int entryCount = alloc.entries.count;
            B2_ASSERT(entryCount > 0);
            ref B2ArenaEntry<T> entry = ref alloc.entries.data[entryCount - 1];
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

        public static void b2GrowArena(B2ArenaAllocator allocator)
        {
            var allocSpan = allocator.AsSpan();

            for (int i = 0; i < allocSpan.Length; ++i)
            {
                var alloc = allocSpan[i];
                alloc.Grow();
            }
        }

        // Grow the arena based on usage
        public static int b2GetArenaCapacity(B2ArenaAllocator allocator)
        {
            int capacity = 0;
            var allocSpan = allocator.AsSpan();
            for (int i = 0; i < allocSpan.Length; ++i)
            {
                capacity += allocSpan[i].maxAllocation;
            }

            return capacity;
        }

        public static int b2GetArenaAllocation(B2ArenaAllocator allocator)
        {
            int allocation = 0;
            var allocSpan = allocator.AsSpan();
            for (int i = 0; i < allocSpan.Length; ++i)
            {
                allocation += allocSpan[i].allocation;
            }

            return allocation;
        }

        public static int b2GetMaxArenaAllocation(B2ArenaAllocator allocator)
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