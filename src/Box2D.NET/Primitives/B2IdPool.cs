// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class b2IdPool
    {
        public b2Array<int> freeArray;
        public int nextIndex;

        public void Clear()
        {
            freeArray = new b2Array<int>();
            nextIndex = 0;
        }
    }
}
