using Box2D.NET.Samples.Samples.Benchmarks;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class BenchmarkKinematicTest
{
    [Test]
    public void TestBenchmarkKinematic()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new BenchmarkKinematic(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}