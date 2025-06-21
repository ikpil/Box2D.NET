namespace Box2D.NET
{
    /// Joint events report joints that are awake and have a force and/or torque exceeding the threshold
    /// The observed forces and torques are not returned for efficiency reasons.
    public struct B2JointEvent
    {
        /// The joint id
        public B2JointId jointId;

        /// The user data from the joint for convenience
        public object userData;
    }
}