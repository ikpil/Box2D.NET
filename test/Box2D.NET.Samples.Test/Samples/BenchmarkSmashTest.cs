using Box2D.NET.Samples.Samples.Benchmarks;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class BenchmarkSmashTest
{
    [Test]
    public void TestBenchmarkSmash()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new BenchmarkSmash(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}