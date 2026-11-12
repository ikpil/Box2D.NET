// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Contact manifold point in local coordinates (frame A).
    public struct B2LocalManifoldPoint
    {
        /// Contact point in frame A
        public B2Vec2 point;

        /// The separation of the contact point, negative if penetrating. May be positive or negative.
        public float separation;

        /// Uniquely identifies a contact point between two shapes
        public ushort id;
    }
}
