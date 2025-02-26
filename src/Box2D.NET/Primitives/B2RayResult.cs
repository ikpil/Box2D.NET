// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// Result from b2World_RayCastClosest
    /// @ingroup world
    public class B2RayResult
    {
        public B2ShapeId shapeId;
        public B2Vec2 point;
        public B2Vec2 normal;
        public float fraction;
        public int nodeVisits;
        public int leafVisits;
        public bool hit;
    }
}
