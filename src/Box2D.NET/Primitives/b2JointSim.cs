namespace Box2D.NET.Primitives
{
    /// The @base joint class. Joints are used to constraint two bodies together in
    /// various fashions. Some joints also feature limits and motors.
    public class b2JointSim
    {
        public int jointId;

        public int bodyIdA;
        public int bodyIdB;

        public b2JointType type;

        // Anchors relative to body origin
        public b2Vec2 localOriginAnchorA;
        public b2Vec2 localOriginAnchorB;

        public float invMassA, invMassB;
        public float invIA, invIB;

        // TODO: @ikpil, check
        // union
        // {
        public b2DistanceJoint distanceJoint;
        public b2MotorJoint motorJoint;
        public b2MouseJoint mouseJoint;
        public b2RevoluteJoint revoluteJoint;
        public b2PrismaticJoint prismaticJoint;
        public b2WeldJoint weldJoint;
        public b2WheelJoint wheelJoint;
        //};

        public void Clear()
        {
            jointId = 0;
            bodyIdA = 0;
            bodyIdB = 0;
            type = b2JointType.b2_distanceJoint;
            localOriginAnchorA = new b2Vec2();
            localOriginAnchorB = new b2Vec2();
            invMassA = 0.0f;
            invMassB = 0.0f;
            invIA = 0.0f;
            invIB = 0.0f;
            distanceJoint = null;
            motorJoint = null;
            mouseJoint = null;
            revoluteJoint = null;
            prismaticJoint = null;
            weldJoint = null;
            wheelJoint = null;
        }
        
        public void CopyFrom(b2JointSim other)
        {
            jointId = other.jointId;
            bodyIdA = other.bodyIdA;
            bodyIdB = other.bodyIdB;
            type = other.type;
            localOriginAnchorA = other.localOriginAnchorA;
            localOriginAnchorB = other.localOriginAnchorB;
            invMassA = other.invMassA;
            invMassB = other.invMassB;
            invIA = other.invIA;
            invIB = other.invIB;
            distanceJoint = other.distanceJoint;
            motorJoint = other.motorJoint;
            mouseJoint = other.mouseJoint;
            revoluteJoint = other.revoluteJoint;
            prismaticJoint = other.prismaticJoint;
            weldJoint = other.weldJoint;
            wheelJoint = other.wheelJoint;
        }
    }
}