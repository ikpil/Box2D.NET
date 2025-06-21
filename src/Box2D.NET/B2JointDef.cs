namespace Box2D.NET
{
    /// Base joint definition used by all joint types.
    /// The local frames are measured from the body's origin rather than the center of mass because:
    /// 1. you might not know where the center of mass will be
    /// 2. if you add/remove shapes from a body and recompute the mass, the joints will be broken
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

        /// Constraint hertz (advanced feature)
        public float constraintHertz;

        /// Constraint damping ratio (advanced feature)
        public float constraintDampingRatio;

        /// Debug draw scale
        public float drawScale;

        /// Set this flag to true if the attached bodies should collide
        public bool collideConnected; 
    }
}