namespace Box2D.NET.Primitives
{
    /// Output parameters for b2TimeOfImpact.
    public class b2TOIOutput
    {
        public b2TOIState state; // The type of result
        public float fraction; // The sweep time of the collision
    }
}