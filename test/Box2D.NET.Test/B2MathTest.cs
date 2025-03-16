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

            float xn = b2UnwindLargeAngle(angle);
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

        v = b2TransformPoint(ref transform2, b2TransformPoint(ref transform1, two));

        B2Vec2 u = b2TransformPoint(ref transform, two);

        Assert.That(u.X - v.X, Is.LessThan(10.0f * FLT_EPSILON));
        Assert.That(u.Y - v.Y, Is.LessThan(10.0f * FLT_EPSILON));

        v = b2TransformPoint(ref transform1, two);
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
    }
}
