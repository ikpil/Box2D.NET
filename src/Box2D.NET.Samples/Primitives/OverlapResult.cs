// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

public class OverlapResult
{
    public b2Vec2[] points = new b2Vec2[32];
    public int count;
}
