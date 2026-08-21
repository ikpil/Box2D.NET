// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    internal enum B2RecQueryKind
    {
        B2_RECQ_OVERLAP_AABB,
        B2_RECQ_OVERLAP_SHAPE,
        B2_RECQ_CAST_RAY,
        B2_RECQ_CAST_SHAPE,
        B2_RECQ_COLLIDE_MOVER,
        B2_RECQ_CAST_RAY_CLOSEST,
        B2_RECQ_CAST_MOVER,
        B2_RECQ_SHAPE_TEST_POINT,
        B2_RECQ_SHAPE_RAY_CAST,
    }
}
