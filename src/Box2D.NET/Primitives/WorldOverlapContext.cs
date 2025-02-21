namespace Box2D.NET.Primitives
{
    public class WorldOverlapContext
    {
        public b2World world;
        public b2OverlapResultFcn fcn;
        public b2QueryFilter filter;
        public b2ShapeProxy proxy;
        public b2Transform transform;
        public object userContext;

        public WorldOverlapContext(b2World world, b2OverlapResultFcn fcn, b2QueryFilter filter, b2ShapeProxy proxy, b2Transform transform, object userContext)
        {
            this.world = world;
            this.fcn = fcn;
            this.filter = filter;
            this.proxy = proxy;
            this.transform = transform;
            this.userContext = userContext;
        }
    }
}