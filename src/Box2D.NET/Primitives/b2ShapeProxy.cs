using Box2D.NET.Core;

namespace Box2D.NET.Primitives
{
    /// A distance proxy is used by the GJK algorithm. It encapsulates any shape.
    public struct b2ShapeProxy
    {
        /// The point cloud
        public UnsafeArray8<b2Vec2> points;

        /// The number of points
        public int count;

        /// The external radius of the point cloud
        public float radius;
    }
}