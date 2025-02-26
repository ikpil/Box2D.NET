// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Primitives
{
    /// A null joint is used to disable collision between two specific bodies.
    ///
    /// @ingroup null_joint
    public class b2NullJointDef
    {
        /// The first attached body.
        public b2BodyId bodyIdA;

        /// The second attached body.
        public b2BodyId bodyIdB;

        /// User data pointer
        public object userData;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
