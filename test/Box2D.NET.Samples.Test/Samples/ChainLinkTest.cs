using Box2D.NET.Samples.Samples.Shapes;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class ChainLinkTest
{
    [Test]
    public void TestChainLink()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new ChainLink(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}