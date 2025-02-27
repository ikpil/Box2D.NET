﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// Output for b2ShapeDistance
    public class B2DistanceOutput
    {
        public B2Vec2 pointA; // Closest point on shapeA

        public B2Vec2 pointB; // Closest point on shapeB

        // todo_erin implement this
        // B2Vec2 normal;			// Normal vector that points from A to B
        public float distance; // The final distance, zero if overlapped
        public int iterations; // Number of GJK iterations used
        public int simplexCount; // The number of simplexes stored in the simplex array
    }
}
