// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Contact manifold in local coordinates (frame A).
    public struct B2LocalManifold
    {
        /// The unit normal vector in frame A, points from shape A to shape B
        public B2Vec2 normal;

        /// The manifold points, up to two are possible in 2D
        public B2FixedArray2<B2LocalManifoldPoint> points;

        /// The number of contacts points, will be 0, 1, or 2
        public int pointCount;
    }
}
