namespace Box2D.NET
{
    public struct B2CollisionPlane
    {
        public B2Plane plane;
        public float pushLimit;
        public float push;
        public bool clipVelocity; 
    }
}