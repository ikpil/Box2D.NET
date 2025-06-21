// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A mouse joint is used to make a point on body B track a point on body A.
    /// You may move local frame A to change the target point.
    /// This a soft constraint and allows the constraint to stretch without
    /// applying huge forces. This also applies rotation constraint heuristic to improve control.
    /// @ingroup mouse_joint
    public struct B2MouseJointDef
    {
        /// Base joint definition
        public B2JointDef @base;

        /// Stiffness in hertz
        public float hertz;

        /// Damping ratio, non-dimensional
        public float dampingRatio;

        /// Maximum force, typically in newtons
        public float maxForce;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
