namespace Box2D.NET.Samples.Samples.Benchmarks;

class BenchmarkManyPyramids : Sample
{
    public:
    explicit BenchmarkManyPyramids( Settings settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 16.0f, 110.0f };
            Draw.g_camera.m_zoom = 25.0f * 5.0f;
            settings.enableSleep = false;
        }

        CreateManyPyramids( m_worldId );
    }

    static Sample* Create( Settings settings )
    {
        return new BenchmarkManyPyramids( settings );
    }
};

static int benchmarkManyPyramids = RegisterSample( "Benchmark", "Many Pyramids", BenchmarkManyPyramids::Create );

