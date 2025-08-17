using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkWasher : Sample
{
    private static int SampleBenchmarkWasher = SampleFactory.Shared.RegisterSample("Benchmark", "Washer", Create);

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkWasher(context);
    }

    private BenchmarkWasher(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(1.5f, 10.0f);
            m_context.camera.m_zoom = 20.0f;
        }

        CreateWasher(m_worldId, true);
    }
}