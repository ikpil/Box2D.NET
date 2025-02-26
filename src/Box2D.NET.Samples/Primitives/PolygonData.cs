// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

public struct PolygonData
{
    public B2Transform transform;
    public B2Vec2 p1, p2, p3, p4, p5, p6, p7, p8;
    public int count;
    public float radius;

    // keep color small
    public RGBA8 color;
}

