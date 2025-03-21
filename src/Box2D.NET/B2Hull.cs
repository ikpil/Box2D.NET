// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A convex hull. Used to create convex polygons.
    /// @warning Do not modify these values directly, instead use b2ComputeHull()
    public struct B2Hull
    {
        /// The final points of the hull
        public B2FixedArray8<B2Vec2> points;

        /// The number of points
        public int count;
    }
}
