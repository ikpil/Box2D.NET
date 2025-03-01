// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public struct CapsuleData
{
    public B2Transform transform;
    public float radius;
    public float length;
    public RGBA8 rgba;

    public CapsuleData(B2Transform transform, float radius, float length, RGBA8 rgba)
    {
        this.transform = transform;
        this.radius = radius;
        this.length = length;
        this.rgba = rgba;
    }
}