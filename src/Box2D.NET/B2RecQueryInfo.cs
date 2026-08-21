// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A spatial query recorded during a replayed frame, exposed for inspection.
    public struct B2RecQueryInfo
    {
        public B2RecQueryType type;
        public B2QueryFilter filter; // zeroed for the shape local query types
        public B2AABB aabb;          // overlap AABB
        public B2Vec2 origin;        // ray and cast origin
        public B2Vec2 translation;   // ray and cast translation
        public B2ShapeId shape;      // target shape for the shape local query types
        public int hitCount;         // number of recorded results
    }
}
