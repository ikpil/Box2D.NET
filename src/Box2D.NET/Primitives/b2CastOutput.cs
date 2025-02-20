namespace Box2D.NET.Primitives
{
    /// Low level ray cast or shape-cast output data
    public class b2CastOutput
    {
        /// The surface normal at the hit point
        public b2Vec2 normal;

        /// The surface hit point
        public b2Vec2 point;

        /// The fraction of the input translation at collision
        public float fraction;

        /// The number of iterations used
        public int iterations;

        /// Did the cast hit?
        public bool hit;
    }
}