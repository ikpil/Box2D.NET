namespace Box2D.NET.Primitives
{
    /// Input parameters for b2TimeOfImpact
    public class b2TOIInput
    {
        public b2ShapeProxy proxyA; // The proxy for shape A
        public b2ShapeProxy proxyB; // The proxy for shape B
        public b2Sweep sweepA; // The movement of shape A
        public b2Sweep sweepB; // The movement of shape B
        public float maxFraction; // Defines the sweep interval [0, maxFraction]
    }
}