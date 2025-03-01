// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Weld joint definition
    ///
    /// A weld joint connect to bodies together rigidly. This constraint provides springs to mimic
    /// soft-body simulation.
    /// @note The approximate solver in Box2D cannot hold many bodies together rigidly
    /// @ingroup weld_joint
    public ref struct B2WeldJointDef
    {
        /// The first attached body
        public B2BodyId bodyIdA;

        /// The second attached body
        public B2BodyId bodyIdB;

        /// The local anchor point relative to bodyA's origin
        public B2Vec2 localAnchorA;

        /// The local anchor point relative to bodyB's origin
        public B2Vec2 localAnchorB;

        /// The bodyB angle minus bodyA angle in the reference state (radians)
        public float referenceAngle;

        /// Linear stiffness expressed as Hertz (cycles per second). Use zero for maximum stiffness.
        public float linearHertz;

        /// Angular stiffness as Hertz (cycles per second). Use zero for maximum stiffness.
        public float angularHertz;

        /// Linear damping ratio, non-dimensional. Use 1 for critical damping.
        public float linearDampingRatio;

        /// Linear damping ratio, non-dimensional. Use 1 for critical damping.
        public float angularDampingRatio;

        /// Set this flag to true if the attached bodies should collide
        public bool collideConnected;

        /// User data pointer
        public object userData;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
