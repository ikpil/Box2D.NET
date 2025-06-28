// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public class B2Sensor
    {
        // todo find a way to pool these
        public B2Array<B2Visitor> hits;
        public B2Array<B2Visitor> overlaps1;
        public B2Array<B2Visitor> overlaps2;
        public int shapeId;
    }
}