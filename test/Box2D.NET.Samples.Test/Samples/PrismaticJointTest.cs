using Box2D.NET.Samples.Samples.Joints;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class PrismaticJointTest
{
    [Test]
    public void TestPrismaticJoint()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new PrismaticJoint(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}