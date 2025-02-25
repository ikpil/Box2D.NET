// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    public class b2DistanceJoint
    {
        public float length;
        public float hertz;
        public float dampingRatio;
        public float minLength;
        public float maxLength;

        public float maxMotorForce;
        public float motorSpeed;

        public float impulse;
        public float lowerImpulse;
        public float upperImpulse;
        public float motorImpulse;

        public int indexA;
        public int indexB;
        public b2Vec2 anchorA;
        public b2Vec2 anchorB;
        public b2Vec2 deltaCenter;
        public b2Softness distanceSoftness;
        public float axialMass;

        public bool enableSpring;
        public bool enableLimit;
        public bool enableMotor;
    }
}
