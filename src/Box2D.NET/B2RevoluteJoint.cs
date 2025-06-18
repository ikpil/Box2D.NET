// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2RevoluteJoint
    {
        public B2Vec2 linearImpulse;
        public float springImpulse;
        public float motorImpulse;
        public float lowerImpulse;
        public float upperImpulse;
        public float hertz;
        public float dampingRatio;
        public float targetAngle;
        public float maxMotorTorque;
        public float motorSpeed;
        public float lowerAngle;
        public float upperAngle;

        public int indexA;
        public int indexB;
        public B2Transform frameA;
        public B2Transform frameB;
        public B2Vec2 deltaCenter;
        public float axialMass;
        public B2Softness springSoftness;

        public bool enableSpring;
        public bool enableMotor;
        public bool enableLimit;
    }
}
