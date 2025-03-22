// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2PrismaticJoint
    {
        public B2Vec2 localAxisA;
        public B2Vec2 impulse;
        public float springImpulse;
        public float motorImpulse;
        public float lowerImpulse;
        public float upperImpulse;
        public float hertz;
        public float dampingRatio;
        public float maxMotorForce;
        public float motorSpeed;
        public float referenceAngle;
        public float lowerTranslation;
        public float upperTranslation;

        public int indexA;
        public int indexB;
        public B2Vec2 anchorA;
        public B2Vec2 anchorB;
        public B2Vec2 axisA;
        public B2Vec2 deltaCenter;
        public float deltaAngle;
        public float axialMass;
        public B2Softness springSoftness;

        public bool enableSpring;
        public bool enableLimit;
        public bool enableMotor;
    }
}
