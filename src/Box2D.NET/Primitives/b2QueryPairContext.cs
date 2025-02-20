namespace Box2D.NET.Primitives
{
    public class b2QueryPairContext
    {
        public b2World world;
        public b2MoveResult moveResult;
        public b2BodyType queryTreeType;
        public int queryProxyKey;
        public int queryShapeIndex;
    }
}