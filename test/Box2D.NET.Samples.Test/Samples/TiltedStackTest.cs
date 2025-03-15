using Box2D.NET.Samples.Samples.Stackings;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class TiltedStackTest
{
    [Test]
    public void TestTiltedStack()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new TiltedStack(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}