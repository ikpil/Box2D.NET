using Box2D.NET.Samples.Samples.Benchmarks;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class BenchmarkSleepTest
{
    [Test]
    public void TestBenchmarkSleep()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new BenchmarkSleep(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}