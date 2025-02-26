// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using NUnit.Framework;
using static Box2D.NET.aabb;
using static Box2D.NET.math_function;

namespace Box2D.NET.Test;

public class test_collision
{
    [Test]
    public void AABBTest()
    {
        B2AABB a;
        a.lowerBound = new B2Vec2(-1.0f, -1.0f);
        a.upperBound = new B2Vec2(-2.0f, -2.0f);

        Assert.That(b2IsValidAABB(a), Is.EqualTo(false));

        a.upperBound = new B2Vec2(1.0f, 1.0f);
        Assert.That(b2IsValidAABB(a), Is.EqualTo(true));

        B2AABB b = new B2AABB(new B2Vec2(2.0f, 2.0f), new B2Vec2(4.0f, 4.0f));
        Assert.That(b2AABB_Overlaps(a, b), Is.EqualTo(false));
        Assert.That(b2AABB_Contains(a, b), Is.EqualTo(false));

        B2Vec2 p1 = new B2Vec2(-2.0f, 0.0f);
        B2Vec2 p2 = new B2Vec2(2.0f, 0.0f);

        B2CastOutput output = b2AABB_RayCast(a, p1, p2);
        Assert.That(output.hit, Is.EqualTo(true));
        Assert.That(0.1f < output.fraction && output.fraction < 0.9f);
    }
}
