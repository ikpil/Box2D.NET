// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A motor joint is used to control the relative motion between two bodies
    /// You may move local frame A to change the target transform.
    /// A typical usage is to control the movement of a dynamic body with respect to the ground.
    /// @ingroup motor_joint
    public struct B2MotorJointDef
    {
        /// Base joint definition
        public B2JointDef @base;

        /// The maximum motor force in newtons
        public float maxForce;

        /// The maximum motor torque in newton-meters
        public float maxTorque;

        /// Position correction factor in the range [0,1]
        public float correctionFactor;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
