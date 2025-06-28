namespace Box2D.NET
{
    /// Motion locks to restrict the body movement
    public struct B2MotionLocks
    {
        /// Prevent translation along the x-axis
        public bool linearX;

        /// Prevent translation along the y-axis
        public bool linearY;

        /// Prevent rotation around the z-axis
        public bool angularZ;

        public B2MotionLocks(bool linearX, bool linearY, bool angularZ)
        {
            this.linearX = linearX;
            this.linearY = linearY;
            this.angularZ = angularZ;
        }
    }
}