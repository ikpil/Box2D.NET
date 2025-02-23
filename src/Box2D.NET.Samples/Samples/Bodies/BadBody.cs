using Box2D.NET.Primitives;
using static Box2D.NET.id;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Primitives;

class BadBody : Sample
{
public:
explicit BadBody( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 2.3f, 10.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.5f;
    }

    b2BodyId groundId = b2_nullBodyId;
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        groundId = b2CreateBody( m_worldId, &bodyDef );

        b2Segment segment = { { -20.0f, 0.0f }, { 20.0f, 0.0f } };
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    // Build a bad body
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { 0.0f, 3.0f };
        bodyDef.angularVelocity = 0.5f;
        bodyDef.rotation = b2MakeRot( 0.25f * B2_PI );

        m_badBodyId = b2CreateBody( m_worldId, &bodyDef );

        b2Capsule capsule = { { 0.0f, -1.0f }, { 0.0f, 1.0f }, 1.0f };
        b2ShapeDef shapeDef = b2DefaultShapeDef();

        // density set to zero intentionally to create a bad body
        shapeDef.density = 0.0f;
        b2CreateCapsuleShape( m_badBodyId, &shapeDef, &capsule );
    }

    // Build a normal body
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { 2.0f, 3.0f };
        bodyDef.rotation = b2MakeRot( 0.25f * B2_PI );

        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2Capsule capsule = { { 0.0f, -1.0f }, { 0.0f, 1.0f }, 1.0f };
        b2ShapeDef shapeDef = b2DefaultShapeDef();

        b2CreateCapsuleShape( bodyId, &shapeDef, &capsule );
    }
}

void Step( Settings& settings ) override
{
    Sample::Step( settings );

    Draw.g_draw.DrawString( 5, m_textLine, "A bad body is a dynamic body with no mass and behaves like a kinematic body." );
    m_textLine += m_textIncrement;

    Draw.g_draw.DrawString( 5, m_textLine, "Bad bodies are considered invalid and a user bug. Behavior is not guaranteed." );
    m_textLine += m_textIncrement;

    // For science
    b2Body_ApplyForceToCenter( m_badBodyId, { 0.0f, 10.0f }, true );
}

static Sample* Create( Settings& settings )
{
    return new BadBody( settings );
}

b2BodyId m_badBodyId;
};

static int sampleBadBody = RegisterSample( "Bodies", "Bad", BadBody::Create );
