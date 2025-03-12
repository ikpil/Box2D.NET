using Box2D.NET.Samples.Samples.Collisions;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class ManifoldTest
{
    [Test]
    public void TestManifold()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new Manifold(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}