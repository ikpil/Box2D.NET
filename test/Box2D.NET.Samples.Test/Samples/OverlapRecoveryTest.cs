using Box2D.NET.Samples.Samples.Robustness;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class OverlapRecoveryTest
{
    [Test]
    public void TestOverlapRecovery()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new OverlapRecovery(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}