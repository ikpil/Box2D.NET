// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A solid convex polygon. It is assumed that the interior of the polygon is to
    /// the left of each edge.
    /// Polygons have a maximum number of vertices equal to B2_MAX_POLYGON_VERTICES.
    /// In most cases you should not need many vertices for a convex polygon.
    /// @warning DO NOT fill this out manually, instead use a helper function like
    /// b2MakePolygon or b2MakeBox.
    public struct B2Polygon
    {
        /// The polygon vertices
        public B2FixedArray8<B2Vec2> vertices; // = new B2Vec2[B2Constants.B2_MAX_POLYGON_VERTICES];

        /// The outward normal vectors of the polygon sides
        public B2FixedArray8<B2Vec2> normals; // = new B2Vec2[B2Constants.B2_MAX_POLYGON_VERTICES];

        /// The centroid of the polygon
        public B2Vec2 centroid;

        /// The external radius for rounded polygons
        public float radius;

        /// The number of polygon vertices
        public int count;
    }
}