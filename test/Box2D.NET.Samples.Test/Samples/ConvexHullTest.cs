using Box2D.NET.Samples.Samples.Geometries;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class ConvexHullTest
{
    [Test]
    public void TestConvexHull()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new ConvexHull(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}