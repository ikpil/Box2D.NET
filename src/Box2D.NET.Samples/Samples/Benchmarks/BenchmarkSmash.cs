namespace Box2D.NET.Samples.Samples.Benchmarks;

class BenchmarkSmash : Sample
{
    public:
    explicit BenchmarkSmash( Settings settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 60.0f, 6.0f };
            Draw.g_camera.m_zoom = 25.0f * 1.6f;
        }

        CreateSmash( m_worldId );
    }

    static Sample* Create( Settings settings )
    {
        return new BenchmarkSmash( settings );
    }
};

static int sampleSmash = RegisterSample( "Benchmark", "Smash", BenchmarkSmash::Create );
