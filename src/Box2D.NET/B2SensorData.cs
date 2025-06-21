namespace Box2D.NET
{
    /// The contact data for two shapes. By convention the manifold normal points
    /// from shape A to shape B.
    /// @see b2Shape_GetContactData() and b2Body_GetContactData()
    public struct B2SensorData
    {
        /// The visiting shape
        public B2ShapeId visitorId;

        /// The transform of the body of the visiting shape. This is normally
        /// the current transform of the body. However, for a sensor hit, this is
        /// the transform of the visiting body when it hit.
        public B2Transform visitTransform;

        public B2SensorData(B2ShapeId visitorId, B2Transform visitTransform)
        {
            this.visitorId = visitorId;
            this.visitTransform = visitTransform;
        }
    }
}