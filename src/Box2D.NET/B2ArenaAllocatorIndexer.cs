// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Atomics;

namespace Box2D.NET
{
    internal class B2ArenaAllocatorIndexer
    {
        private static B2AtomicInt _indices;

        internal static int Next<T>()
        {
            return b2AtomicFetchAddInt(ref _indices, 1);
        }

        public static int Index<T>() where T : new()
        {
            return B2ArenaAllocatorIndexer<T>.Index;
        }
    }

    internal class B2ArenaAllocatorIndexer<T> where T : new()
    {
        public static readonly int Index = B2ArenaAllocatorIndexer.Next<T>();

        private B2ArenaAllocatorIndexer()
        {
        }
    }
}