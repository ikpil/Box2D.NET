namespace Box2D.NET.Primitives
{
    /// Shape type
    /// @ingroup shape
    public enum b2ShapeType
    {
        /// A circle with an offset
        b2_circleShape,

        /// A capsule is an extruded circle
        b2_capsuleShape,

        /// A line segment
        b2_segmentShape,

        /// A convex polygon
        b2_polygonShape,

        /// A line segment owned by a chain shape
        b2_chainSegmentShape,

        /// The number of shape types
        b2_shapeTypeCount
    }
}