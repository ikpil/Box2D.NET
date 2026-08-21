// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Per-frame draw record for one query call
    internal struct B2RecDrawQuery
    {
        internal int kind;
        internal B2QueryFilter filter;
        internal B2AABB aabb;
        internal B2ShapeProxy proxy;
        internal B2Capsule mover;
        internal B2Vec2 origin;
        internal B2Vec2 translation;
        internal bool boolResult;
        internal float castFraction;
        internal B2CastOutput castOut;
        internal B2ShapeId shape;
        internal int hitStart;
        internal int hitCount;
    }
}
