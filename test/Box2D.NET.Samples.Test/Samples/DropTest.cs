using Box2D.NET.Samples.Samples.Continuous;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class DropTest
{
    [Test]
    public void TestDrop()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new Drop(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}