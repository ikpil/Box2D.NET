using System.Diagnostics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Robustness;

class OverlapRecovery : Sample
{
public:
explicit OverlapRecovery( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 2.5f };
        Draw.g_camera.m_zoom = 25.0f * 0.15f;
    }

    m_bodyIds = nullptr;
    m_bodyCount = 0;
    m_baseCount = 4;
    m_overlap = 0.25f;
    m_extent = 0.5f;
    m_pushout = 3.0f;
    m_hertz = 30.0f;
    m_dampingRatio = 10.0f;

    b2BodyDef bodyDef = b2DefaultBodyDef();
    b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

    float groundWidth = 40.0f;
    b2ShapeDef shapeDef = b2DefaultShapeDef();
    shapeDef.density = 1.0f;

    b2Segment segment = { { -groundWidth, 0.0f }, { groundWidth, 0.0f } };
    b2CreateSegmentShape( groundId, &shapeDef, &segment );

    CreateScene();
}

~OverlapRecovery() override
{
    free( m_bodyIds );
}

void CreateScene()
{
    for ( int i = 0; i < m_bodyCount; ++i )
    {
        b2DestroyBody( m_bodyIds[i] );
    }

    b2World_SetContactTuning( m_worldId, m_hertz, m_dampingRatio, m_pushout );

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;

    b2Polygon box = b2MakeBox( m_extent, m_extent );
    b2ShapeDef shapeDef = b2DefaultShapeDef();
    shapeDef.density = 1.0f;

    m_bodyCount = m_baseCount * ( m_baseCount + 1 ) / 2;
    m_bodyIds = (b2BodyId*)realloc( m_bodyIds, m_bodyCount * sizeof( b2BodyId ) );

    int bodyIndex = 0;
    float fraction = 1.0f - m_overlap;
    float y = m_extent;
    for ( int i = 0; i < m_baseCount; ++i )
    {
        float x = fraction * m_extent * ( i - m_baseCount );
        for ( int j = i; j < m_baseCount; ++j )
        {
            bodyDef.position = { x, y };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

            b2CreatePolygonShape( bodyId, &shapeDef, &box );

            m_bodyIds[bodyIndex++] = bodyId;

            x += 2.0f * fraction * m_extent;
        }

        y += 2.0f * fraction * m_extent;
    }

    Debug.Assert( bodyIndex == m_bodyCount );
}

void UpdateUI() override
{
    float height = 210.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 220.0f, height ) );

    ImGui.Begin( "Overlap Recovery", nullptr, ImGuiWindowFlags.NoResize );
    ImGui.PushItemWidth( 100.0f );

    bool changed = false;
    changed = changed || ImGui.SliderFloat( "Extent", &m_extent, 0.1f, 1.0f, "%.1f" );
    changed = changed || ImGui.SliderInt( "Base Count", &m_baseCount, 1, 10 );
    changed = changed || ImGui.SliderFloat( "Overlap", &m_overlap, 0.0f, 1.0f, "%.2f" );
    changed = changed || ImGui.SliderFloat( "Pushout", &m_pushout, 0.0f, 10.0f, "%.1f" );
    changed = changed || ImGui.SliderFloat( "Hertz", &m_hertz, 0.0f, 120.0f, "%.f" );
    changed = changed || ImGui.SliderFloat( "Damping Ratio", &m_dampingRatio, 0.0f, 20.0f, "%.1f" );
    changed = changed || ImGui.Button( "Reset Scene" );

    if ( changed )
    {
        CreateScene();
    }

    ImGui.PopItemWidth();
    ImGui.End();
}

static Sample* Create( Settings& settings )
{
    return new OverlapRecovery( settings );
}

b2BodyId* m_bodyIds;
int m_bodyCount;
int m_baseCount;
float m_overlap;
float m_extent;
float m_pushout;
float m_hertz;
float m_dampingRatio;
};

static int sampleIndex4 = RegisterSample( "Robustness", "Overlap Recovery", OverlapRecovery::Create );