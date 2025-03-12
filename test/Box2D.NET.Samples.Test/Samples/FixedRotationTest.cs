using Box2D.NET.Samples.Samples.Joints;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class FixedRotationTest
{
    [Test]
    public void TestFixedRotation()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new FixedRotation(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}