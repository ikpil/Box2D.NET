using Box2D.NET.Samples.Samples.Bodies;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class BadBodyTest
{
    [Test]
    public void TestBadBody()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new BadBody(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}