using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Joints;

class Ragdoll : Sample
{
public:
explicit Ragdoll( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 12.0f };
        Draw.g_camera.m_zoom = 16.0f;

        // Draw.g_camera.m_center = { 0.0f, 26.0f };
        // Draw.g_camera.m_zoom = 1.0f;
    }

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Segment segment = { { -20.0f, 0.0f }, { 20.0f, 0.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    m_jointFrictionTorque = 0.03f;
    m_jointHertz = 5.0f;
    m_jointDampingRatio = 0.5f;

    m_human = {};

    Spawn();
}

void Spawn()
{
    CreateHuman( &m_human, m_worldId, { 0.0f, 25.0f }, 1.0f, m_jointFrictionTorque, m_jointHertz, m_jointDampingRatio, 1,
    nullptr, false );
    Human_ApplyRandomAngularImpulse( &m_human, 10.0f );
}

void UpdateUI() override
{
    float height = 140.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 180.0f, height ) );

    ImGui.Begin( "Ragdoll", nullptr, ImGuiWindowFlags.NoResize );
    ImGui.PushItemWidth( 100.0f );

    if ( ImGui.SliderFloat( "Friction", &m_jointFrictionTorque, 0.0f, 1.0f, "%3.2f" ) )
    {
        Human_SetJointFrictionTorque( &m_human, m_jointFrictionTorque );
    }

    if ( ImGui.SliderFloat( "Hertz", &m_jointHertz, 0.0f, 10.0f, "%3.1f" ) )
    {
        Human_SetJointSpringHertz( &m_human, m_jointHertz );
    }

    if ( ImGui.SliderFloat( "Damping", &m_jointDampingRatio, 0.0f, 4.0f, "%3.1f" ) )
    {
        Human_SetJointDampingRatio( &m_human, m_jointDampingRatio );
    }

    if ( ImGui.Button( "Respawn" ) )
    {
        DestroyHuman( &m_human );
        Spawn();
    }
    ImGui.PopItemWidth();
    ImGui.End();
}

static Sample* Create( Settings& settings )
{
    return new Ragdoll( settings );
}

Human m_human;
float m_jointFrictionTorque;
float m_jointHertz;
float m_jointDampingRatio;
};

static int sampleRagdoll = RegisterSample( "Joints", "Ragdoll", Ragdoll::Create );

