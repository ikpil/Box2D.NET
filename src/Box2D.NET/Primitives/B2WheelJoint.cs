// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class B2WheelJoint
    {
        public B2Vec2 localAxisA;
        public float perpImpulse;
        public float motorImpulse;
        public float springImpulse;
        public float lowerImpulse;
        public float upperImpulse;
        public float maxMotorTorque;
        public float motorSpeed;
        public float lowerTranslation;
        public float upperTranslation;
        public float hertz;
        public float dampingRatio;

        public int indexA;
        public int indexB;
        public B2Vec2 anchorA;
        public B2Vec2 anchorB;
        public B2Vec2 axisA;
        public B2Vec2 deltaCenter;
        public float perpMass;
        public float motorMass;
        public float axialMass;
        public B2Softness springSoftness;

        public bool enableSpring;
        public bool enableMotor;
        public bool enableLimit;
    }
}
