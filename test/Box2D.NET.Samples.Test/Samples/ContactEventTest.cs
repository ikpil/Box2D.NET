using Box2D.NET.Samples.Samples.Events;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class ContactEventTest
{
    [Test]
    public void TestContactEvent()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new ContactEvent(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}