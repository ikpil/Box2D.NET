// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // A stack array uses a fixed size buffer but can grow on the heap if necessary.
    public struct B2StackArray<T>
    {
        public T[] stackData;
        public T[] data;
        public int count;
        public int capacity;
    }
}
