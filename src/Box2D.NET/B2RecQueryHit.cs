// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// One result of a recorded spatial query.
    public struct B2RecQueryHit
    {
        public B2ShapeId shape;
        public B2Vec2 point;
        public B2Vec2 normal;
        public float fraction;
    }
}
