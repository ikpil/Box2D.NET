// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public class B2Sensor
    {
        public B2Array<B2ShapeRef> hits;
        public B2Array<B2ShapeRef> overlaps1;
        public B2Array<B2ShapeRef> overlaps2;
        public int shapeId;
    }
}