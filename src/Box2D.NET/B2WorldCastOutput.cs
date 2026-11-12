// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// World-space cast output. The hit point is a world position. The normal stays a float direction.
    public struct B2WorldCastOutput
    {
        /// The surface normal at the hit point
        public B2Vec2 normal;

        /// The surface hit point in world space
        public B2Pos point;

        /// The fraction of the input translation at collision
        public float fraction;

        /// The number of iterations used
        public int iterations;

        /// Did the cast hit?
        public bool hit;
    }
}
