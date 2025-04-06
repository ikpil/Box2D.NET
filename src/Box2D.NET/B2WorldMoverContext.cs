namespace Box2D.NET
{
    public struct B2WorldMoverContext
    {
        public B2World world;
        public b2PlaneResultFcn fcn;
        public B2QueryFilter filter;
        public B2Capsule mover;
        public object userContext;
    }
}