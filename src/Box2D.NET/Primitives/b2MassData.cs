// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// This holds the mass data computed for a shape.
    public class b2MassData // TODO: @ikpil class or struct
    {
        /// The mass of the shape, usually in kilograms.
        public float mass;

        /// The position of the shape's centroid relative to the shape's origin.
        public b2Vec2 center;

        /// The rotational inertia of the shape about the local origin.
        public float rotationalInertia;

        public b2MassData()
        {
        }

        public b2MassData(float mass, b2Vec2 center, float rotationalInertia)
        {
            this.mass = mass;
            this.center = center;
            this.rotationalInertia = rotationalInertia;
        }
    }
}
