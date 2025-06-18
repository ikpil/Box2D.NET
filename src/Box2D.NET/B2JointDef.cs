namespace Box2D.NET
{
    public struct B2JointDef
    {
        /// User data pointer
        public object userData;

        /// The first attached body
        public B2BodyId bodyIdA;

        /// The second attached body
        public B2BodyId bodyIdB;

        /// The first local joint frame
        public B2Transform localFrameA;

        /// The second local joint frame
        public B2Transform localFrameB;

        /// Force threshold for joint events
        public float forceThreshold;

        /// Torque threshold for joint events
        public float torqueThreshold;

        /// Debug draw size
        public float drawSize;

        /// Set this flag to true if the attached bodies should collide
        public bool collideConnected; 
    }
}