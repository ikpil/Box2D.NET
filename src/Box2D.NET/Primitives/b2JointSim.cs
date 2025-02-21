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

        public void CopyFrom(b2JointSim jointSim)
        {
            this.jointId = jointSim.jointId;
            this.bodyIdA = jointSim.bodyIdA;
            this.bodyIdB = jointSim.bodyIdB;
            this.type = jointSim.type;
            this.localOriginAnchorA = jointSim.localOriginAnchorA;
            this.localOriginAnchorB = jointSim.localOriginAnchorB;
            this.invMassA = jointSim.invMassA;
            this.invMassB = jointSim.invMassB;
            this.invIA = jointSim.invIA;
            this.invIB = jointSim.invIB;
            this.distanceJoint = jointSim.distanceJoint;
            this.motorJoint = jointSim.motorJoint;
            this.mouseJoint = jointSim.mouseJoint;
            this.revoluteJoint = jointSim.revoluteJoint;
            this.prismaticJoint = jointSim.prismaticJoint;
            this.weldJoint = jointSim.weldJoint;
            this.wheelJoint = jointSim.wheelJoint;
        }
    }
}