using Box2D.NET.Samples.Samples.Shapes;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class FrictionTest
{
    [Test]
    public void TestFriction()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new Friction(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}