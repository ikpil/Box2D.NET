namespace Box2D.NET.Samples.Samples.Benchmarks;

class BenchmarkTumbler : Sample
{
    public:
    explicit BenchmarkTumbler( Settings settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 1.5f, 10.0f };
            Draw.g_camera.m_zoom = 25.0f * 0.6f;
        }

        CreateTumbler( m_worldId );
    }

    static Sample* Create( Settings settings )
    {
        return new BenchmarkTumbler( settings );
    }
};

static int benchmarkTumbler = RegisterSample( "Benchmark", "Tumbler", BenchmarkTumbler::Create );
