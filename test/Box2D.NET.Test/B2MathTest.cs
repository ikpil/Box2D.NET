// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using NUnit.Framework;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Test;

public class B2MathTest
{
    // 0.0023 degrees
    public const float ATAN_TOL = 0.00004f;

    [Test]
    public void MathTest()
    {
        for (float t = -10.0f; t < 10.0f; t += 0.01f)
        {
            float angle = B2_PI * t;
            B2Rot r = b2MakeRot(angle);
            float c = MathF.Cos(angle);
            float s = MathF.Sin(angle);

            // The cosine and sine approximations are accurate to about 0.1 degrees (0.002 radians)
            // Console.Write( "%g %g\n", r.c - c, r.s - s );
            Assert.That(r.c - c, Is.LessThan(0.002f));
            Assert.That(r.s - s, Is.LessThan(0.002f));

            float xn = b2UnwindAngle(angle);
            Assert.That(-B2_PI <= xn && xn <= B2_PI, Is.True);

            float a = b2Atan2(s, c);
            Assert.That(b2IsValidFloat(a));

            float diff = b2AbsFloat(a - xn);

            // The two results can be off by 360 degrees (-pi and pi)
            if (diff > B2_PI)
            {
                diff -= 2.0f * B2_PI;
            }

            // The approximate atan2 is quite accurate
            Assert.That(diff, Is.LessThan(ATAN_TOL));
        }

        for (float y = -1.0f; y <= 1.0f; y += 0.01f)
        {
            for (float x = -1.0f; x <= 1.0f; x += 0.01f)
            {
                float a1 = b2Atan2(y, x);
                float a2 = MathF.Atan2(y, x);
                float diff = b2AbsFloat(a1 - a2);
                Assert.That(b2IsValidFloat(a1));
                Assert.That(diff, Is.LessThan(ATAN_TOL));
            }
        }

        {
            float a1 = b2Atan2(1.0f, 0.0f);
            float a2 = MathF.Atan2(1.0f, 0.0f);
            float diff = b2AbsFloat(a1 - a2);
            Assert.That(b2IsValidFloat(a1));
            Assert.That(diff, Is.LessThan(ATAN_TOL));
        }

        {
            float a1 = b2Atan2(-1.0f, 0.0f);
            float a2 = MathF.Atan2(-1.0f, 0.0f);
            float diff = b2AbsFloat(a1 - a2);
            Assert.That(b2IsValidFloat(a1));
            Assert.That(diff, Is.LessThan(ATAN_TOL));
        }

        {
            float a1 = b2Atan2(0.0f, 1.0f);
            float a2 = MathF.Atan2(0.0f, 1.0f);
            float diff = b2AbsFloat(a1 - a2);
            Assert.That(b2IsValidFloat(a1));
            Assert.That(diff, Is.LessThan(ATAN_TOL));
        }

        {
            float a1 = b2Atan2(0.0f, -1.0f);
            float a2 = MathF.Atan2(0.0f, -1.0f);
            float diff = b2AbsFloat(a1 - a2);
            Assert.That(b2IsValidFloat(a1));
            Assert.That(diff, Is.LessThan(ATAN_TOL));
        }

        {
            float a1 = b2Atan2(0.0f, 0.0f);
            float a2 = MathF.Atan2(0.0f, 0.0f);
            float diff = b2AbsFloat(a1 - a2);
            Assert.That(b2IsValidFloat(a1));
            Assert.That(diff, Is.LessThan(ATAN_TOL));
        }

        B2Vec2 zero = b2Vec2_zero;
        B2Vec2 one = new B2Vec2(1.0f, 1.0f);
        B2Vec2 two = new B2Vec2(2.0f, 2.0f);

        B2Vec2 v = b2Add(one, two);
        Assert.That(v.X == 3.0f && v.Y == 3.0f);

        v = b2Sub(zero, two);
        Assert.That(v.X == -2.0f && v.Y == -2.0f);

        v = b2Add(two, two);
        Assert.That(v.X != 5.0f && v.Y != 5.0f);

        B2Transform transform1 = new B2Transform(new B2Vec2(-2.0f, 3.0f), b2MakeRot(1.0f));
        B2Transform transform2 = new B2Transform(new B2Vec2(1.0f, 0.0f), b2MakeRot(-2.0f));

        B2Transform transform = b2MulTransforms(transform2, transform1);

        v = b2TransformPoint(transform2, b2TransformPoint(transform1, two));

        B2Vec2 u = b2TransformPoint(transform, two);

        Assert.That(u.X - v.X, Is.LessThan(10.0f * FLT_EPSILON));
        Assert.That(u.Y - v.Y, Is.LessThan(10.0f * FLT_EPSILON));

        v = b2TransformPoint(transform1, two);
        v = b2InvTransformPoint(transform1, v);

        Assert.That(v.X - two.X, Is.LessThan(8.0f * FLT_EPSILON));
        Assert.That(v.Y - two.Y, Is.LessThan(8.0f * FLT_EPSILON));

        v = b2Normalize(new B2Vec2(0.2f, -0.5f));
        for (float y = -1.0f; y <= 1.0f; y += 0.01f)
        {
            for (float x = -1.0f; x <= 1.0f; x += 0.01f)
            {
                if (x == 0.0f && y == 0.0f)
                {
                    continue;
                }

                u = b2Normalize(new B2Vec2(x, y));

                B2Rot r = b2ComputeRotationBetweenUnitVectors(v, u);

                B2Vec2 w = b2RotateVector(r, v);
                Assert.That(w.X - u.X, Is.LessThan(4.0f * FLT_EPSILON));
                Assert.That(w.Y - u.Y, Is.LessThan(4.0f * FLT_EPSILON));
            }
        }

        // NLerp of b2Rot has an error of over 4 degrees.
        // 2D quaternions should have an error under 1 degree.
        B2Rot q1 = b2Rot_identity;
        B2Rot q2 = b2MakeRot(0.5f * B2_PI);
        int n = 100;
        for (int i = 0; i <= n; ++i)
        {
            float alpha = (float)i / (float)n;
            B2Rot q = b2NLerp(q1, q2, alpha);
            float angle = b2Rot_GetAngle(q);
            Assert.That(alpha * 0.5f * B2_PI - angle, Is.LessThan(5.0f * B2_PI / 180.0f));
            //printf("angle = [%g %g %g]\n", alpha, alpha * 0.5f * B2_PI, angle);
        }

        // Test relative angle
        float baseAngle = 0.75f * B2_PI;
        q1 = b2MakeRot(baseAngle);
        for (float t = -10.0f; t < 10.0f; t += 0.01f)
        {
            float angle = B2_PI * t;
            q2 = b2MakeRot(angle);

            float relativeAngle = b2RelativeAngle(q1, q2);
            float unwoundAngle = b2UnwindAngle(angle - baseAngle);
            float tolerance = 0.1f * B2_PI / 180.0f;
            Assert.That(relativeAngle - unwoundAngle, Is.LessThan(tolerance));
        }
    }

    // World position boundary helpers. With large world mode off these collapse to the float
    // ops, so the round trips hold in both builds.
    [Test]
    public void WorldPositionTest()
    {
        B2Vec2 d = new B2Vec2(0.25f, -0.5f);
        B2Pos @base = new B2Pos(10.0f, -20.0f);
        B2Pos p = b2OffsetPos(@base, d);
        B2Vec2 back = b2SubPos(p, @base);
        Assert.That(MathF.Abs(back.X - d.X), Is.LessThanOrEqualTo(8.0f * FLT_EPSILON));
        Assert.That(MathF.Abs(back.Y - d.Y), Is.LessThanOrEqualTo(8.0f * FLT_EPSILON));

        B2Vec2 r = b2ToVec2(@base);
        Assert.That(r.X == 10.0f && r.Y == -20.0f, Is.True);

        Assert.That(b2IsValidPosition(p), Is.True);
        Assert.That(b2IsValidPosition(b2Pos_zero), Is.True);
        Assert.That(b2IsValidWorldTransform(b2WorldTransform_identity), Is.True);

        B2WorldTransform wt = new B2WorldTransform(new B2Pos(3.0f, -4.0f), b2MakeRot(0.7f));
        Assert.That(b2IsValidWorldTransform(wt), Is.True);

        // Local to world to local round trip
        B2Vec2 local = new B2Vec2(1.5f, 2.5f);
        B2Pos world = b2TransformWorldPoint(wt, local);
        B2Vec2 backLocal = b2InvTransformWorldPoint(wt, world);
        Assert.That(MathF.Abs(backLocal.X - local.X), Is.LessThanOrEqualTo(8.0f * FLT_EPSILON));
        Assert.That(MathF.Abs(backLocal.Y - local.Y), Is.LessThanOrEqualTo(8.0f * FLT_EPSILON));

        // Relative transform of B in A matches a float reference at modest coordinates
        B2WorldTransform A = new B2WorldTransform(new B2Pos(-2.0f, 3.0f), b2MakeRot(1.0f));
        B2WorldTransform B = new B2WorldTransform(new B2Pos(1.0f, 0.0f), b2MakeRot(-2.0f));
        B2Transform rel = b2InvMulWorldTransforms(A, B);
        B2Transform refA = new B2Transform(b2ToVec2(A.p), A.q);
        B2Transform refB = new B2Transform(b2ToVec2(B.p), B.q);
        B2Transform @ref = b2InvMulTransforms(refA, refB);
        Assert.That(MathF.Abs(rel.p.X - @ref.p.X), Is.LessThanOrEqualTo(8.0f * FLT_EPSILON));
        Assert.That(MathF.Abs(rel.p.Y - @ref.p.Y), Is.LessThanOrEqualTo(8.0f * FLT_EPSILON));
    }
}
