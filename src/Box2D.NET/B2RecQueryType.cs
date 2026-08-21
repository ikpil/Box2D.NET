// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// The kind of a recorded spatial query, matching the public query and cast functions.
    public enum B2RecQueryType
    {
        // These values match the replay query kinds. Pin them explicitly to prevent enum drift.
        b2_recQueryOverlapAABB = 0,
        b2_recQueryOverlapShape = 1,
        b2_recQueryCastRay = 2,
        b2_recQueryCastShape = 3,
        b2_recQueryCollideMover = 4,
        b2_recQueryCastRayClosest = 5,
        b2_recQueryCastMover = 6,
        b2_recQueryShapeTestPoint = 7,
        b2_recQueryShapeRayCast = 8,
    }
}
