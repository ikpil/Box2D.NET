namespace Box2D.NET.Primitives
{
    /// Simplex from the GJK algorithm
    public class b2Simplex
    {
        public b2SimplexVertex v1 = new b2SimplexVertex();
        public b2SimplexVertex v2 = new b2SimplexVertex();
        public b2SimplexVertex v3 = new b2SimplexVertex(); // vertices
        public int count; // number of valid vertices
    }
}