// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Memory;

namespace Box2D.NET
{
    /// A distance proxy is used by the GJK algorithm. It encapsulates any shape.
    public struct B2ShapeProxy
    {
        /// The point cloud
        public B2FixedArray8<B2Vec2> points;

        /// The number of points
        public int count;

        /// The external radius of the point cloud
        public float radius;
    }
}
