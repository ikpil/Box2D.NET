// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A distance proxy is used by the GJK algorithm. It encapsulates any shape.
    /// You can provide between 1 and B2_MAX_POLYGON_VERTICES and a radius.
    public struct B2ShapeProxy
    {
        /// The point cloud
        public B2FixedArray8<B2Vec2> points;

        /// The number of points. Must be greater than 0.
        public int count;

        /// The external radius of the point cloud. May be zero.
        public float radius;
    }
}
