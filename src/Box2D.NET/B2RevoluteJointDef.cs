// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Revolute joint definition
    /// A point on body B is fixed to a point on body A. Allows relative rotation.
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
