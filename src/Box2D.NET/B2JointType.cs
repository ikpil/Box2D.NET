// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Joint type enumeration
    ///
    /// This is useful because all joint types use b2JointId and sometimes you
    /// want to get the type of a joint.
    /// @ingroup joint
    public enum B2JointType
    {
        b2_distanceJoint,
        b2_filterJoint,
        b2_motorJoint,
        b2_mouseJoint,
        b2_prismaticJoint,
        b2_revoluteJoint,
        b2_weldJoint,
        b2_wheelJoint,
    }
}
