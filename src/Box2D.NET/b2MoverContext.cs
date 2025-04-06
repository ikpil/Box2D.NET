namespace Box2D.NET
{
    public struct B2MoverContext
    {
        public B2World world;
        public B2QueryFilter filter;
        public B2ShapeProxy proxy;
        public B2Transform transform;
        public object userContext;
    }
}