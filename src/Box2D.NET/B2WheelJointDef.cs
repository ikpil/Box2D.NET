// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Wheel joint definition
    ///
    /// This requires defining a line of motion using an axis and an anchor point.
    /// The definition uses local  anchor points and a local axis so that the initial
    /// configuration can violate the constraint slightly. The joint translation is zero
    /// when the local anchor points coincide in world space.
    /// @ingroup wheel_joint
    public class B2WheelJointDef
    {
        /// The first attached body
        public B2BodyId bodyIdA;

        /// The second attached body
        public B2BodyId bodyIdB;

        /// The local anchor point relative to bodyA's origin
        public B2Vec2 localAnchorA;

        /// The local anchor point relative to bodyB's origin
        public B2Vec2 localAnchorB;

        /// The local translation unit axis in bodyA
        public B2Vec2 localAxisA;

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

        /// Set this flag to true if the attached bodies should collide
        public bool collideConnected;

        /// User data pointer
        public object userData;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
