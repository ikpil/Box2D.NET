namespace Box2D.NET
{
    public struct B2CollisionPlane
    {
        public B2Plane plane;
        public float pushLimit;
        public float push;
        public bool clipVelocity;

        public B2CollisionPlane(B2Plane plane, float pushLimit, float push, bool clipVelocity)
        {
            this.plane = plane;
            this.pushLimit = pushLimit;
            this.push = push;
            this.clipVelocity = clipVelocity;
        }
    }
}