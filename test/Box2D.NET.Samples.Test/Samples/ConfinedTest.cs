using Box2D.NET.Samples.Samples.Stackings;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class ConfinedTest
{
    [Test]
    public void TestConfined()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new Confined(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}