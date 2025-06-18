// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2MotorJoint
    {
        public B2Vec2 linearOffset;
        public float angularOffset;
        public B2Vec2 linearImpulse;
        public float angularImpulse;
        public float maxForce;
        public float maxTorque;
        public float correctionFactor;

        public int indexA;
        public int indexB;
        public B2Transform frameA;
        public B2Transform frameB;
        public B2Vec2 deltaCenter;
        public B2Mat22 linearMass;
        public float angularMass;
    }
}
