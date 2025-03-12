using Box2D.NET.Samples.Samples.Events;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class PlatformerTest
{
    [Test]
    public void TestPlatformer()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new Platformer(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}