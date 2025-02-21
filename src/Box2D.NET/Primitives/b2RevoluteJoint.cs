namespace Box2D.NET.Primitives
{
    public class b2RevoluteJoint
    {
        public b2Vec2 linearImpulse;
        public float springImpulse;
        public float motorImpulse;
        public float lowerImpulse;
        public float upperImpulse;
        public float hertz;
        public float dampingRatio;
        public float maxMotorTorque;
        public float motorSpeed;
        public float referenceAngle;
        public float lowerAngle;
        public float upperAngle;

        public int indexA;
        public int indexB;
        public b2Vec2 anchorA;
        public b2Vec2 anchorB;
        public b2Vec2 deltaCenter;
        public float deltaAngle;
        public float axialMass;
        public b2Softness springSoftness;

        public bool enableSpring;
        public bool enableMotor;
        public bool enableLimit;
    }
}