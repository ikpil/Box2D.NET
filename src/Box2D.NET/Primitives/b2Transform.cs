namespace Box2D.NET.Primitives
{
    /// A 2D rigid transform
    public struct b2Transform
    {
        public b2Vec2 p;
        public b2Rot q;

        public b2Transform(b2Vec2 p, b2Rot q)
        {
            this.p = p;
            this.q = q;
        }
    }
}