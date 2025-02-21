namespace Box2D.NET.Primitives
{
    /// 2D rotation
    /// This is similar to using a complex number for rotation
    public struct b2Rot
    {
        /// cosine and sine
        public float c, s;

        public b2Rot(float c, float s)
        {
            this.c = c;
            this.s = s;
        }
    }
}