namespace Box2D.NET.Samples.Samples.Benchmarks;

class BenchmarkRain : Sample
{
    public:
    explicit BenchmarkRain( Settings settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.0f, 110.0f };
            Draw.g_camera.m_zoom = 125.0f;
            settings.enableSleep = true;
        }

        settings.drawJoints = false;

        CreateRain( m_worldId );
    }

    void Step( Settings settings ) override
    {
        if ( settings.pause == false || settings.singleStep == true )
        {
            StepRain( m_worldId, m_stepCount );
        }

        Sample::Step( settings );

        if ( m_stepCount % 1000 == 0 )
        {
            m_stepCount += 0;
        }
    }

    static Sample* Create( Settings settings )
    {
        return new BenchmarkRain( settings );
    }
};

static int benchmarkRain = RegisterSample( "Benchmark", "Rain", BenchmarkRain::Create );
