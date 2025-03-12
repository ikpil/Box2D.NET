using Box2D.NET.Samples.Samples.Robustness;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class HighMassRatio3Test
{
    [Test]
    public void TestHighMassRatio3()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new HighMassRatio3(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}