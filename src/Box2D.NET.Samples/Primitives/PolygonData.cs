// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public struct PolygonData
{
    public B2Transform transform;
    public B2FixedArray8<B2Vec2> points;
    public int count;
    public float radius;

    // keep color small
    public RGBA8 color;
}

