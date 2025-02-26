// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;

namespace Box2D.NET.Primitives
{
    public class b2ShapeRefComparer : IComparer<b2ShapeRef>
    {
        public static readonly b2ShapeRefComparer Shared = new b2ShapeRefComparer();

        private b2ShapeRefComparer()
        {
        }

        public int Compare(b2ShapeRef a, b2ShapeRef b)
        {
            return sensor.b2CompareShapeRefs(a, b);
        }
    }
}
