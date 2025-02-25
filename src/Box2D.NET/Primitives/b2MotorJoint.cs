// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class b2MotorJoint
    {
        public b2Vec2 linearOffset;
        public float angularOffset;
        public b2Vec2 linearImpulse;
        public float angularImpulse;
        public float maxForce;
        public float maxTorque;
        public float correctionFactor;

        public int indexA;
        public int indexB;
        public b2Vec2 anchorA;
        public b2Vec2 anchorB;
        public b2Vec2 deltaCenter;
        public float deltaAngle;
        public b2Mat22 linearMass;
        public float angularMass;
    }
}
