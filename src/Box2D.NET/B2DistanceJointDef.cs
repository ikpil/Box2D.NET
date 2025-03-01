// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Distance joint definition
    ///
    /// This requires defining an anchor point on both
    /// bodies and the non-zero distance of the distance joint. The definition uses
    /// local anchor points so that the initial configuration can violate the
    /// constraint slightly. This helps when saving and loading a game.
    /// @ingroup distance_joint
    public ref struct B2DistanceJointDef
    {
        /// The first attached body
        public B2BodyId bodyIdA;

        /// The second attached body
        public B2BodyId bodyIdB;

        /// The local anchor point relative to bodyA's origin
        public B2Vec2 localAnchorA;

        /// The local anchor point relative to bodyB's origin
        public B2Vec2 localAnchorB;

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

        /// Set this flag to true if the attached bodies should collide
        public bool collideConnected;

        /// User data pointer
        public object userData;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
