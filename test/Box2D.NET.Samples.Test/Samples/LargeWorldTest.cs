using Box2D.NET.Samples.Samples.Worlds;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class LargeWorldTest
{
    [Test]
    public void TestLargeWorld()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new LargeWorld(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}