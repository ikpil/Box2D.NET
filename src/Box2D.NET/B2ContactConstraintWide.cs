// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    // Soft contact constraints with sub-stepping support
    // Uses fixed anchors for Jacobians for better behavior on rolling shapes (circles & capsules)
    // http://mmacklin.com/smallsteps.pdf
    // https://box2d.org/files/ErinCatto_SoftConstraints_GDC2011.pdf
    public struct B2ContactConstraintWide
    {
        // Read common
        public B2FixedArray4<int> indexA; // = new int[B2_SIMD_WIDTH];
        public B2FixedArray4<int> indexB; // = new int[B2_SIMD_WIDTH];

        public B2FloatW invMassA, invMassB;
        public B2FloatW invIA, invIB;
        public B2Vec2W normal;
        public B2Vec2W anchorA1, anchorB1;
        public B2Vec2W anchorA2, anchorB2;
        public B2FloatW normalMass1, normalMass2;
        public B2FloatW baseSeparation1;
        public B2FloatW baseSeparation2;

        // Write in push and solve
        public B2FloatW normalImpulse1, normalImpulse2;
        public B2FloatW totalNormalImpulse1, totalNormalImpulse2;

        // Write in solve
        public B2FloatW tangentImpulse1, tangentImpulse2;
        public B2FloatW rollingImpulse;

        // Read in solve
        public B2FloatW friction;
        public B2FloatW tangentSpeed;
        public B2FloatW rollingResistance;
        public B2FloatW tangentMass1, tangentMass2;
        public B2FloatW negRestitutionVelocity1, negRestitutionVelocity2;
    }
}
