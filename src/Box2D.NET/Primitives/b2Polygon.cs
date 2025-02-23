using System;

namespace Box2D.NET.Primitives
{
    /// A solid convex polygon. It is assumed that the interior of the polygon is to
    /// the left of each edge.
    /// Polygons have a maximum number of vertices equal to B2_MAX_POLYGON_VERTICES.
    /// In most cases you should not need many vertices for a convex polygon.
    /// @warning DO NOT fill this out manually, instead use a helper function like
    /// b2MakePolygon or b2MakeBox.
    public class b2Polygon
    {
        /// The polygon vertices
        public readonly b2Vec2[] vertices = new b2Vec2[constants.B2_MAX_POLYGON_VERTICES];

        /// The outward normal vectors of the polygon sides
        public readonly b2Vec2[] normals = new b2Vec2[constants.B2_MAX_POLYGON_VERTICES];

        /// The centroid of the polygon
        public b2Vec2 centroid;

        /// The external radius for rounded polygons
        public float radius;

        /// The number of polygon vertices
        public int count;

        public b2Polygon Clone()
        {
            var p = new b2Polygon();
            Array.Copy(vertices, p.vertices, vertices.Length);
            Array.Copy(normals, p.normals, normals.Length);
            p.centroid = centroid;
            p.radius = radius;
            p.count = count;
            return p;
        }
    }
}