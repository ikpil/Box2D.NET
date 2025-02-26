// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;


namespace Box2D.NET.Samples.Primitives;

public struct CapsuleData
{
    public B2Transform transform;
    public float radius;
    public float length;
    public RGBA8 rgba;
}
