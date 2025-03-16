// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    public class B2ArenaAllocator
    {
        private readonly object _lock;
        private int _count;
        private IB2ArenaAllocatable[] _allocators;

        public B2ArenaAllocator()
        {
            _lock = new object();
            _allocators = Array.Empty<IB2ArenaAllocatable>();
        }

        public B2ArenaAllocatorImpl<T> Touch<T>() where T : new()
        {
            var index = B2ArenaAllocatorIndexer.Index<T>();
            if (_allocators.Length <= index || null == _allocators[index])
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

                    // new 
                    if (null == _allocators[index])
                    {
                        _allocators[index] = B2ArenaAllocators.b2CreateArenaAllocator<T>(16);
                        _count++;
                    }
                }
            }

            return _allocators[index] as B2ArenaAllocatorImpl<T>;
        }

        public Span<IB2ArenaAllocatable> AsSpan()
        {
            return new Span<IB2ArenaAllocatable>(_allocators, 0, _count);
        }
    }
}