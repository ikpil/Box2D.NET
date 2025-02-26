// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

public struct PointData
{
    public B2Vec2 position;
    public float size;
    public RGBA8 rgba;

    public PointData(B2Vec2 position, float size, RGBA8 rgba)
    {
        this.position = position;
        this.size = size;
        this.rgba = rgba;
    }
}

