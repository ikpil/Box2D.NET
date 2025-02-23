namespace Box2D.NET.Samples.Samples.Continuous;

class BounceHumans : Sample
{
public:
explicit BounceHumans( Settings& settings )
    : Sample( settings )
{
    Draw.g_camera.m_center = { 0.0f, 0.0f };
    Draw.g_camera.m_zoom = 12.0f;

    b2BodyDef bodyDef = b2DefaultBodyDef();
    b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    shapeDef.restitution = 1.3f;
    shapeDef.friction = 0.1f;

    {
        b2Segment segment = { { -10.0f, -10.0f }, { 10.0f, -10.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    {
        b2Segment segment = { { 10.0f, -10.0f }, { 10.0f, 10.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    {
        b2Segment segment = { { 10.0f, 10.0f }, { -10.0f, 10.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    {
        b2Segment segment = { { -10.0f, 10.0f }, { -10.0f, -10.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    b2Circle circle = { { 0.0f, 0.0f }, 2.0f };
    shapeDef.restitution = 2.0f;
    b2CreateCircleShape( groundId, &shapeDef, &circle );
}

void Step( Settings& settings ) override
{
    if ( m_humanCount < 5 && m_countDown <= 0.0f )
    {
        float jointFrictionTorque = 0.0f;
        float jointHertz = 1.0f;
        float jointDampingRatio = 0.1f;

        CreateHuman( m_humans + m_humanCount, m_worldId, { 0.0f, 5.0f }, 1.0f, jointFrictionTorque, jointHertz,
        jointDampingRatio, 1, nullptr, true );
        // Human_SetVelocity( m_humans + m_humanCount, { 10.0f - 5.0f * m_humanCount, -20.0f + 5.0f * m_humanCount } );

        m_countDown = 2.0f;
        m_humanCount += 1;
    }

    float timeStep = 1.0f / 60.0f;
    b2CosSin cs1 = b2ComputeCosSin( 0.5f * m_time );
    b2CosSin cs2 = b2ComputeCosSin( m_time );
    float gravity = 10.0f;
    b2Vec2 gravityVec = { gravity * cs1.sine, gravity * cs2.cosine };
    Draw.g_draw.DrawSegment( b2Vec2_zero, b2Vec2{ 3.0f * cs1.sine, 3.0f * cs2.cosine }, b2_colorWhite );
    m_time += timeStep;
    m_countDown -= timeStep;
    b2World_SetGravity( m_worldId, gravityVec );

    Sample::Step( settings );
}

static Sample* Create( Settings& settings )
{
    return new BounceHumans( settings );
}

Human m_humans[5] = {};
int m_humanCount = 0;
float m_countDown = 0.0f;
float m_time = 0.0f;
};

static int sampleBounceHumans = RegisterSample( "Continuous", "Bounce Humans", BounceHumans::Create );

