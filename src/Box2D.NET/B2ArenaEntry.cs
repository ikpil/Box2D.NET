// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    public struct B2ArenaEntry<T>
    {
        public ArraySegment<T> data;
        public string name;
        public int size;
        public bool usedMalloc;
    }
}
