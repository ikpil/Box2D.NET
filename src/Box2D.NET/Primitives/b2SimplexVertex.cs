namespace Box2D.NET.Primitives
{
    /// Simplex vertex for debugging the GJK algorithm
    public class b2SimplexVertex
    {
        public b2Vec2 wA; // support point in proxyA
        public b2Vec2 wB; // support point in proxyB
        public b2Vec2 w; // wB - wA
        public float a; // barycentric coordinate for closest point
        public int indexA; // wA index
        public int indexB; // wB index
    }
}