using Box2D.NET.Samples.Samples.Collisions;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class ShapeCastTest
{
    [Test]
    public void TestShapeCast()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new ShapeCast(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}