// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// This holds the mass data computed for a shape.
    public struct B2MassData
    {
        /// The mass of the shape, usually in kilograms.
        public float mass;

        /// The position of the shape's centroid relative to the shape's origin.
        public B2Vec2 center;

        /// The rotational inertia of the shape about the shape center.
        public float rotationalInertia;

        public B2MassData(float mass, B2Vec2 center, float rotationalInertia)
        {
            this.mass = mass;
            this.center = center;
            this.rotationalInertia = rotationalInertia;
        }
    }
}
