// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public struct SolidCircleData
{
    public B2Transform transform;
    public float radius;
    public RGBA8 rgba;

    public SolidCircleData(B2Transform transform, float radius, RGBA8 rgba)
    {
        this.transform = transform;
        this.radius = radius;
        this.rgba = rgba;
    }
}

