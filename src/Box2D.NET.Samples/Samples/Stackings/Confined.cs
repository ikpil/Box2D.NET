using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Stackings;

class Confined : Sample
{
public:
enum
{
    e_gridCount = 25,
    e_maxCount = e_gridCount * e_gridCount
};

explicit Confined( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 10.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.5f;
    }

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Capsule capsule;
        capsule = { { -10.5f, 0.0f }, { 10.5f, 0.0f }, 0.5f };
        b2CreateCapsuleShape( groundId, &shapeDef, &capsule );
        capsule = { { -10.5f, 0.0f }, { -10.5f, 20.5f }, 0.5f };
        b2CreateCapsuleShape( groundId, &shapeDef, &capsule );
        capsule = { { 10.5f, 0.0f }, { 10.5f, 20.5f }, 0.5f };
        b2CreateCapsuleShape( groundId, &shapeDef, &capsule );
        capsule = { { -10.5f, 20.5f }, { 10.5f, 20.5f }, 0.5f };
        b2CreateCapsuleShape( groundId, &shapeDef, &capsule );
    }

    m_row = 0;
    m_column = 0;
    m_count = 0;

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;
    bodyDef.gravityScale = 0.0f;

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    b2Circle circle = { { 0.0f, 0.0f }, 0.5f };

    while ( m_count < e_maxCount )
    {
        m_row = 0;
        for ( int i = 0; i < e_gridCount; ++i )
        {
            float x = -8.75f + m_column * 18.0f / e_gridCount;
            float y = 1.5f + m_row * 18.0f / e_gridCount;

            bodyDef.position = { x, y };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2CreateCircleShape( bodyId, &shapeDef, &circle );

            m_count += 1;
            m_row += 1;
        }
        m_column += 1;
    }
}

static Sample Create( Settings settings )
{
    return new Confined( settings );
}

int m_row;
int m_column;
int m_count;
};

static int sampleConfined = RegisterSample( "Stacking", "Confined", Confined::Create );

