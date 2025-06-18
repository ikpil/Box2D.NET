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
        /// Base joint definition
        public B2JointDef @base;
        
        /// The target angle for the joint in radians. The spring-damper will drive
        /// to this angle.
        public float targetAngle;

        /// Enable a rotational spring on the revolute hinge axis
        public bool enableSpring;

        /// The spring stiffness Hertz, cycles per second
        public float hertz;

        /// The spring damping ratio, non-dimensional
        public float dampingRatio;

        /// A flag to enable joint limits
        public bool enableLimit;

        /// The lower angle for the joint limit in radians. Minimum of -0.99*pi radians.
        public float lowerAngle;

        /// The upper angle for the joint limit in radians. Maximum of 0.99*pi radians.
        public float upperAngle;

        /// A flag to enable the joint motor
        public bool enableMotor;

        /// The maximum motor torque, typically in newton-meters
        public float maxMotorTorque;

        /// The desired motor speed in radians per second
        public float motorSpeed;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
