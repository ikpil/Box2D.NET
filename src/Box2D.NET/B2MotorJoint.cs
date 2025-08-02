// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2MotorJoint
    {
        public B2Vec2 linearVelocity;
        public float maxVelocityForce;
        public float angularVelocity;
        public float maxVelocityTorque;
        public float linearHertz;
        public float linearDampingRatio;
        public float maxSpringForce;
        public float angularHertz;
        public float angularDampingRatio;
        public float maxSpringTorque;

        public B2Vec2 linearVelocityImpulse;
        public float angularVelocityImpulse;
        public B2Vec2 linearSpringImpulse;
        public float angularSpringImpulse;

        public B2Softness linearSpring;
        public B2Softness angularSpring;

        public int indexA;
        public int indexB;
        public B2Transform frameA;
        public B2Transform frameB;
        public B2Vec2 deltaCenter;
        public B2Mat22 linearMass;
        public float angularMass;
    }
}
