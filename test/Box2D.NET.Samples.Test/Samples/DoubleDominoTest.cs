using Box2D.NET.Samples.Samples.Stackings;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class DoubleDominoTest
{
    [Test]
    public void TestDoubleDomino()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new DoubleDomino(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}