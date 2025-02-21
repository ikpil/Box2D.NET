namespace Box2D.NET.Primitives
{
    public ref struct b2SeparationFunction
    {
        public b2ShapeProxy proxyA;
        public b2ShapeProxy proxyB;
        public b2Sweep sweepA, sweepB;
        public b2Vec2 localPoint;
        public b2Vec2 axis;
        public b2SeparationType type;
    }
}