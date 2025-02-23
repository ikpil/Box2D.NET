namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkJointGrid : Sample
{
    BenchmarkJointGrid( Settings settings ) : base( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 60.0f, -57.0f };
            Draw.g_camera.m_zoom = 25.0f * 2.5f;
            settings.enableSleep = false;
        }

        CreateJointGrid( m_worldId );
    }

    static Sample* Create( Settings settings )
    {
        return new BenchmarkJointGrid( settings );
    }
};

static int benchmarkJointGridIndex = RegisterSample( "Benchmark", "Joint Grid", BenchmarkJointGrid::Create );
