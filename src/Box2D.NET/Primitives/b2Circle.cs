namespace Box2D.NET.Primitives
{
    /// A solid circle
    public class b2Circle
    {
        /// The local center
        public b2Vec2 center;

        /// The radius
        public float radius;

        public b2Circle(b2Vec2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
    }
}