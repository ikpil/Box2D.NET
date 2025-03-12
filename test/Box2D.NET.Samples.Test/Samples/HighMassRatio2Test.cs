using Box2D.NET.Samples.Samples.Robustness;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class HighMassRatio2Test
{
    [Test]
    public void TestHighMassRatio2()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new HighMassRatio2(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}