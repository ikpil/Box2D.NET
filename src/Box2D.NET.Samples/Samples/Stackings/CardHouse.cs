using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Stackings;

// From PEEL
class CardHouse : Sample
{
public:
explicit CardHouse( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.75f, 0.9f };
        Draw.g_camera.m_zoom = 25.0f * 0.05f;
    }

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.position = { 0.0f, -2.0f };
    b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    shapeDef.friction = 0.7f;

    b2Polygon groundBox = b2MakeBox( 40.0f, 2.0f );
    b2CreatePolygonShape( groundId, &shapeDef, &groundBox );

    float cardHeight = 0.2f;
    float cardThickness = 0.001f;

    float angle0 = 25.0f * B2_PI / 180.0f;
    float angle1 = -25.0f * B2_PI / 180.0f;
    float angle2 = 0.5f * B2_PI;

    b2Polygon cardBox = b2MakeBox( cardThickness, cardHeight );
    bodyDef.type = b2BodyType.b2_dynamicBody;

    int Nb = 5;
    float z0 = 0.0f;
    float y = cardHeight - 0.02f;
    while ( Nb )
    {
        float z = z0;
        for ( int i = 0; i < Nb; i++ )
        {
            if ( i != Nb - 1 )
            {
                bodyDef.position = { z + 0.25f, y + cardHeight - 0.015f };
                bodyDef.rotation = b2MakeRot( angle2 );
                b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
                b2CreatePolygonShape( bodyId, &shapeDef, &cardBox );
            }

            bodyDef.position = { z, y };
            bodyDef.rotation = b2MakeRot( angle1 );
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2CreatePolygonShape( bodyId, &shapeDef, &cardBox );

            z += 0.175f;

            bodyDef.position = { z, y };
            bodyDef.rotation = b2MakeRot( angle0 );
            bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2CreatePolygonShape( bodyId, &shapeDef, &cardBox );

            z += 0.175f;
        }
        y += cardHeight * 2.0f - 0.03f;
        z0 += 0.175f;
        Nb--;
    }
}

static Sample Create( Settings settings )
{
    return new CardHouse( settings );
}
};

static int sampleCardHouse = RegisterSample( "Stacking", "Card House", CardHouse::Create );