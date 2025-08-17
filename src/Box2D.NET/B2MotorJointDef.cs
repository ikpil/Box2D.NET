// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A motor joint is used to control the relative velocity and or transform between two bodies.
    /// With a velocity of zero this acts like top-down friction.
    /// @ingroup motor_joint
    public struct B2MotorJointDef
    {
        /// Base joint definition
        public B2JointDef @base;

        /// The desired linear velocity
        public B2Vec2 linearVelocity;

        /// The maximum motor force in newtons
        public float maxVelocityForce;

        /// The desired angular velocity
        public float angularVelocity;

        /// The maximum motor torque in newton-meters
        public float maxVelocityTorque;

        /// Linear spring hertz for position control
        public float linearHertz;

        /// Linear spring damping ratio
        public float linearDampingRatio;

        /// Maximum spring force in newtons
        public float maxSpringForce;

        /// Angular spring hertz for position control
        public float angularHertz;

        /// Angular spring damping ratio
        public float angularDampingRatio;

        /// Maximum spring torque in newton-meters
        public float maxSpringTorque;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
