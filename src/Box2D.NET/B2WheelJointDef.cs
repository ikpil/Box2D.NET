// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Wheel joint definition
    /// Body B is a wheel that may rotate freely and slide along the local x-axis in frame A.
    /// The joint translation is zero when the local frame origins coincide in world space.
    /// @ingroup wheel_joint
    public struct B2WheelJointDef
    {
        /// Base joint definition
        public B2JointDef @base;

        /// Enable a linear spring along the local axis
        public bool enableSpring;

        /// Spring stiffness in Hertz
        public float hertz;

        /// Spring damping ratio, non-dimensional
        public float dampingRatio;

        /// Enable/disable the joint linear limit
        public bool enableLimit;

        /// The lower translation limit
        public float lowerTranslation;

        /// The upper translation limit
        public float upperTranslation;

        /// Enable/disable the joint rotational motor
        public bool enableMotor;

        /// The maximum motor torque, typically in newton-meters
        public float maxMotorTorque;

        /// The desired motor speed in radians per second
        public float motorSpeed;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
