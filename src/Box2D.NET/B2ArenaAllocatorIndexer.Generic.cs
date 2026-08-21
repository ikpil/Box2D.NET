// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    internal class B2ArenaAllocatorIndexer<T> where T : new()
    {
        internal static readonly int Index = B2ArenaAllocatorIndexer.Next<T>();

        private B2ArenaAllocatorIndexer()
        {
        }
    }
}
