using Box2D.NET.Samples.Samples.Benchmarks;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test.Samples;

public class BenchmarkCreateDestroyTest
{
    [Test]
    public void TestBenchmarkCreateDestroy()
    {
        var ctx = SampleAppContext.CreateWithoutGLFW();
        var settings = Helpers.CreateSettings();

        using var testObject = new BenchmarkCreateDestroy(ctx, settings);

        for (int i = 0; i < 10; ++i)
        {
            testObject.Step(settings);
        }
    }
}