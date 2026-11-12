// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A mover joint is used to move a dynamic character mover through velocity commands.
    /// The x and y directions are handled separately. Does not affect rotation.
    /// @ingroup mover_joint
    public struct B2MoverJointDef
    {
        /// Base joint definition
        public B2JointDef @base;

        /// The desired linear velocity
        public B2Vec2 linearVelocity;

        /// The maximum motor force in newtons.
        public B2Vec2 maxVelocityForce;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
