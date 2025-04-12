// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    public class B2ArenaAllocator
    {
        private readonly object _lock;

        private int _capacity;
        private IB2ArenaAllocatable[] _allocators;
        private int _allocatorCount;

        public int Count => _allocatorCount;

        public B2ArenaAllocator(int capacity)
        {
            _lock = new object();
            _capacity = capacity;
            _allocators = Array.Empty<IB2ArenaAllocatable>();
            _allocatorCount = 0;
        }

        public B2ArenaAllocatorTyped<T> GetOrCreateFor<T>() where T : new()
        {
            var index = B2ArenaAllocatorIndexer.Index<T>();
            if (_allocators.Length <= index)
            {
                lock (_lock)
                {
                    // grow
                    if (_allocators.Length <= index)
                    {
                        IB2ArenaAllocatable[] temp = new IB2ArenaAllocatable[index + 16];
                        if (0 < _allocators.Length)
                        {
                            Array.Copy(_allocators, temp, _allocators.Length);
                        }

                        _allocators = temp;
                    }
                }
            }

            if (null == _allocators[index])
            {
                lock (_lock)
                {
                    // new 
                    if (null == _allocators[index])
                    {
                        _allocators[index] = B2ArenaAllocators.b2CreateArenaAllocator<T>(_capacity);
                        _allocatorCount++;
                    }
                }
            }

            return _allocators[index] as B2ArenaAllocatorTyped<T>;
        }

        public Span<IB2ArenaAllocatable> AsSpan()
        {
            return new Span<IB2ArenaAllocatable>(_allocators, 0, _allocatorCount);
        }
    }
}