using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Continuous;

class Drop : Sample
{
public:
explicit Drop( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 1.5f };
        Draw.g_camera.m_zoom = 3.0f;
        settings.enableSleep = false;
        settings.drawJoints = false;
    }

#if 0
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        float w = 0.25f;
        int count = 40;

        float x = -0.5f * count * w;
        float h = 0.05f;
        for ( int j = 0; j <= count; ++j )
        {
            b2Polygon box = b2MakeOffsetBox( w, h, { x, -h }, b2Rot_identity );
            b2CreatePolygonShape( groundId, &shapeDef, &box );
            x += w;
        }
    }
#endif

    m_human = {};
    m_frameSkip = 0;
    m_frameCount = 0;
    m_continuous = true;
    m_speculative = true;

    Scene1();
}

void Clear()
{
    for ( int i = 0; i < m_bodyIds.size(); ++i )
    {
        b2DestroyBody( m_bodyIds[i] );
    }

    m_bodyIds.clear();

    if ( m_human.isSpawned )
    {
        DestroyHuman( &m_human );
    }
}

void CreateGround1()
{
    for ( int i = 0; i < m_groundIds.size(); ++i )
    {
        b2DestroyBody( m_groundIds[i] );
    }
    m_groundIds.clear();

    b2BodyDef bodyDef = b2DefaultBodyDef();
    b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();

    float w = 0.25f;
    int count = 40;
    b2Segment segment = { { -0.5f * count * w, 0.0f }, { 0.5f * count * w, 0.0f } };
    b2CreateSegmentShape( groundId, &shapeDef, &segment );

    m_groundIds.push_back( groundId );
}

void CreateGround2()
{
    for ( int i = 0; i < m_groundIds.size(); ++i )
    {
        b2DestroyBody( m_groundIds[i] );
    }
    m_groundIds.clear();

    b2BodyDef bodyDef = b2DefaultBodyDef();
    b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();

    float w = 0.25f;
    int count = 40;

    float x = -0.5f * count * w;
    float h = 0.05f;
    for ( int j = 0; j <= count; ++j )
    {
        b2Polygon box = b2MakeOffsetBox( 0.5f * w, h, { x, 0.0f }, b2Rot_identity );
        b2CreatePolygonShape( groundId, &shapeDef, &box );
        x += w;
    }

    m_groundIds.push_back( groundId );
}

void CreateGround3()
{
    for ( int i = 0; i < m_groundIds.size(); ++i )
    {
        b2DestroyBody( m_groundIds[i] );
    }
    m_groundIds.clear();

    b2BodyDef bodyDef = b2DefaultBodyDef();
    b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();

    float w = 0.25f;
    int count = 40;
    b2Segment segment = { { -0.5f * count * w, 0.0f }, { 0.5f * count * w, 0.0f } };
    b2CreateSegmentShape( groundId, &shapeDef, &segment );
    segment = { { 3.0f, 0.0f }, { 3.0f, 8.0f } };
    b2CreateSegmentShape( groundId, &shapeDef, &segment );

    m_groundIds.push_back( groundId );
}

// ball
void Scene1()
{
    Clear();
    CreateGround2();

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;
    bodyDef.position = { 0.0f, 4.0f };
    bodyDef.linearVelocity = { 0.0f, -100.0f };

    b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    b2Circle circle = { { 0.0f, 0.0f }, 0.125f };
    b2CreateCircleShape( bodyId, &shapeDef, &circle );

    m_bodyIds.push_back( bodyId );
    m_frameCount = 1;
}

// ruler
void Scene2()
{
    Clear();
    CreateGround1();

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;
    bodyDef.position = { 0.0f, 4.0f };
    bodyDef.rotation = b2MakeRot( 0.5f * B2_PI );
    bodyDef.linearVelocity = { 0.0f, 0.0f };
    bodyDef.angularVelocity = -0.5f;

    b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    b2Polygon box = b2MakeBox( 0.75f, 0.01f );
    b2CreatePolygonShape( bodyId, &shapeDef, &box );

    m_bodyIds.push_back( bodyId );
    m_frameCount = 1;
}

// ragdoll
void Scene3()
{
    Clear();
    CreateGround2();

    float jointFrictionTorque = 0.03f;
    float jointHertz = 1.0f;
    float jointDampingRatio = 0.5f;

    CreateHuman( &m_human, m_worldId, { 0.0f, 40.0f }, 1.0f, jointFrictionTorque, jointHertz, jointDampingRatio, 1, nullptr,
    true );

    m_frameCount = 1;
}

void Scene4()
{
    Clear();
    CreateGround3();

    float a = 0.25f;
    b2Polygon box = b2MakeSquare( a );

    b2ShapeDef shapeDef = b2DefaultShapeDef();

    float offset = 0.01f;

    for ( int i = 0; i < 5; ++i )
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;

        float shift = ( i % 2 == 0 ? -offset : offset );
        bodyDef.position = { 2.5f + shift, a + 2.0f * a * i };
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        m_bodyIds.push_back( bodyId );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );
    }

    b2Circle circle = { { 0.0f, 0.0f }, 0.125f };
    shapeDef.density = 4.0f;

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { -7.7f, 1.9f };
        bodyDef.linearVelocity = { 200.0f, 0.0f };
        bodyDef.isBullet = true;

        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreateCircleShape( bodyId, &shapeDef, &circle );
        m_bodyIds.push_back( bodyId );
    }

    m_frameCount = 1;
}

public override void Keyboard( int key )
{
    switch ( key )
    {
        case GLFW_KEY_1:
            Scene1();
            break;

        case GLFW_KEY_2:
            Scene2();
            break;

        case GLFW_KEY_3:
            Scene3();
            break;

        case GLFW_KEY_4:
            Scene4();
            break;

        case GLFW_KEY_C:
            Clear();
            m_continuous = !m_continuous;
            break;

        case GLFW_KEY_V:
            Clear();
            m_speculative = !m_speculative;
            b2World_EnableSpeculative( m_worldId, m_speculative );
            break;

        case GLFW_KEY_S:
            m_frameSkip = m_frameSkip > 0 ? 0 : 60;
            break;

        default:
            Sample::Keyboard( key );
            break;
    }
}

public override void Step(Settings settings)
{
#if 0
    ImGui.SetNextWindowPos( new Vector2( 0.0f, 0.0f ) );
    ImGui.SetNextWindowSize( new Vector2( float( Draw.g_camera.m_width ), float( Draw.g_camera.m_height ) ) );
    ImGui.SetNextWindowBgAlpha( 0.0f );
    ImGui.Begin( "DropBackground", nullptr,
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
        ImGuiWindowFlags.NoScrollbar );

    ImDrawList* drawList = ImGui.GetWindowDrawList();

    const char* ContinuousText = m_continuous && m_speculative ? "Continuous ON" : "Continuous OFF";
    drawList->AddText( Draw.g_draw.m_largeFont, Draw.g_draw.m_largeFont->FontSize, { 40.0f, 40.0f }, IM_COL32_WHITE, ContinuousText );

    if ( m_frameSkip > 0 )
    {
        drawList->AddText( Draw.g_draw.m_mediumFont, Draw.g_draw.m_mediumFont->FontSize, { 40.0f, 40.0f + 64.0f + 20.0f },
        IM_COL32( 200, 200, 200, 255 ), "Slow Time" );
    }

    ImGui.End();
#endif

    //if (m_frameCount == 165)
    //{
    //	settings.pause = true;
    //	m_frameSkip = 30;
    //}

    settings.enableContinuous = m_continuous;

    if ( ( m_frameSkip == 0 || m_frameCount % m_frameSkip == 0 ) && settings.pause == false )
    {
        base.Step( settings );
    }
    else
    {
        bool pause = settings.pause;
        settings.pause = true;
        base.Step( settings );
        settings.pause = pause;
    }

    m_frameCount += 1;
}

static Sample Create( Settings settings )
{
    return new Drop( settings );
}

std::vector<b2BodyId> m_groundIds;
std::vector<b2BodyId> m_bodyIds;
Human m_human;
int m_frameSkip;
int m_frameCount;
bool m_continuous;
bool m_speculative;
};

static int sampleDrop = RegisterSample( "Continuous", "Drop", Drop::Create );
