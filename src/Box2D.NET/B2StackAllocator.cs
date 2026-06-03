// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    // This is a stack-like arena allocator used for fast per step allocations.
    // You must nest allocate/free pairs. The code will B2_ASSERT
    // if you try to interleave multiple allocate/free pairs.
    // This allocator uses the heap if space is insufficient.
    // I could remove the need to free entries individually.
    public class B2StackAllocator
    {
        private readonly object _lock;

        private int _capacity;
        private IB2ArenaAllocatable[] _lookup;
        private IB2ArenaAllocatable[] _allocators;

        public int Count => _allocators.Length;

        public B2StackAllocator(int capacity)
        {
            _lock = new object();
            _capacity = capacity;
            _lookup = Array.Empty<IB2ArenaAllocatable>();
            _allocators = Array.Empty<IB2ArenaAllocatable>();
        }

        public B2Stack<T> GetOrCreateFor<T>() where T : new()
        {
            var index = B2ArenaAllocatorIndexer.Index<T>();
            if (_lookup.Length <= index || null == _lookup[index])
            {
                lock (_lock)
                {
                    // grow
                    if (_lookup.Length <= index)
                    {
                        _lookup = Resize(_lookup, index + 16);
                    }

                    // new 
                    if (null == _lookup[index])
                    {
                        var newAllocator = B2ArenaAllocators.b2CreateStack<T>(_capacity);
                        _lookup[index] = newAllocator;

                        //
                        var tempAllocators = Resize(_allocators, _allocators.Length + 1);
                        tempAllocators[_allocators.Length] = newAllocator;
                        _allocators = tempAllocators;
                    }
                }
            }

            return _lookup[index] as B2Stack<T>;
        }

        private static IB2ArenaAllocatable[] Resize(IB2ArenaAllocatable[] source, int count)
        {
            IB2ArenaAllocatable[] temp = source;
            if (source.Length < count)
            {
                temp = new IB2ArenaAllocatable[count];
            }

            if (0 < source.Length)
            {
                Array.Copy(source, temp, source.Length);
            }

            return temp;
        }

        public Span<IB2ArenaAllocatable> AsSpan()
        {
            return new Span<IB2ArenaAllocatable>(_allocators);
        }
    }
}
