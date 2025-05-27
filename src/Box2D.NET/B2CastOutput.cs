// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Low level ray cast or shape-cast output data. Returns a zero fraction and normal in the case of initial overlap.
    public struct B2CastOutput
    {
        /// The surface normal at the hit point
        public B2Vec2 normal;

        /// The surface hit point
        public B2Vec2 point;

        /// The fraction of the input translation at collision
        public float fraction;

        /// The number of iterations used
        public int iterations;

        /// Did the cast hit?
        public bool hit;
    }
}