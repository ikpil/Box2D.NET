namespace Box2D.NET.Primitives
{
    /// A 2-by-2 Matrix
    public struct b2Mat22
    {
        /// columns
        public b2Vec2 cx, cy;

        public b2Mat22(b2Vec2 cx, b2Vec2 cy)
        {
            this.cx = cx;
            this.cy = cy;
        }
    }
}