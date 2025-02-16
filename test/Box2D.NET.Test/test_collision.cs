// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.aabb;
using static Box2D.NET.math_function;

namespace Box2D.NET.Test;

public class test_collision
{
    [Test]
    public void AABBTest()
    {
        b2AABB a;
        a.lowerBound = new b2Vec2
        {
            x = -1.0f, y = -1.0f
        };

        a.upperBound = new b2Vec2
        {
            x = -2.0f, y = -2.0f
        };

        Assert.That(b2IsValidAABB(a), Is.EqualTo(false));

        a.upperBound = new b2Vec2
        {
            x = 1.0f, y = 1.0f
        };
        Assert.That(b2IsValidAABB(a), Is.EqualTo(true));

        b2AABB b = new b2AABB { lowerBound = { x = 2.0f, y = 2.0f }, upperBound = { x = 4.0f, y = 4.0f } };
        Assert.That(b2AABB_Overlaps(a, b), Is.EqualTo(false));
        Assert.That(b2AABB_Contains(a, b), Is.EqualTo(false));

        b2Vec2 p1 = new b2Vec2
        {
            x = -2.0f, y = 0.0f
        };
        b2Vec2 p2 = new b2Vec2
        {
            x = 2.0f, y = 0.0f
        };

        b2CastOutput output = b2AABB_RayCast(a, p1, p2);
        Assert.That(output.hit, Is.EqualTo(true));
        Assert.That(0.1f < output.fraction && output.fraction < 0.9f);
    }
}