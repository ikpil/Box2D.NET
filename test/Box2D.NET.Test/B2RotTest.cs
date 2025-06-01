// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2MathFunction;
using System;

namespace Box2D.NET.Test;

public class B2RotTest
{
    [Test]
    public void Test_B2Rot_Constructor()
    {
        var rot = b2MakeRot(B2_PI / 4);
        Assert.That(rot.c, Is.EqualTo(MathF.Cos(B2_PI / 4)).Within(FLT_EPSILON), "Cosine of 45 degrees should match MathF.Cos");
        Assert.That(rot.s, Is.EqualTo(MathF.Sin(B2_PI / 4)).Within(FLT_EPSILON), "Sine of 45 degrees should match MathF.Sin");

        var zeroRot = b2MakeRot(0.0f);
        Assert.That(zeroRot.c, Is.EqualTo(1.0f).Within(FLT_EPSILON), "Cosine of 0 degrees should be 1");
        Assert.That(zeroRot.s, Is.EqualTo(0.0f).Within(FLT_EPSILON), "Sine of 0 degrees should be 0");
    }

    [Test]
    public void Test_B2Rot_Normalization()
    {
        var rot = new B2Rot(2.0f, 0.0f);
        var norm = b2NormalizeRot(rot);
        Assert.That(norm.c, Is.EqualTo(1.0f).Within(FLT_EPSILON), "Normalized cosine should be 1");
        Assert.That(norm.s, Is.EqualTo(0.0f).Within(FLT_EPSILON), "Normalized sine should be 0");
        Assert.That(norm.c * norm.c + norm.s * norm.s, Is.EqualTo(1.0f).Within(FLT_EPSILON), "Normalized rotation should have unit length");
    }

    [Test]
    public void Test_B2Rot_VectorRotation()
    {
        var rot = b2MakeRot(B2_PI / 2);
        var vec = new B2Vec2(1.0f, 0.0f);
        var result = b2RotateVector(rot, vec);
        Assert.That(result.X, Is.EqualTo(0.0f).Within(FLT_EPSILON), "90-degree rotation should map (1,0) to (0,1)");
        Assert.That(result.Y, Is.EqualTo(1.0f).Within(FLT_EPSILON), "90-degree rotation should map (1,0) to (0,1)");
    }

    [Test]
    public void Test_B2Rot_Integration()
    {
        var rot = b2MakeRot(0.0f);
        float deltaAngle = B2_PI / 2;
        var result = b2IntegrateRotation(rot, deltaAngle);
        float expectedMag = MathF.Sqrt(1.0f + (B2_PI / 2) * (B2_PI / 2));
        float expectedC = 1.0f / expectedMag;
        float expectedS = (B2_PI / 2) / expectedMag;
        Assert.That(result.c, Is.EqualTo(expectedC).Within(0.0001f), "Integrated rotation cosine should match expected value");
        Assert.That(result.s, Is.EqualTo(expectedS).Within(0.0001f), "Integrated rotation sine should match expected value");
        Assert.That(result.c * result.c + result.s * result.s, Is.EqualTo(1.0f).Within(0.0001f), "Integrated rotation should remain normalized");
    }

    [Test]
    public void Test_B2Rot_Multiplication()
    {
        var rot1 = b2MakeRot(B2_PI / 4);
        var rot2 = b2MakeRot(B2_PI / 4);
        var result = b2MulRot(rot1, rot2);
        Assert.That(result.c, Is.EqualTo(0.0f).Within(FLT_EPSILON), "Multiplying two 45-degree rotations should give 90 degrees");
        Assert.That(result.s, Is.EqualTo(1.0f).Within(FLT_EPSILON), "Multiplying two 45-degree rotations should give 90 degrees");
    }

    [Test]
    public void Test_B2Rot_Validation()
    {
        var validRot = b2MakeRot(B2_PI / 4);
        Assert.That(b2IsValidRotation(validRot), Is.True, "Normalized rotation should be valid");

        var invalidRot = new B2Rot(2.0f, 0.0f);
        Assert.That(b2IsValidRotation(invalidRot), Is.False, "Non-normalized rotation should be invalid");

        var nanRot = new B2Rot(float.NaN, 0.0f);
        Assert.That(b2IsValidRotation(nanRot), Is.False, "Rotation with NaN should be invalid");
    }

    [Test]
    public void Test_B2Rot_GetAngle()
    {
        var rot = b2MakeRot(B2_PI / 4);
        Assert.That(b2Rot_GetAngle(rot), Is.EqualTo(B2_PI / 4).Within(0.0001f), "GetAngle should return the original angle");
    }

    [Test]
    public void Test_B2Rot_GetAxes()
    {
        var rot = b2MakeRot(B2_PI / 2);
        var xAxis = b2Rot_GetXAxis(rot);
        var yAxis = b2Rot_GetYAxis(rot);
        Assert.That(xAxis.X, Is.EqualTo(0.0f).Within(FLT_EPSILON), "90-degree rotation X-axis should point up");
        Assert.That(xAxis.Y, Is.EqualTo(1.0f).Within(FLT_EPSILON), "90-degree rotation X-axis should point up");
        Assert.That(yAxis.X, Is.EqualTo(-1.0f).Within(FLT_EPSILON), "90-degree rotation Y-axis should point left");
        Assert.That(yAxis.Y, Is.EqualTo(0.0f).Within(FLT_EPSILON), "90-degree rotation Y-axis should point left");
    }

    [Test]
    public void Test_B2Rot_InvMulRot()
    {
        var rot1 = b2MakeRot(B2_PI / 4);
        var rot2 = b2MakeRot(B2_PI / 4);
        var result = b2InvMulRot(rot1, rot2);
        Assert.That(result.c, Is.EqualTo(1.0f).Within(FLT_EPSILON), "Inverse multiplication of same rotations should give identity");
        Assert.That(result.s, Is.EqualTo(0.0f).Within(FLT_EPSILON), "Inverse multiplication of same rotations should give identity");
    }

    [Test]
    public void Test_B2Rot_RelativeAngle()
    {
        var rot1 = b2MakeRot(0.0f);
        var rot2 = b2MakeRot(B2_PI / 2);
        Assert.That(b2RelativeAngle(rot2, rot1), Is.EqualTo(B2_PI / 2).Within(FLT_EPSILON), "Relative angle between 0 and 90 degrees should be 90 degrees");
    }

    [Test]
    public void Test_B2Rot_UnwindAngle()
    {
        Assert.That(b2UnwindAngle(3 * B2_PI / 2), Is.EqualTo(-B2_PI / 2).Within(FLT_EPSILON), "Angle greater than PI should be normalized to [-PI, PI]");
        Assert.That(b2UnwindAngle(-3 * B2_PI / 2), Is.EqualTo(B2_PI / 2).Within(FLT_EPSILON), "Angle less than -PI should be normalized to [-PI, PI]");
        Assert.That(b2UnwindAngle(B2_PI), Is.EqualTo(B2_PI).Within(FLT_EPSILON), "PI should remain unchanged");
        Assert.That(b2UnwindAngle(-B2_PI), Is.EqualTo(-B2_PI).Within(FLT_EPSILON), "-PI should remain unchanged");
        Assert.That(b2UnwindAngle(5 * B2_PI), Is.EqualTo(-B2_PI).Within(0.0001f), "Large angle should be normalized to [-PI, PI]");
        Assert.That(b2UnwindAngle(-5 * B2_PI), Is.EqualTo(B2_PI).Within(0.0001f), "Large negative angle should be normalized to [-PI, PI]");
    }

    [Test]
    public void Test_B2Rot_InvRotateVector()
    {
        var rot = b2MakeRot(B2_PI / 2);
        var vec = new B2Vec2(0.0f, 1.0f);
        var result = b2InvRotateVector(rot, vec);
        Assert.That(result.X, Is.EqualTo(1.0f).Within(FLT_EPSILON), "Inverse rotation should map (0,1) back to (1,0)");
        Assert.That(result.Y, Is.EqualTo(0.0f).Within(FLT_EPSILON), "Inverse rotation should map (0,1) back to (1,0)");
    }
}
