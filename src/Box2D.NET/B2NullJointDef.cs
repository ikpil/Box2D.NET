// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A null joint is used to disable collision between two specific bodies.
    ///
    /// @ingroup null_joint
    public struct B2NullJointDef
    {
        /// The first attached body.
        public B2BodyId bodyIdA;

        /// The second attached body.
        public B2BodyId bodyIdB;

        /// User data pointer
        public object userData;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
