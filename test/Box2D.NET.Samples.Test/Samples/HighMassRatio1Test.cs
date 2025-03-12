using Box2D.NET.Samples.Samples.Robustness;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class HighMassRatio1Test
{
    [Test]
    public void TestHighMassRatio1()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new HighMassRatio1(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}