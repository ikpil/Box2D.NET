// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// Axis-aligned bounding box
    public struct b2AABB
    {
        public b2Vec2 lowerBound;
        public b2Vec2 upperBound;

        public b2AABB(b2Vec2 lowerBound, b2Vec2 upperBound)
        {
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
        }
    }
}
