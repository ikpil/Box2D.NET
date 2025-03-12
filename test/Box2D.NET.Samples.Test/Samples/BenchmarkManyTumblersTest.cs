using Box2D.NET.Samples.Samples.Benchmarks;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class BenchmarkManyTumblersTest
{
    [Test]
    public void TestBenchmarkManyTumblers()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var testObject = new BenchmarkManyTumblers(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            testObject.Step(settings);
        }
    }
}