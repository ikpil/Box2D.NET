using Box2D.NET.Samples.Samples.Joints;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class DistanceJointTest
{
    [Test]
    public void TestDistanceJoint()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new DistanceJoint(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}