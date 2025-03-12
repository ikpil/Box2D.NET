using Box2D.NET.Samples.Samples.Stackings;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class CircleStackTest
{
    [Test]
    public void TestCircleStack()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new CircleStack(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}