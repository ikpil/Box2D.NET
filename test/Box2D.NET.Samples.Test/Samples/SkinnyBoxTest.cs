using Box2D.NET.Samples.Samples.Continuous;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class SkinnyBoxTest
{
    [Test]
    public void TestSkinnyBox()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new SkinnyBox(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}