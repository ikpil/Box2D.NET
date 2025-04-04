using Box2D.NET.Samples.Samples.Joints;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class RagdollTest
{
    [Test]
    public void TestRagdoll()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new Ragdoll(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}