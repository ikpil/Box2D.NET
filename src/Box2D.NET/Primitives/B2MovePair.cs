// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class b2MovePair
    {
        public int shapeIndexA;
        public int shapeIndexB;
        public b2MovePair next;
        public bool heap;
    }
}
