// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // A single recorded callback hit, used both as reader scratch and as the per-frame draw store
    internal struct B2RecRecordedHit
    {
        internal B2ShapeId id;
        internal B2Vec2 point;
        internal B2Vec2 normal;
        internal float fraction;
        internal B2PlaneResult plane;
        internal float userReturnF;
        internal bool userReturnB;
    }
}
