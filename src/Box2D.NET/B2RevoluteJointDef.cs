// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Revolute joint definition
    ///
    /// This requires defining an anchor point where the bodies are joined.
    /// The definition uses local anchor points so that the
    /// initial configuration can violate the constraint slightly. You also need to
    /// specify the initial relative angle for joint limits. This helps when saving
    /// and loading a game.
    /// The local anchor points are measured from the body's origin
    /// rather than the center of mass because:
    /// 1. you might not know where the center of mass will be
    /// 2. if you add/remove shapes from a body and recompute the mass, the joints will be broken
    /// @ingroup revolute_joint
    public struct B2RevoluteJointDef
    {
        /// The first attached body
        public B2BodyId bodyIdA;

        /// The second attached body
        public B2BodyId bodyIdB;

        /// The local anchor point relative to bodyA's origin
        public B2Vec2 localAnchorA;

        /// The local anchor point relative to bodyB's origin
        public B2Vec2 localAnchorB;

        /// The bodyB angle minus bodyA angle in the reference state (radians).
        /// This defines the zero angle for the joint limit.
        public float referenceAngle;

        /// Enable a rotational spring on the revolute hinge axis
        public bool enableSpring;

        /// The spring stiffness Hertz, cycles per second
        public float hertz;

        /// The spring damping ratio, non-dimensional
        public float dampingRatio;

        /// A flag to enable joint limits
        public bool enableLimit;

        /// The lower angle for the joint limit in radians. Minimum of -0.95*pi radians.
        public float lowerAngle;

        /// The upper angle for the joint limit in radians. Maximum of 0.95*pi radians.
        public float upperAngle;

        /// A flag to enable the joint motor
        public bool enableMotor;

        /// The maximum motor torque, typically in newton-meters
        public float maxMotorTorque;

        /// The desired motor speed in radians per second
        public float motorSpeed;

        /// Scale the debug draw
        public float drawSize;

        /// Set this flag to true if the attached bodies should collide
        public bool collideConnected;

        /// User data pointer
        public object userData;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
