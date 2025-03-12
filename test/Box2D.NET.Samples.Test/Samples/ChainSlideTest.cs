using Box2D.NET.Samples.Samples.Continuous;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class ChainSlideTest
{
    [Test]
    public void TestChainSlide()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new ChainSlide(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}