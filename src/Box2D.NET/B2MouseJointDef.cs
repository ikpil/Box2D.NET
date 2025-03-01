// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A mouse joint is used to make a point on a body track a specified world point.
    ///
    /// This a soft constraint and allows the constraint to stretch without
    /// applying huge forces. This also applies rotation constraint heuristic to improve control.
    /// @ingroup mouse_joint
    public class B2MouseJointDef
    {
        /// The first attached body. This is assumed to be static.
        public B2BodyId bodyIdA;

        /// The second attached body.
        public B2BodyId bodyIdB;

        /// The initial target point in world space
        public B2Vec2 target;

        /// Stiffness in hertz
        public float hertz;

        /// Damping ratio, non-dimensional
        public float dampingRatio;

        /// Maximum force, typically in newtons
        public float maxForce;

        /// Set this flag to true if the attached bodies should collide.
        public bool collideConnected;

        /// User data pointer
        public object userData;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
