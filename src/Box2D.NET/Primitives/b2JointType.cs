namespace Box2D.NET.Primitives
{
    /// Joint type enumeration
    ///
    /// This is useful because all joint types use b2JointId and sometimes you
    /// want to get the type of a joint.
    /// @ingroup joint
    public enum b2JointType
    {
        b2_distanceJoint,
        b2_motorJoint,
        b2_mouseJoint,
        b2_nullJoint,
        b2_prismaticJoint,
        b2_revoluteJoint,
        b2_weldJoint,
        b2_wheelJoint,
    }
}