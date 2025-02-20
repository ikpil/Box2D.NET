﻿namespace Box2D.NET.Primitives
{
    /// Output for b2ShapeDistance
    public class b2DistanceOutput
    {
        public b2Vec2 pointA; // Closest point on shapeA

        public b2Vec2 pointB; // Closest point on shapeB

        // todo_erin implement this
        // b2Vec2 normal;			// Normal vector that points from A to B
        public float distance; // The final distance, zero if overlapped
        public int iterations; // Number of GJK iterations used
        public int simplexCount; // The number of simplexes stored in the simplex array
    }
}