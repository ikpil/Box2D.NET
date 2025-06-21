// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Distance joint definition
    /// Connects a point on body A with a point on body B by a segment.
    /// Useful for ropes and springs.
    /// @ingroup distance_joint
    public struct B2DistanceJointDef
    {
        /// Base joint definition
        public B2JointDef @base;

        /// The rest length of this joint. Clamped to a stable minimum value.
        public float length;

        /// Enable the distance constraint to behave like a spring. If false
        /// then the distance joint will be rigid, overriding the limit and motor.
        public bool enableSpring;

        /// The spring linear stiffness Hertz, cycles per second
        public float hertz;

        /// The spring linear damping ratio, non-dimensional
        public float dampingRatio;

        /// Enable/disable the joint limit
        public bool enableLimit;

        /// Minimum length. Clamped to a stable minimum value.
        public float minLength;

        /// Maximum length. Must be greater than or equal to the minimum length.
        public float maxLength;

        /// Enable/disable the joint motor
        public bool enableMotor;

        /// The maximum motor force, usually in newtons
        public float maxMotorForce;

        /// The desired motor speed, usually in meters per second
        public float motorSpeed;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
