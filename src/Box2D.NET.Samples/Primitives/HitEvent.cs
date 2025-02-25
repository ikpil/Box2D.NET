// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

public struct HitEvent
{
    public b2Vec2 point;
    public float speed;
    public int stepIndex;

    public void Clear()
    {
        point = new b2Vec2();
        speed = 0.0f;
        stepIndex = 0;
    }
}
