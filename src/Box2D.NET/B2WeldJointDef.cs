// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Weld joint definition
    /// Connects two bodies together rigidly. This constraint provides springs to mimic
    /// soft-body simulation.
    /// @note The approximate solver in Box2D cannot hold many bodies together rigidly
    /// @ingroup weld_join
    public struct B2WeldJointDef
    {
        /// Base joint definition
        public B2JointDef @base;
            
        /// Linear stiffness expressed as Hertz (cycles per second). Use zero for maximum stiffness.
        public float linearHertz;

        /// Angular stiffness as Hertz (cycles per second). Use zero for maximum stiffness.
        public float angularHertz;

        /// Linear damping ratio, non-dimensional. Use 1 for critical damping.
        public float linearDampingRatio;

        /// Linear damping ratio, non-dimensional. Use 1 for critical damping.
        public float angularDampingRatio;

        /// Used internally to detect a valid definition. DO NOT SET.
        public int internalValue;
    }
}
