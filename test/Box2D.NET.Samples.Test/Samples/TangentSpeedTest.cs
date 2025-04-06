﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Samples.Samples.Shapes;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class TangentSpeedTest
{
    [Test]
    public void TestTangentSpeed()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new TangentSpeed(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}
