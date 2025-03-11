using System;
using Box2D.NET.Samples.Samples.Continuous;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test;

public class PinballTest
{
    [Test]
    public void TestPinball()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var pinball = new Pinball(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            pinball.Step(settings);
        }
    }
}