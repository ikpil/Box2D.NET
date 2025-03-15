using Box2D.NET.Samples.Samples.Events;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class FootSensorTest
{
    [Test]
    public void TestFootSensor()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new FootSensor(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}