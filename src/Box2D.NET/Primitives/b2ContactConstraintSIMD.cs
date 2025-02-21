using static Box2D.NET.core;

namespace Box2D.NET.Primitives
{
    // Soft contact constraints with sub-stepping support
    // Uses fixed anchors for Jacobians for better behavior on rolling shapes (circles & capsules)
    // http://mmacklin.com/smallsteps.pdf
    // https://box2d.org/files/ErinCatto_SoftConstraints_GDC2011.pdf
    public class b2ContactConstraintSIMD
    {
        public int[] indexA = new int[B2_SIMD_WIDTH];
        public int[] indexB = new int[B2_SIMD_WIDTH];

        public b2FloatW invMassA, invMassB;
        public b2FloatW invIA, invIB;
        public b2Vec2W normal;
        public b2FloatW friction;
        public b2FloatW tangentSpeed;
        public b2FloatW rollingResistance;
        public b2FloatW rollingMass;
        public b2FloatW rollingImpulse;
        public b2FloatW biasRate;
        public b2FloatW massScale;
        public b2FloatW impulseScale;
        public b2Vec2W anchorA1, anchorB1;
        public b2FloatW normalMass1, tangentMass1;
        public b2FloatW baseSeparation1;
        public b2FloatW normalImpulse1;
        public b2FloatW maxNormalImpulse1;
        public b2FloatW tangentImpulse1;
        public b2Vec2W anchorA2, anchorB2;
        public b2FloatW baseSeparation2;
        public b2FloatW normalImpulse2;
        public b2FloatW maxNormalImpulse2;
        public b2FloatW tangentImpulse2;
        public b2FloatW normalMass2, tangentMass2;
        public b2FloatW restitution;
        public b2FloatW relativeVelocity1, relativeVelocity2;
    }
}