namespace Box2D.NET.Primitives
{
    public class b2ChainShape
    {
        public int id;
        public int bodyId;
        public int nextChainId;
        public int count;
        public int materialCount;
        public int[] shapeIndices;
        public b2SurfaceMaterial[] materials;
        public ushort generation;
    }
}