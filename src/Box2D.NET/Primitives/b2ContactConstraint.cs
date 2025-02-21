using Box2D.NET.Core;

namespace Box2D.NET.Primitives
{
    public class b2ContactConstraint
    {
        public int indexA;
        public int indexB;
        public UnsafeArray2<b2ContactConstraintPoint> points;
        public b2Vec2 normal;
        public float invMassA, invMassB;
        public float invIA, invIB;
        public float friction;
        public float restitution;
        public float tangentSpeed;
        public float rollingResistance;
        public float rollingMass;
        public float rollingImpulse;
        public b2Softness softness;
        public int pointCount;
    }
}