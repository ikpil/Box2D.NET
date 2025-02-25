// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// Low level shape cast input in generic form. This allows casting an arbitrary point
    /// cloud wrap with a radius. For example, a circle is a single point with a non-zero radius.
    /// A capsule is two points with a non-zero radius. A box is four points with a zero radius.
    public struct b2ShapeCastInput
    {
        /// A point cloud to cast
        //public b2Vec2[] points = new b2Vec2[constants.B2_MAX_POLYGON_VERTICES];
        public b2Vec2[] points;

        /// The number of points
        public int count;

        /// The radius around the point cloud
        public float radius;

        /// The translation of the shape cast
        public b2Vec2 translation;

        /// The maximum fraction of the translation to consider, typically 1
        public float maxFraction;
    }
}
