using Box2D.NET.Samples.Samples.Joints;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class BreakableJointTest
{
    [Test]
    public void TestBreakableJoint()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new BreakableJoint(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}