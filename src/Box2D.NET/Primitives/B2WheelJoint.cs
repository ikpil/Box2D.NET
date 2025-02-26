// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class b2WheelJoint
    {
        public b2Vec2 localAxisA;
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
        public b2Vec2 anchorA;
        public b2Vec2 anchorB;
        public b2Vec2 axisA;
        public b2Vec2 deltaCenter;
        public float perpMass;
        public float motorMass;
        public float axialMass;
        public b2Softness springSoftness;

        public bool enableSpring;
        public bool enableMotor;
        public bool enableLimit;
    }
}
