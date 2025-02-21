namespace Box2D.NET.Primitives
{
    /// Axis-aligned bounding box
    public struct b2AABB
    {
        public b2Vec2 lowerBound;
        public b2Vec2 upperBound;

        public b2AABB(b2Vec2 lowerBound, b2Vec2 upperBound)
        {
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
        }
    }
}