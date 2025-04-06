// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Soft contact constraints with sub-stepping support
    // Uses fixed anchors for Jacobians for better behavior on rolling shapes (circles & capsules)
    // http://mmacklin.com/smallsteps.pdf
    // https://box2d.org/files/ErinCatto_SoftConstraints_GDC2011.pdf
    public struct B2ContactConstraintSIMD
    {
        public B2FixedArray4<int> indexA; // = new int[B2_SIMD_WIDTH];
        public B2FixedArray4<int> indexB; // = new int[B2_SIMD_WIDTH];

        public B2FloatW invMassA, invMassB;
        public B2FloatW invIA, invIB;
        public B2Vec2W normal;
        public B2FloatW friction;
        public B2FloatW tangentSpeed;
        public B2FloatW rollingResistance;
        public B2FloatW rollingMass;
        public B2FloatW rollingImpulse;
        public B2FloatW biasRate;
        public B2FloatW massScale;
        public B2FloatW impulseScale;
        public B2Vec2W anchorA1, anchorB1;
        public B2FloatW normalMass1, tangentMass1;
        public B2FloatW baseSeparation1;
        public B2FloatW normalImpulse1;
        public B2FloatW totalNormalImpulse1;
        public B2FloatW tangentImpulse1;
        public B2Vec2W anchorA2, anchorB2;
        public B2FloatW baseSeparation2;
        public B2FloatW normalImpulse2;
        public B2FloatW totalNormalImpulse2;
        public B2FloatW tangentImpulse2;
        public B2FloatW normalMass2, tangentMass2;
        public B2FloatW restitution;
        public B2FloatW relativeVelocity1, relativeVelocity2;
    }
}
