// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A solid circle
    public struct B2Circle
    {
        /// The local center
        public B2Vec2 center;

        /// The radius
        public float radius;

        public B2Circle(B2Vec2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
    }
}
