using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Events;

// This shows how to process body events.
class BodyMove : Sample
{
public:
enum
{
    e_count = 50
};

explicit BodyMove( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 2.0f, 8.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.55f;
    }

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.friction = 0.1f;

        b2Polygon box = b2MakeOffsetBox( 12.0f, 0.1f, { -10.0f, -0.1f }, b2MakeRot( -0.15f * B2_PI ) );
        b2CreatePolygonShape( groundId, &shapeDef, &box );

        box = b2MakeOffsetBox( 12.0f, 0.1f, { 10.0f, -0.1f }, b2MakeRot( 0.15f * B2_PI ) );
        b2CreatePolygonShape( groundId, &shapeDef, &box );

        shapeDef.restitution = 0.8f;

        box = b2MakeOffsetBox( 0.1f, 10.0f, { 19.9f, 10.0f }, b2Rot_identity );
        b2CreatePolygonShape( groundId, &shapeDef, &box );

        box = b2MakeOffsetBox( 0.1f, 10.0f, { -19.9f, 10.0f }, b2Rot_identity );
        b2CreatePolygonShape( groundId, &shapeDef, &box );

        box = b2MakeOffsetBox( 20.0f, 0.1f, { 0.0f, 20.1f }, b2Rot_identity );
        b2CreatePolygonShape( groundId, &shapeDef, &box );
    }

    m_sleepCount = 0;
    m_count = 0;

    m_explosionPosition = { 0.0f, -5.0f };
    m_explosionRadius = 10.0f;
    m_explosionMagnitude = 10.0f;
}

void CreateBodies()
{
    b2Capsule capsule = { { -0.25f, 0.0f }, { 0.25f, 0.0f }, 0.25f };
    b2Circle circle = { { 0.0f, 0.0f }, 0.35f };
    b2Polygon square = b2MakeSquare( 0.35f );

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;
    b2ShapeDef shapeDef = b2DefaultShapeDef();

    float x = -5.0f, y = 10.0f;
    for ( int i = 0; i < 10 && m_count < e_count; ++i )
    {
        bodyDef.position = { x, y };
        bodyDef.userData = m_bodyIds + m_count;
        m_bodyIds[m_count] = b2CreateBody( m_worldId, &bodyDef );
        m_sleeping[m_count] = false;

        int remainder = m_count % 4;
        if ( remainder == 0 )
        {
            b2CreateCapsuleShape( m_bodyIds[m_count], &shapeDef, &capsule );
        }
        else if ( remainder == 1 )
        {
            b2CreateCircleShape( m_bodyIds[m_count], &shapeDef, &circle );
        }
        else if ( remainder == 2 )
        {
            b2CreatePolygonShape( m_bodyIds[m_count], &shapeDef, &square );
        }
        else
        {
            b2Polygon poly = RandomPolygon( 0.75f );
            poly.radius = 0.1f;
            b2CreatePolygonShape( m_bodyIds[m_count], &shapeDef, &poly );
        }

        m_count += 1;
        x += 1.0f;
    }
}

void UpdateUI() override
{
    float height = 100.0f;
    ImGui.SetNextWindowPos( ImVec2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( ImVec2( 240.0f, height ) );

    ImGui.Begin( "Body Move", nullptr, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize );

    if ( ImGui.Button( "Explode" ) )
    {
        b2ExplosionDef def = b2DefaultExplosionDef();
        def.position = m_explosionPosition;
        def.radius = m_explosionRadius;
        def.falloff = 0.1f;
        def.impulsePerLength = m_explosionMagnitude;
        b2World_Explode( m_worldId, &def );
    }

    ImGui.SliderFloat( "Magnitude", &m_explosionMagnitude, -20.0f, 20.0f, "%.1f" );

    ImGui.End();
}

void Step( Settings& settings ) override
{
    if ( settings.pause == false && ( m_stepCount & 15 ) == 15 && m_count < e_count )
    {
        CreateBodies();
    }

    Sample::Step( settings );

    // Process body events
    b2BodyEvents events = b2World_GetBodyEvents( m_worldId );
    for ( int i = 0; i < events.moveCount; ++i )
    {
        // draw the transform of every body that moved (not sleeping)
        const b2BodyMoveEvent* event = events.moveEvents + i;
        Draw.g_draw.DrawTransform( event->transform );

        // this shows a somewhat contrived way to track body sleeping
        b2BodyId* bodyId = static_cast<b2BodyId*>( event->userData );
        ptrdiff_t diff = bodyId - m_bodyIds;
        bool* sleeping = m_sleeping + diff;

        if ( event->fellAsleep )
        {
            *sleeping = true;
            m_sleepCount += 1;
        }
        else
        {
            if ( *sleeping )
            {
                *sleeping = false;
                m_sleepCount -= 1;
            }
        }
    }

    Draw.g_draw.DrawCircle( m_explosionPosition, m_explosionRadius, b2_colorAzure );

    Draw.g_draw.DrawString( 5, m_textLine, "sleep count: %d", m_sleepCount );
    m_textLine += m_textIncrement;
}

static Sample* Create( Settings& settings )
{
    return new BodyMove( settings );
}

b2BodyId m_bodyIds[e_count];
bool m_sleeping[e_count];
int m_count;
int m_sleepCount;
b2Vec2 m_explosionPosition;
float m_explosionRadius;
float m_explosionMagnitude;
};

static int sampleBodyMove = RegisterSample( "Events", "Body Move", BodyMove::Create );
