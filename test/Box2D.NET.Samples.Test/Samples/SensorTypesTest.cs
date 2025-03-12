using Box2D.NET.Samples.Samples.Events;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class SensorTypesTest
{
    [Test]
    public void TestSensorTypes()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new SensorTypes(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}