// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// A filter joint is used to disable collision between two specific bodies.
    ///
    /// @ingroup filter_joint
    public struct b2FilterJointDef
    {
        /// Base joint definition
        public B2JointDef @base;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
