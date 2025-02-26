// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class b2Sensor
    {
        public b2Array<b2ShapeRef> overlaps1;
        public b2Array<b2ShapeRef> overlaps2;
        public int shapeId;
    }
}
