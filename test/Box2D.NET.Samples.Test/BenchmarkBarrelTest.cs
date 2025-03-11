using Box2D.NET.Samples.Samples.Benchmarks;

namespace Box2D.NET.Samples.Test;

public class BarrelTest
{
    [Test]
    public void TestPinball()
    {
        var ctx = SampleAppContext.Create();
        var settings = new Settings();
        
        var pinball = new BenchmarkBarrel(ctx, settings);

        for (int i = 0; i < 37; ++i)
        {
            pinball.Step(settings);
        }
    }

}