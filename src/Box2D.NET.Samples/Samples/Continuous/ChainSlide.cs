using Box2D.NET.Primitives;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Continuous;

class ChainSlide : Sample
{
public:
explicit ChainSlide( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 10.0f };
        Draw.g_camera.m_zoom = 15.0f;
    }

#ifndef NDEBUG
    b2_toiHitCount = 0;
#endif

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

        constexpr int count = 80;
        b2Vec2 points[count];

        float w = 2.0f;
        float h = 1.0f;
        float x = 20.0f, y = 0.0f;
        for (int i = 0; i < 20; ++i)
        {
            points[i] = { x, y };
            x -= w;
        }

        for (int i = 20; i < 40; ++i)
        {
            points[i] = { x, y };
            y += h;
        }

        for (int i = 40; i < 60; ++i)
        {
            points[i] = { x, y };
            x += w;
        }

        for (int i = 60; i < 80; ++i)
        {
            points[i] = { x, y };
            y -= h;
        }

        b2ChainDef chainDef = b2DefaultChainDef();
        chainDef.points = points;
        chainDef.count = count;
        chainDef.isLoop = true;

        b2CreateChain( groundId, &chainDef );
    }

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.linearVelocity = { 100.0f, 0.0f };
        bodyDef.position = { -19.5f, 0.0f + 0.5f };
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.friction = 0.0f;
        b2Circle circle = { { 0.0f, 0.0f }, 0.5f };
        b2CreateCircleShape( bodyId, &shapeDef, &circle );
    }
}

void Step( Settings& settings ) override
{
    Sample::Step( settings );

#ifndef NDEBUG
    Draw.g_draw.DrawString( 5, m_textLine, "toi hits = %d", b2_toiHitCount );
    m_textLine += m_textIncrement;
#endif
}

static Sample* Create( Settings& settings )
{
    return new ChainSlide( settings );
}
};

static int sampleChainSlide = RegisterSample( "Continuous", "Chain Slide", ChainSlide::Create );

