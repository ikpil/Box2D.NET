﻿namespace Box2D.NET.Primitives
{
    public class b2PrismaticJoint
    {
        public b2Vec2 localAxisA;
        public b2Vec2 impulse;
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
        public b2Vec2 anchorA;
        public b2Vec2 anchorB;
        public b2Vec2 axisA;
        public b2Vec2 deltaCenter;
        public float deltaAngle;
        public float axialMass;
        public b2Softness springSoftness;

        public bool enableSpring;
        public bool enableLimit;
        public bool enableMotor;
    }
}