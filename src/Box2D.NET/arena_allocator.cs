// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using static Box2D.NET.array;
using static Box2D.NET.core;


namespace Box2D.NET;

public class b2ArenaEntry<T>
{
    public ArraySegment<T> data;
    public string name;
    public int size;
    public bool usedMalloc;
}

public interface IArenaAllocator
{
    
}

// This is a stack-like arena allocator used for fast per step allocations.
// You must nest allocate/free pairs. The code will Debug.Assert
// if you try to interleave multiple allocate/free pairs.
// This allocator uses the heap if space is insufficient.
// I could remove the need to free entries individually.
public class b2ArenaAllocator<T> : IArenaAllocator
{
    public ArraySegment<T> data;
    public int capacity;
    public int index;

    public int allocation;
    public int maxAllocation;

    public b2Array<b2ArenaEntry<T>> entries;
}

public class b2ArenaAllocator
{
    public b2ArenaAllocator<T> GetAlloc<T>()
    {
        // TODO: @ikpil, check
        return null;
    }

    public IArenaAllocator[] GetAll()
    {
        return null;
    }
}

public class arena_allocator
{
    public static b2ArenaAllocator b2CreateArenaAllocator(int capacity)
    {
        var allocator = new b2ArenaAllocator();
        return allocator;
    }
    
    public static void b2DestroyArenaAllocator(b2ArenaAllocator allocator)
    {
        // Array_Destroy(allocator.entries);
        // b2Free(allocator.data, allocator.capacity);
    }

    public static b2ArenaAllocator<T> b2CreateArenaAllocator<T>(int capacity) where T : new()
    {
        Debug.Assert(capacity >= 0);
        b2ArenaAllocator<T> allocator = new b2ArenaAllocator<T>();
        allocator.capacity = capacity;
        allocator.data = b2Alloc<T>(capacity);
        allocator.allocation = 0;
        allocator.maxAllocation = 0;
        allocator.index = 0;
        allocator.entries = Array_Create<b2ArenaEntry<T>>(32);
        return allocator;
    }

    public static ArraySegment<T> b2AllocateArenaItem<T>(b2ArenaAllocator allocator, int size,  string name ) where T : new()
    {
        var alloc = allocator.GetAlloc<T>();
        // ensure allocation is 32 byte aligned to support 256-bit SIMD
        int size32 = ((size - 1) | 0x1F) + 1;

        b2ArenaEntry<T> entry = new b2ArenaEntry<T>();
        entry.size = size32;
        entry.name = name;
        if (alloc.index + size32 > alloc.capacity)
        {
            // fall back to the heap (undesirable)
            entry.data = b2Alloc<T>(size32);
            entry.usedMalloc = true;

            //Debug.Assert(((uintptr_t)entry.data & 0x1F) == 0);
        }
        else
        {
            entry.data = alloc.data.Slice(alloc.index, size32);
            entry.usedMalloc = false;
            alloc.index += size32;

            //Debug.Assert(((uintptr_t)entry.data & 0x1F) == 0);
        }

        alloc.allocation += size32;
        if (alloc.allocation > alloc.maxAllocation)
        {
            alloc.maxAllocation = alloc.allocation;
        }

        Array_Push(alloc.entries, entry);
        return entry.data;
    }

    public static void b2FreeArenaItem<T>(b2ArenaAllocator allocator, ArraySegment<T> mem)
    {
        var alloc = allocator.GetAlloc<T>();
        int entryCount = alloc.entries.count;
        Debug.Assert(entryCount > 0);
        b2ArenaEntry<T> entry = alloc.entries.data[entryCount - 1];
        Debug.Assert(mem == entry.data);
        if (entry.usedMalloc)
        {
            b2Free(mem.Array, entry.size);
        }
        else
        {
            alloc.index -= entry.size;
        }

        alloc.allocation -= entry.size;
        Array_Pop(alloc.entries);
    }

    public static void b2GrowArena(b2ArenaAllocator allocator)
    {
        var allocs = allocator.GetAll();

        for (int i = 0; i < allocs.Length; ++i)
        {
            var alloc = allocs[i];
            // TODO: @ikpil. check
            // // Stack must not be in use
            // Debug.Assert(alloc.allocation == 0);
            //
            // if (alloc.maxAllocation > alloc.capacity)
            // {
            //     b2Free(alloc.data.Array, alloc.capacity);
            //     alloc.capacity = alloc.maxAllocation + alloc.maxAllocation / 2;
            //     alloc.data = b2Alloc<T>(alloc.capacity);
            // }
        }
    }

    // Grow the arena based on usage
    public static int b2GetArenaCapacity(b2ArenaAllocator allocator)
    {
        // var alloc = allocator.GetAlloc<T>();
        // return alloc.capacity;
        return -1;
    }

    public static int b2GetArenaAllocation(b2ArenaAllocator allocator)
    {
        // var alloc = allocator.GetAlloc<T>();
        // return alloc.allocation;
        return -1;
    }

    public static int b2GetMaxArenaAllocation(b2ArenaAllocator allocator)
    {
        // var alloc = allocator.GetAlloc<T>();
        // return alloc.maxAllocation;
        return -1;
    }
}