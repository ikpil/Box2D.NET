// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2ContactConstraint
    {
        public int indexA;
        public int indexB;
        public B2FixedArray2<B2ContactConstraintPoint> points;
        public B2Vec2 normal;
        public float invMassA, invMassB;
        public float invIA, invIB;
        public float friction;
        public float restitution;
        public float tangentSpeed;
        public float rollingResistance;
        public float rollingMass;
        public float rollingImpulse;
        public B2Softness softness;
        public int pointCount;
    }
}
