namespace Box2D.NET.Primitives
{
    public struct b2ContactConstraintPoint
    {
        public b2Vec2 anchorA, anchorB;
        public float baseSeparation;
        public float relativeVelocity;
        public float normalImpulse;
        public float tangentImpulse;
        public float maxNormalImpulse;
        public float normalMass;
        public float tangentMass;
    }
}