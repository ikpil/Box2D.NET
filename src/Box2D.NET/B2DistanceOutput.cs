// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Output for b2ShapeDistance
    public struct B2DistanceOutput
    {
        public B2Vec2 pointA; // Closest point on shapeA
        public B2Vec2 pointB; // Closest point on shapeB
        public B2Vec2 normal; // Normal vector that points from A to B. Invalid if distance is zero.
        public float distance; // The final distance, zero if overlapped
        public int iterations; // Number of GJK iterations used
        public int simplexCount; // The number of simplexes stored in the simplex array
    }
}