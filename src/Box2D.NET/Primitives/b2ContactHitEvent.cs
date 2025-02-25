// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// A hit touch event is generated when two shapes collide with a speed faster than the hit speed threshold.
    public class b2ContactHitEvent
    {
        /// Id of the first shape
        public b2ShapeId shapeIdA;

        /// Id of the second shape
        public b2ShapeId shapeIdB;

        /// Point where the shapes hit
        public b2Vec2 point;

        /// Normal vector pointing from shape A to shape B
        public b2Vec2 normal;

        /// The speed the shapes are approaching. Always positive. Typically in meters per second.
        public float approachSpeed;
    }
}
