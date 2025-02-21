namespace Box2D.NET.Primitives
{
    public struct b2Softness
    {
        public float biasRate;
        public float massScale;
        public float impulseScale;

        public b2Softness(float biasRate, float massScale, float impulseScale)
        {
            this.biasRate = biasRate;
            this.massScale = massScale;
            this.impulseScale = impulseScale;
        }
    }
}