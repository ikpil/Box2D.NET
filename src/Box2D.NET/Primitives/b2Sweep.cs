namespace Box2D.NET.Primitives
{
    /// This describes the motion of a body/shape for TOI computation. Shapes are defined with respect to the body origin,
    /// which may not coincide with the center of mass. However, to support dynamics we must interpolate the center of mass
    /// position.
    public class b2Sweep
    {
        public b2Vec2 localCenter; // Local center of mass position
        public b2Vec2 c1; // Starting center of mass world position
        public b2Vec2 c2; // Ending center of mass world position
        public b2Rot q1; // Starting world rotation
        public b2Rot q2; // Ending world rotation

        public b2Sweep()
        {
        }

        public b2Sweep(b2Vec2 localCenter, b2Vec2 c1, b2Vec2 c2, b2Rot q1, b2Rot q2)
        {
            this.localCenter = localCenter;
            this.c1 = c1;
            this.c2 = c2;
            this.q1 = q1;
            this.q2 = q2;
        }

        public b2Sweep Clone()
        {
            return new b2Sweep(localCenter, c1, c2, q1, q2);
        }
    }
}