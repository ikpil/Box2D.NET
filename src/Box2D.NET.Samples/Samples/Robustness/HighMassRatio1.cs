using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Robustness;

// Pyramid with heavy box on top
public class HighMassRatio1 : Sample
{
explicit HighMassRatio1( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 3.0f, 14.0f };
        Draw.g_camera.m_zoom = 25.0f;
    }

    float extent = 1.0f;

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Polygon box = b2MakeOffsetBox( 50.0f, 1.0f, { 0.0f, -1.0f }, b2Rot_identity );
        b2CreatePolygonShape( groundId, &shapeDef, &box );
    }

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        b2Polygon box = b2MakeBox( extent, extent );
        b2ShapeDef shapeDef = b2DefaultShapeDef();

        for ( int j = 0; j < 3; ++j )
        {
            int count = 10;
            float offset = -20.0f * extent + 2.0f * ( count + 1.0f ) * extent * j;
            float y = extent;
            while ( count > 0 )
            {
                for ( int i = 0; i < count; ++i )
                {
                    float coeff = i - 0.5f * count;

                    float yy = count == 1 ? y + 2.0f : y;
                    bodyDef.position = { 2.0f * coeff * extent + offset, yy };
                    b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

                    shapeDef.density = count == 1 ? ( j + 1.0f ) * 100.0f : 1.0f;
                    b2CreatePolygonShape( bodyId, &shapeDef, &box );
                }

                --count;
                y += 2.0f * extent;
            }
        }
    }
}

static Sample Create( Settings settings )
{
    return new HighMassRatio1( settings );
}
};

static int sampleIndex1 = RegisterSample( "Robustness", "HighMassRatio1", HighMassRatio1::Create );
