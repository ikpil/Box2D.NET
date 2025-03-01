// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// The body simulation type.
    /// Each body is one of these three types. The type determines how the body behaves in the simulation.
    /// @ingroup body
    public enum B2BodyType
    {
        /// zero mass, zero velocity, may be manually moved
        b2_staticBody = 0,

        /// zero mass, velocity set by user, moved by solver
        b2_kinematicBody = 1,

        /// positive mass, velocity determined by forces, moved by solver
        b2_dynamicBody = 2,

        /// number of body types
        b2_bodyTypeCount,
    }
}
