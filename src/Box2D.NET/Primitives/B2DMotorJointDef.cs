// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// A motor joint is used to control the relative motion between two bodies
    ///
    /// A typical usage is to control the movement of a dynamic body with respect to the ground.
    /// @ingroup motor_joint
    public class b2MotorJointDef
    {
        /// The first attached body
        public b2BodyId bodyIdA;

        /// The second attached body
        public b2BodyId bodyIdB;

        /// Position of bodyB minus the position of bodyA, in bodyA's frame
        public b2Vec2 linearOffset;

        /// The bodyB angle minus bodyA angle in radians
        public float angularOffset;

        /// The maximum motor force in newtons
        public float maxForce;

        /// The maximum motor torque in newton-meters
        public float maxTorque;

        /// Position correction factor in the range [0,1]
        public float correctionFactor;

        /// Set this flag to true if the attached bodies should collide
        public bool collideConnected;

        /// User data pointer
        public object userData;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
