// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Ids;

namespace Box2D.NET.Samples.Samples.Replays;

internal sealed class ReplayPickContext
{
    internal B2Vec2 point;
    internal B2ShapeId shape;
}
