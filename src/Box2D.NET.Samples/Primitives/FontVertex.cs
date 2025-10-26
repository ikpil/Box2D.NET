// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public struct FontVertex
{
    public B2Vec2 position;
    public B2Vec2 uv;
    public RGBA8 color;

    public FontVertex(B2Vec2 position, B2Vec2 uv, RGBA8 color)
    {
        this.position = position;
        this.uv = uv;
        this.color = color;
    }
}
