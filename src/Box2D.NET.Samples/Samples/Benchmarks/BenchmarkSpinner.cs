namespace Box2D.NET.Samples.Samples.Benchmarks;

class BenchmarkSpinner : Sample
{
    public:
    explicit BenchmarkSpinner( Settings settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.0f, 32.0f };
            Draw.g_camera.m_zoom = 42.0f;
        }

#ifndef NDEBUG
        b2_toiCalls = 0;
        b2_toiHitCount = 0;
#endif

        CreateSpinner( m_worldId );
    }

    void Step( Settings settings ) override
    {
        Sample::Step( settings );

        if ( m_stepCount == 1000 && false )
        {
            // 0.1 : 46544, 25752
            // 0.25 : 5745, 1947
            // 0.5 : 2197, 660
            settings.pause = true;
        }

#ifndef NDEBUG
        DrawTextLine( "toi calls, hits = %d, %d", b2_toiCalls, b2_toiHitCount );
#endif
    }

    static Sample* Create( Settings settings )
    {
        return new BenchmarkSpinner( settings );
    }
};

static int sampleSpinner = RegisterSample( "Benchmark", "Spinner", BenchmarkSpinner::Create );
