namespace Box2D.NET.Primitives
{
    /// A solid capsule can be viewed as two semicircles connected
    /// by a rectangle.
    public class b2Capsule
    {
        /// Local center of the first semicircle
        public b2Vec2 center1;

        /// Local center of the second semicircle
        public b2Vec2 center2;

        /// The radius of the semicircles
        public float radius;

        public b2Capsule(b2Vec2 center1, b2Vec2 center2, float radius)
        {
            this.center1 = center1;
            this.center2 = center2;
            this.radius = radius;
        }
    }
}