using Box2D.NET.Samples.Samples.Continuous;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test;

public class PinballTest
{
    [Test]
    public void TestPinball()
    {
        var ctx = new SampleAppContext();
        var settings = new Settings();
        var pinball = new Pinball(ctx, settings);
    }
}