﻿namespace Box2D.NET.Samples.Samples.Continuous;

class ChainDrop : Sample
{
public:
explicit ChainDrop( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 0.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.35f;
    }

    // 
    //b2World_SetContactTuning( m_worldId, 30.0f, 1.0f, 100.0f );

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.position = { 0.0f, -6.0f };
    b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

    b2Vec2 points[4] = { { -10.0f, -2.0f }, { 10.0f, -2.0f }, { 10.0f, 1.0f }, { -10.0f, 1.0f } };

    b2ChainDef chainDef = b2DefaultChainDef();
    chainDef.points = points;
    chainDef.count = 4;
    chainDef.isLoop = true;

    b2CreateChain( groundId, &chainDef );

    m_bodyId = b2_nullBodyId;
    m_yOffset = -0.1f;
    m_speed = -42.0f;

    Launch();
}

void Launch()
{
    if ( B2_IS_NON_NULL( m_bodyId ) )
    {
        b2DestroyBody( m_bodyId );
    }

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;
    bodyDef.linearVelocity = { 0.0f, m_speed };
    bodyDef.position = { 0.0f, 10.0f + m_yOffset };
    bodyDef.rotation = b2MakeRot( 0.5f * B2_PI );
    bodyDef.fixedRotation = true;
    m_bodyId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();

    b2Circle circle = { { 0.0f, 0.0f }, 0.5f };
    m_shapeId = b2CreateCircleShape( m_bodyId, &shapeDef, &circle );

    //b2Capsule capsule = { { -0.5f, 0.0f }, { 0.5f, 0.0 }, 0.25f };
    //m_shapeId = b2CreateCapsuleShape( m_bodyId, &shapeDef, &capsule );

    //float h = 0.5f;
    //b2Polygon box = b2MakeBox( h, h );
    //m_shapeId = b2CreatePolygonShape( m_bodyId, &shapeDef, &box );
}

void UpdateUI() override
{
    float height = 140.0f;
    ImGui.SetNextWindowPos( ImVec2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( ImVec2( 240.0f, height ) );

    ImGui.Begin( "Chain Drop", nullptr, ImGuiWindowFlags.NoResize );

    ImGui.SliderFloat( "Speed", &m_speed, -100.0f, 0.0f, "%.0f" );
    ImGui.SliderFloat( "Y Offset", &m_yOffset, -1.0f, 1.0f, "%.1f" );

    if ( ImGui.Button( "Launch" ) )
    {
        Launch();
    }

    ImGui.End();
}

static Sample* Create( Settings& settings )
{
    return new ChainDrop( settings );
}

b2BodyId m_bodyId;
b2ShapeId m_shapeId;
float m_yOffset;
float m_speed;
};

static int sampleChainDrop = RegisterSample( "Continuous", "Chain Drop", ChainDrop::Create );
