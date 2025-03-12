using Box2D.NET.Samples.Samples.Joints;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class BallAndChainTest
{
    [Test]
    public void TestBallAndChain()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new BallAndChain(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}