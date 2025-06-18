namespace Box2D.NET
{
    /// Joint events report joints that are awake and have a force and/or torque exceeding the threshold
    /// The observed forces and torques are not returned for efficiency reasons.
    public struct B2JointEvent
    {
        public B2JointId jointId;
        public object userData;
    }
}