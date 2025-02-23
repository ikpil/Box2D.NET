namespace Box2D.NET.Samples.Samples.Benchmarks;

class BenchmarkLargePyramid : Sample
{
    public:
    explicit BenchmarkLargePyramid( Settings settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.0f, 50.0f };
            Draw.g_camera.m_zoom = 25.0f * 2.2f;
            settings.enableSleep = false;
        }

        CreateLargePyramid( m_worldId );
    }

    static Sample* Create( Settings settings )
    {
        return new BenchmarkLargePyramid( settings );
    }
};

static int benchmarkLargePyramid = RegisterSample( "Benchmark", "Large Pyramid", BenchmarkLargePyramid::Create );
