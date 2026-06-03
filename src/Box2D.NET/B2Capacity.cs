// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Optional world capacities that can be used to avoid run-time allocations.
    /// @see b2World_GetMaxCapacity
    /// @ingroup world
    public struct B2Capacity
    {
        /// Number of expected static shapes.
        public int staticShapeCount;

        /// Number of expected dynamic and kinematic shapes.
        public int dynamicShapeCount;

        /// Number of expected static bodies.
        public int staticBodyCount;

        /// Number of expected dynamic and kinematic bodies.
        public int dynamicBodyCount;

        /// Number of expected contacts.
        public int contactCount;
    }
}
