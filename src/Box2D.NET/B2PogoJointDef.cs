// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Pogo joint definition
    /// @ingroup pogo_joint
    public struct B2PogoJointDef
    {
        /// Base joint definition
        public B2JointDef @base;

        /// Normal vector at the contact
        public B2Vec2 normal;

        /// The spring stiffness Hertz, cycles per second
        public float hertz;

        /// The spring damping ratio, non-dimensional
        public float dampingRatio;

        /// Spring length.
        public float restLength;

        /// Maximum tension force. Controls how sticky the mover is to ground.
        public float maxTensionForce;

        /// Maximum compression force. How hard the pogo pushes the mover up from the ground.
        public float maxCompressionForce;

        /// The initial pogo internal impulse. The pogo is typically recreated every frame. This
        /// gives a way to move the internal state forward for smoother spring behavior.
        public float impulse;

        /// The initial pogo internal velocity. The pogo is typically recreated every frame. This
        /// gives a way to move the internal state forward for smoother spring behavior.
        public float velocity;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
