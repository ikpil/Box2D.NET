namespace Box2D.NET.Primitives
{
    /// A convex hull. Used to create convex polygons.
    /// @warning Do not modify these values directly, instead use b2ComputeHull()
    public class b2Hull
    {
        /// The final points of the hull
        public b2Vec2[] points = new b2Vec2[constants.B2_MAX_POLYGON_VERTICES];

        /// The number of points
        public int count;
    }
}