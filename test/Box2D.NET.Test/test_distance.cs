// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using NUnit.Framework;
using static Box2D.NET.math_function;
using static Box2D.NET.distance;
using static Box2D.NET.core;

namespace Box2D.NET.Test;

public class test_distance
{
    [Test]
    public void SegmentDistanceTest()
    {
        b2Vec2 p1 = new b2Vec2(-1.0f, -1.0f);
        b2Vec2 q1 = new b2Vec2(-1.0f, 1.0f);
        b2Vec2 p2 = new b2Vec2(2.0f, 0.0f);
        b2Vec2 q2 = new b2Vec2(1.0f, 0.0f);

        b2SegmentDistanceResult result = b2SegmentDistance(p1, q1, p2, q2);

        Assert.That(result.fraction1 - 0.5f, Is.LessThan(FLT_EPSILON));
        Assert.That(result.fraction2 - 1.0f, Is.LessThan(FLT_EPSILON));
        Assert.That(result.closest1.x + 1.0f, Is.LessThan(FLT_EPSILON));
        Assert.That(result.closest1.y, Is.LessThan(FLT_EPSILON));
        Assert.That(result.closest2.x - 1.0f, Is.LessThan(FLT_EPSILON));
        Assert.That(result.closest2.y, Is.LessThan(FLT_EPSILON));
        Assert.That(result.distanceSquared - 4.0f, Is.LessThan(FLT_EPSILON));
    }

    [Test]
    public void ShapeDistanceTest()
    {
        b2Vec2[] vas =
        [
            new b2Vec2(-1.0f, -1.0f),
            new b2Vec2(1.0f, -1.0f),
            new b2Vec2(1.0f, 1.0f),
            new b2Vec2(-1.0f, 1.0f)
        ];
        b2Vec2[] vbs =
        [
            new b2Vec2(2.0f, -1.0f),
            new b2Vec2(2.0f, 1.0f)
        ];

        b2DistanceInput input = new b2DistanceInput();
        input.proxyA = b2MakeProxy(vas, B2_ARRAY_COUNT(vas), 0.0f);
        input.proxyB = b2MakeProxy(vbs, B2_ARRAY_COUNT(vbs), 0.0f);
        input.transformA = b2Transform_identity;
        input.transformB = b2Transform_identity;
        input.useRadii = false;

        b2SimplexCache cache = new b2SimplexCache();
        b2DistanceOutput output = b2ShapeDistance(ref cache, ref input, null, 0);

        Assert.That(output.distance - 1.0f, Is.LessThan(FLT_EPSILON));
    }

    [Test]
    public void ShapeCastTest()
    {
        b2Vec2[] vas =
        [
            new b2Vec2(-1.0f, -1.0f),
            new b2Vec2(1.0f, -1.0f),
            new b2Vec2(1.0f, 1.0f),
            new b2Vec2(-1.0f, 1.0f)
        ];

        b2Vec2[] vbs =
        [
            new b2Vec2(2.0f, -1.0f),
            new b2Vec2(2.0f, 1.0f)
        ];

        b2ShapeCastPairInput input = new b2ShapeCastPairInput();
        input.proxyA = b2MakeProxy(vas, B2_ARRAY_COUNT(vas), 0.0f);
        input.proxyB = b2MakeProxy(vbs, B2_ARRAY_COUNT(vbs), 0.0f);
        input.transformA = b2Transform_identity;
        input.transformB = b2Transform_identity;
        input.translationB = new b2Vec2(-2.0f, 0.0f);
        input.maxFraction = 1.0f;

        b2CastOutput output = b2ShapeCast(ref input);

        Assert.That(output.hit);
        Assert.That(output.fraction - 0.5f, Is.LessThan(0.005f));
    }

    [Test]
    public void TimeOfImpactTest()
    {
        b2Vec2[] vas =
        [
            new b2Vec2(-1.0f, -1.0f),
            new b2Vec2(1.0f, -1.0f),
            new b2Vec2(1.0f, 1.0f),
            new b2Vec2(-1.0f, 1.0f)
        ];

        b2Vec2[] vbs =
        [
            new b2Vec2(2.0f, -1.0f),
            new b2Vec2(2.0f, 1.0f)
        ];

        b2TOIInput input = new b2TOIInput();
        input.proxyA = b2MakeProxy(vas, B2_ARRAY_COUNT(vas), 0.0f);
        input.proxyB = b2MakeProxy(vbs, B2_ARRAY_COUNT(vbs), 0.0f);
        input.sweepA = new b2Sweep(b2Vec2_zero, b2Vec2_zero, b2Vec2_zero, b2Rot_identity, b2Rot_identity);
        input.sweepB = new b2Sweep(b2Vec2_zero, b2Vec2_zero, new b2Vec2(-2.0f, 0.0f), b2Rot_identity, b2Rot_identity);
        input.maxFraction = 1.0f;

        b2TOIOutput output = b2TimeOfImpact(input);

        Assert.That(output.state, Is.EqualTo(b2TOIState.b2_toiStateHit));
        Assert.That(output.fraction - 0.5f, Is.LessThan(0.005f));
    }
}
