using Box2D.NET.Samples.Samples.Determinisms;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class FallingHingesTest
{
    [Test]
    public void TestFallingHinges()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new FallingHinges(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}