using Box2D.NET.Samples.Samples.Collisions;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class OverlapWorldTest
{
    [Test]
    public void TestOverlapWorld()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new OverlapWorld(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}