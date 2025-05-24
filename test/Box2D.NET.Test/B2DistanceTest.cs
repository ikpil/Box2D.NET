// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Distances;

namespace Box2D.NET.Test;

public class B2DistanceTest
{
    [Test]
    public void SegmentDistanceTest()
    {
        B2Vec2 p1 = new B2Vec2(-1.0f, -1.0f);
        B2Vec2 q1 = new B2Vec2(-1.0f, 1.0f);
        B2Vec2 p2 = new B2Vec2(2.0f, 0.0f);
        B2Vec2 q2 = new B2Vec2(1.0f, 0.0f);

        B2SegmentDistanceResult result = b2SegmentDistance(p1, q1, p2, q2);

        Assert.That(result.fraction1 - 0.5f, Is.LessThan(FLT_EPSILON));
        Assert.That(result.fraction2 - 1.0f, Is.LessThan(FLT_EPSILON));
        Assert.That(result.closest1.X + 1.0f, Is.LessThan(FLT_EPSILON));
        Assert.That(result.closest1.Y, Is.LessThan(FLT_EPSILON));
        Assert.That(result.closest2.X - 1.0f, Is.LessThan(FLT_EPSILON));
        Assert.That(result.closest2.Y, Is.LessThan(FLT_EPSILON));
        Assert.That(result.distanceSquared - 4.0f, Is.LessThan(FLT_EPSILON));
    }

    [Test]
    public void ShapeDistanceTest()
    {
        B2Vec2[] vas =
        [
            new B2Vec2(-1.0f, -1.0f),
            new B2Vec2(1.0f, -1.0f),
            new B2Vec2(1.0f, 1.0f),
            new B2Vec2(-1.0f, 1.0f)
        ];
        B2Vec2[] vbs =
        [
            new B2Vec2(2.0f, -1.0f),
            new B2Vec2(2.0f, 1.0f)
        ];

        B2DistanceInput input = new B2DistanceInput();
        input.proxyA = b2MakeProxy(vas, vas.Length, 0.0f);
        input.proxyB = b2MakeProxy(vbs, vbs.Length, 0.0f);
        input.transformA = b2Transform_identity;
        input.transformB = b2Transform_identity;
        input.useRadii = false;

        B2SimplexCache cache = new B2SimplexCache();
        B2DistanceOutput output = b2ShapeDistance(ref input, ref cache, null, 0);

        Assert.That(output.distance - 1.0f, Is.LessThan(FLT_EPSILON));
    }

    [Test]
    public void ShapeCastTest()
    {
        B2Vec2[] vas =
        [
            new B2Vec2(-1.0f, -1.0f),
            new B2Vec2(1.0f, -1.0f),
            new B2Vec2(1.0f, 1.0f),
            new B2Vec2(-1.0f, 1.0f)
        ];

        B2Vec2[] vbs =
        [
            new B2Vec2(2.0f, -1.0f),
            new B2Vec2(2.0f, 1.0f)
        ];

        B2ShapeCastPairInput input = new B2ShapeCastPairInput();
        input.proxyA = b2MakeProxy(vas, vas.Length, 0.0f);
        input.proxyB = b2MakeProxy(vbs, vbs.Length, 0.0f);
        input.transformA = b2Transform_identity;
        input.transformB = b2Transform_identity;
        input.translationB = new B2Vec2(-2.0f, 0.0f);
        input.maxFraction = 1.0f;

        B2CastOutput output = b2ShapeCast(ref input);

        Assert.That(output.hit);
        Assert.That(output.fraction - 0.5f, Is.LessThan(0.005f));
    }

    [Test]
    public void TimeOfImpactTest()
    {
        B2Vec2[] vas =
        [
            new B2Vec2(-1.0f, -1.0f),
            new B2Vec2(1.0f, -1.0f),
            new B2Vec2(1.0f, 1.0f),
            new B2Vec2(-1.0f, 1.0f)
        ];

        B2Vec2[] vbs =
        [
            new B2Vec2(2.0f, -1.0f),
            new B2Vec2(2.0f, 1.0f)
        ];

        B2TOIInput input = new B2TOIInput();
        input.proxyA = b2MakeProxy(vas, vas.Length, 0.0f);
        input.proxyB = b2MakeProxy(vbs, vbs.Length, 0.0f);
        input.sweepA = new B2Sweep(b2Vec2_zero, b2Vec2_zero, b2Vec2_zero, b2Rot_identity, b2Rot_identity);
        input.sweepB = new B2Sweep(b2Vec2_zero, b2Vec2_zero, new B2Vec2(-2.0f, 0.0f), b2Rot_identity, b2Rot_identity);
        input.maxFraction = 1.0f;

        B2TOIOutput output = b2TimeOfImpact(ref input);

        Assert.That(output.state, Is.EqualTo(B2TOIState.b2_toiStateHit));
        Assert.That(output.fraction - 0.5f, Is.LessThan(0.005f));
    }
}
