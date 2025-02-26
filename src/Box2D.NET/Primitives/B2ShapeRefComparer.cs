// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;

namespace Box2D.NET.Primitives
{
    public class B2ShapeRefComparer : IComparer<B2ShapeRef>
    {
        public static readonly B2ShapeRefComparer Shared = new B2ShapeRefComparer();

        private B2ShapeRefComparer()
        {
        }

        public int Compare(B2ShapeRef a, B2ShapeRef b)
        {
            return sensor.b2CompareShapeRefs(a, b);
        }
    }
}
