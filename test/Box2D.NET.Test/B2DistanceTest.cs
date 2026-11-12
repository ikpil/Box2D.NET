// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Constants;

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

        B2SegmentDistanceResult result = b2ComputeSegmentDistance(p1, q1, p2, q2);

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
        input.transform = b2Transform_identity;
        input.useRadii = false;

        B2SimplexCache cache = new B2SimplexCache();
        B2DistanceOutput output = b2ComputeShapeDistance(ref input, ref cache, null, 0);

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
        input.transform = b2Transform_identity;
        input.translationB = new B2Vec2(-2.0f, 0.0f);
        input.maxFraction = 1.0f;
        input.canEncroach = false;

        B2CastOutput output = b2CastShape(input);

        Assert.That(output.hit);
        Assert.That(output.fraction - 0.5f, Is.LessThan(0.005f));
    }

    // A thin plank and a point a linear slop off the middle of its short end face. The closest feature
    // is that end face, so GJK finishes on a two point simplex spanning it.
    //
    // b2SolveSimplex2 hands back a search direction of magnitude 2 * distance * edge^2, which the
    // caller weighs against an absolute epsilon. That test is cubic in length, so it gives out on
    // short edges: below an edge of about 3.5 mm at default length units it declares the shapes
    // overlapped and returns a zero distance with no normal, though they are plainly apart.
    [Test]
    public void ShapeDistanceShortEdgeTest()
    {
        float halfThickness = 0.0016f;

        B2Vec2[] corners =
        [
            new B2Vec2(-0.5f, -halfThickness),
            new B2Vec2(0.5f, -halfThickness),
            new B2Vec2(0.5f, halfThickness),
            new B2Vec2(-0.5f, halfThickness),
        ];

        B2Vec2[] point = [new B2Vec2(0.5f + B2_LINEAR_SLOP, 0.0f)];

        B2DistanceInput input = new B2DistanceInput();
        input.proxyA = b2MakeProxy(corners, corners.Length, 0.0f);
        input.proxyB = b2MakeProxy(point, 1, 0.0f);
        input.transform = b2Transform_identity;
        input.useRadii = false;

        B2SimplexCache cache = new B2SimplexCache();
        B2DistanceOutput output = b2ComputeShapeDistance(ref input, ref cache, null, 0);

        Assert.That(output.distance, Is.GreaterThan(0.0f));
        Assert.That(b2IsNormalized(output.normal));
        Assert.That(b2AbsFloat(output.distance - B2_LINEAR_SLOP), Is.LessThanOrEqualTo(1e-6f));
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

        B2TOIOutput output = b2ComputeTimeOfImpact(input);

        Assert.That(output.state, Is.EqualTo(B2TOIState.b2_toiStateHit));
        Assert.That(output.fraction - 0.5f, Is.LessThan(0.005f));
    }
}
