namespace Box2D.NET
{
    /// separation = dot(normal, point) - offset
    public struct B2Plane
    {
        public B2Vec2 normal;
        public float offset;

        public B2Plane(B2Vec2 normal, float offset)
        {
            this.normal = normal;
            this.offset = offset;
        }
    }
}