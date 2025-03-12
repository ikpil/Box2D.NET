using Box2D.NET.Samples.Samples.Shapes;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class RollingResistanceTest
{
    [Test]
    public void TestRollingResistance()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new RollingResistance(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}