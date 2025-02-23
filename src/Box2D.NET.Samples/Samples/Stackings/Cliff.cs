namespace Box2D.NET.Samples.Samples.Stackings;

class Cliff : Sample
{
public:
explicit Cliff( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_zoom = 25.0f * 0.5f;
        Draw.g_camera.m_center = { 0.0f, 5.0f };
    }

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = { 0.0f, 0.0f };
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Polygon box = b2MakeOffsetBox( 100.0f, 1.0f, { 0.0f, -1.0f }, b2Rot_identity );
        b2CreatePolygonShape( groundId, &shapeDef, &box );

        b2Segment segment = { { -14.0f, 4.0f }, { -8.0f, 4.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );

        box = b2MakeOffsetBox( 3.0f, 0.5f, { 0.0f, 4.0f }, b2Rot_identity );
        b2CreatePolygonShape( groundId, &shapeDef, &box );

        b2Capsule capsule = { { 8.5f, 4.0f }, { 13.5f, 4.0f }, 0.5f };
        b2CreateCapsuleShape( groundId, &shapeDef, &capsule );
    }

    m_flip = false;

    for ( int i = 0; i < 9; ++i )
    {
        m_bodyIds[i] = b2_nullBodyId;
    }

    CreateBodies();
}

void CreateBodies()
{
    for ( int i = 0; i < 9; ++i )
    {
        if ( B2_IS_NON_NULL( m_bodyIds[i] ) )
        {
            b2DestroyBody( m_bodyIds[i] );
            m_bodyIds[i] = b2_nullBodyId;
        }
    }

    float sign = m_flip ? -1.0f : 1.0f;

    b2Capsule capsule = { { -0.25f, 0.0f }, { 0.25f, 0.0f }, 0.25f };
    b2Circle circle = { { 0.0f, 0.0f }, 0.5f };
    b2Polygon square = b2MakeSquare( 0.5f );

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;

    {
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.friction = 0.01f;
        bodyDef.linearVelocity = { 2.0f * sign, 0.0f };

        float offset = m_flip ? -4.0f : 0.0f;

        bodyDef.position = { -9.0f + offset, 4.25f };
        m_bodyIds[0] = b2CreateBody( m_worldId, &bodyDef );
        b2CreateCapsuleShape( m_bodyIds[0], &shapeDef, &capsule );

        bodyDef.position = { 2.0f + offset, 4.75f };
        m_bodyIds[1] = b2CreateBody( m_worldId, &bodyDef );
        b2CreateCapsuleShape( m_bodyIds[1], &shapeDef, &capsule );

        bodyDef.position = { 13.0f + offset, 4.75f };
        m_bodyIds[2] = b2CreateBody( m_worldId, &bodyDef );
        b2CreateCapsuleShape( m_bodyIds[2], &shapeDef, &capsule );
    }

    {
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.friction = 0.01f;
        bodyDef.linearVelocity = { 2.5f * sign, 0.0f };

        bodyDef.position = { -11.0f, 4.5f };
        m_bodyIds[3] = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( m_bodyIds[3], &shapeDef, &square );

        bodyDef.position = { 0.0f, 5.0f };
        m_bodyIds[4] = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( m_bodyIds[4], &shapeDef, &square );

        bodyDef.position = { 11.0f, 5.0f };
        m_bodyIds[5] = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( m_bodyIds[5], &shapeDef, &square );
    }

    {
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.friction = 0.2f;
        bodyDef.linearVelocity = { 1.5f * sign, 0.0f };

        float offset = m_flip ? 4.0f : 0.0f;

        bodyDef.position = { -13.0f + offset, 4.5f };
        m_bodyIds[6] = b2CreateBody( m_worldId, &bodyDef );
        b2CreateCircleShape( m_bodyIds[6], &shapeDef, &circle );

        bodyDef.position = { -2.0f + offset, 5.0f };
        m_bodyIds[7] = b2CreateBody( m_worldId, &bodyDef );
        b2CreateCircleShape( m_bodyIds[7], &shapeDef, &circle );

        bodyDef.position = { 9.0f + offset, 5.0f };
        m_bodyIds[8] = b2CreateBody( m_worldId, &bodyDef );
        b2CreateCircleShape( m_bodyIds[8], &shapeDef, &circle );
    }
}

void UpdateUI() override
{
    float height = 60.0f;
    ImGui.SetNextWindowPos( ImVec2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( ImVec2( 160.0f, height ) );

    ImGui.Begin( "Cliff", nullptr, ImGuiWindowFlags.NoResize );

    if ( ImGui.Button( "Flip" ) )
    {
        m_flip = !m_flip;
        CreateBodies();
    }

    ImGui.End();
}

static Sample* Create( Settings& settings )
{
    return new Cliff( settings );
}

b2BodyId m_bodyIds[9];
bool m_flip;
};

static int sampleCliff = RegisterSample( "Stacking", "Cliff", Cliff::Create );
