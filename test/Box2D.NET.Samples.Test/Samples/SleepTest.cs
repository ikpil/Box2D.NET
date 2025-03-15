using Box2D.NET.Samples.Samples.Bodies;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class SleepTest
{
    [Test]
    public void TestSleep()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new Sleep(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}