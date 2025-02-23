using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Robustness;

// Big box on small boxes
public class HighMassRatio2 : Sample
{
    static Sample Create( Settings settings )
    {
        return new HighMassRatio2( settings );
    }
    static int sampleIndex2 = RegisterSample( "Robustness", "HighMassRatio2", Create );

public HighMassRatio2( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 16.5f };
        Draw.g_camera.m_zoom = 25.0f;
    }

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
        b2ShapeDef shapeDef = b2DefaultShapeDef();

        float extent = 1.0f;
        b2Polygon smallBox = b2MakeBox( 0.5f * extent, 0.5f * extent );
        b2Polygon bigBox = b2MakeBox( 10.0f * extent, 10.0f * extent );

        {
            bodyDef.position = { -9.0f * extent, 0.5f * extent };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2CreatePolygonShape( bodyId, &shapeDef, &smallBox );
        }

        {
            bodyDef.position = { 9.0f * extent, 0.5f * extent };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2CreatePolygonShape( bodyId, &shapeDef, &smallBox );
        }

        {
            bodyDef.position = { 0.0f, ( 10.0f + 16.0f ) * extent };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2CreatePolygonShape( bodyId, &shapeDef, &bigBox );
        }
    }
}

}

