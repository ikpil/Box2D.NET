// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Result from b2World_RayCastClosest
    /// If there is initial overlap the fraction and normal will be zero while the point is an arbitrary point in the overlap region.
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
