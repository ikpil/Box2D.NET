using static Box2D.NET.math_function;

namespace Box2D.NET.Primitives
{
    /// A line segment with one-sided collision. Only collides on the right side.
    /// Several of these are generated for a chain shape.
    /// ghost1 -> point1 -> point2 -> ghost2
    public class b2ChainSegment
    {
        /// The tail ghost vertex
        public b2Vec2 ghost1;

        /// The line segment
        public b2Segment segment = new b2Segment(b2Vec2_zero, b2Vec2_zero);

        /// The head ghost vertex
        public b2Vec2 ghost2;

        /// The owning chain shape index (internal usage only)
        public int chainId;
    }
}