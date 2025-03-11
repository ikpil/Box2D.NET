using Box2D.NET.Samples.Samples.Benchmarks;
using NUnit.Framework;

namespace Box2D.NET.Samples.Test;

public class BenchmarkBarrelTest
{
    [Test]
    public void TestPinball()
    {
        var ctx = SampleAppContext.Create();
        var settings = Helpers.CreateSettings();

        using var barrel = new BenchmarkBarrel(ctx, settings);

        for (int i = 0; i < 120; ++i)
        {
            barrel.Step(settings);
        }
    }
}