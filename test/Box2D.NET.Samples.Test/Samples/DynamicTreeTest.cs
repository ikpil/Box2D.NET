using Box2D.NET.Samples.Samples.Collisions;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class DynamicTreeTest
{
    [Test]
    public void TestDynamicTree()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new DynamicTree(ctx, settings);
        
        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}