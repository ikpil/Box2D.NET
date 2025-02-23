using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkCompound : Sample
{
explicit BenchmarkCompound( Settings settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 18.0f, 115.0f };
        Draw.g_camera.m_zoom = 25.0f * 5.5f;
    }

    float grid = 1.0f;
#ifdef NDEBUG
    int height = 200;
    int width = 200;
#else
    int height = 100;
    int width = 100;
#endif
    {

        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );
        b2ShapeDef shapeDef = b2DefaultShapeDef();

        for ( int i = 0; i < height; ++i )
        {
            float y = grid * i;
            for ( int j = i; j < width; ++j )
            {
                float x = grid * j;
                b2Polygon square = b2MakeOffsetBox( 0.5f * grid, 0.5f * grid, { x, y }, b2Rot_identity );
                b2CreatePolygonShape( groundId, &shapeDef, &square );
            }
        }

        for ( int i = 0; i < height; ++i )
        {
            float y = grid * i;
            for ( int j = i; j < width; ++j )
            {
                float x = -grid * j;
                b2Polygon square = b2MakeOffsetBox( 0.5f * grid, 0.5f * grid, { x, y }, b2Rot_identity );
                b2CreatePolygonShape( groundId, &shapeDef, &square );
            }
        }
    }

    {
#ifdef NDEBUG
        int span = 20;
        int count = 5;
#else
        int span = 5;
        int count = 5;
#endif

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        // defer mass properties to avoid n-squared mass computations
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.updateBodyMass = false;

        for ( int m = 0; m < count; ++m )
        {
            float ybody = ( 100.0f + m * span ) * grid;

            for ( int n = 0; n < count; ++n )
            {
                float xbody = -0.5f * grid * count * span + n * span * grid;
                bodyDef.position = { xbody, ybody };
                b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

                for ( int i = 0; i < span; ++i )
                {
                    float y = i * grid;
                    for ( int j = 0; j < span; ++j )
                    {
                        float x = j * grid;
                        b2Polygon square = b2MakeOffsetBox( 0.5f * grid, 0.5f * grid, { x, y }, b2Rot_identity );
                        b2CreatePolygonShape( bodyId, &shapeDef, &square );
                    }
                }

                // All shapes have been added so I can efficiently compute the mass properties.
                b2Body_ApplyMassFromShapes( bodyId );
            }
        }
    }
}

static Sample* Create( Settings settings )
{
    return new BenchmarkCompound( settings );
}
};

static int sampleCompound = RegisterSample( "Benchmark", "Compound", BenchmarkCompound::Create );
