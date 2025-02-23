namespace Box2D.NET.Primitives
{
    /// A line segment with two-sided collision.
    public class b2Segment // todo: @ikpil class or struct?
    {
        /// The first point
        public b2Vec2 point1;

        /// The second point
        public b2Vec2 point2;

        public b2Segment(b2Vec2 point1, b2Vec2 point2)
        {
            this.point1 = point1;
            this.point2 = point2;
        }
    }
}