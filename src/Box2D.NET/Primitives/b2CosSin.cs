namespace Box2D.NET.Primitives
{
    /// Cosine and sine pair
    /// This uses a custom implementation designed for cross-platform determinism
    public struct b2CosSin
    {
        /// cosine and sine
        public float cosine;

        public float sine;
    }
}