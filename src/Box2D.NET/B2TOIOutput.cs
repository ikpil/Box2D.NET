// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Time of impact output
    public struct B2TOIOutput
    {
        /// The type of result
        public B2TOIState state;

        /// The hit point
        public B2Vec2 point;

        /// The hit normal
        public B2Vec2 normal;

        /// The sweep time of the collision 
        public float fraction;
    }
}