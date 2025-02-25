﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    // Array declaration that doesn't need the type T to be defined
    public struct b2Array<T>
    {
        public T[] data;
        public int count;
        public int capacity;
    }
}
