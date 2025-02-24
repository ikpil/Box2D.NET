﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// Result from b2World_RayCastClosest
    /// @ingroup world
    public class b2RayResult
    {
        public b2ShapeId shapeId;
        public b2Vec2 point;
        public b2Vec2 normal;
        public float fraction;
        public int nodeVisits;
        public int leafVisits;
        public bool hit;
    }
}
