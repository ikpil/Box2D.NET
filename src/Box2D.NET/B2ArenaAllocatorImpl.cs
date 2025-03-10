﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    // This is a stack-like arena allocator used for fast per step allocations.
    // You must nest allocate/free pairs. The code will Debug.Assert
    // if you try to interleave multiple allocate/free pairs.
    // This allocator uses the heap if space is insufficient.
    // I could remove the need to free entries individually.
    public class B2ArenaAllocatorImpl<T> : IB2ArenaAllocatable where T : new()
    {
        private static readonly object _lock = new object();
        private static B2ArenaAllocatorImpl<T> _shared;

        public ArraySegment<T> data;
        public int capacity { get; set; }
        public int index { get; set; }
        public int allocation { get; set; }
        public int maxAllocation { get; set; }

        public B2Array<B2ArenaEntry<T>> entries;

        public static B2ArenaAllocatorImpl<T> Touch(B2ArenaAllocator allocator)
        {
            if (null == _shared)
            {
                lock (_lock)
                {
                    if (null == _shared)
                    {
                        _shared = B2ArenaAllocators.b2CreateArenaAllocator<T>(16); // TODO: @ikpil, test
                        allocator.Add(_shared);
                    }
                }
            }

            return _shared;
        }
    }

    public class B2ArenaAllocator
    {
        private readonly object _lock;
        private IB2ArenaAllocatable[] _allocators;

        public B2ArenaAllocator()
        {
            _lock = new object();
            _allocators = Array.Empty<IB2ArenaAllocatable>();
        }

        public B2ArenaAllocatorImpl<T> Touch<T>() where T : new()
        {
            return B2ArenaAllocatorImpl<T>.Touch(this);
        }

        public void Add<T>(B2ArenaAllocatorImpl<T> alloc) where T : new()
        {
            lock (_lock)
            {
                IB2ArenaAllocatable[] temp = new IB2ArenaAllocatable[_allocators.Length + 1];
                if (0 < _allocators.Length)
                {
                    Array.Copy(_allocators, temp, _allocators.Length);
                }

                temp[_allocators.Length] = alloc;
                _allocators = temp;
            }
        }

        public IB2ArenaAllocatable[] AsArray()
        {
            return _allocators;
        }
    }
}
