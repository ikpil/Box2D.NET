using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Continuous;

class BounceHouse : Sample
{
public:
enum ShapeType
{
    e_circleShape = 0,
    e_capsuleShape,
    e_boxShape
};

struct HitEvent
{
    b2Vec2 point;
    float speed;
    int stepIndex;
};

public BounceHouse( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 0.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.45f;
    }

    b2BodyDef bodyDef = b2DefaultBodyDef();
    b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();
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

    m_shapeType = e_boxShape;
    m_bodyId = b2_nullBodyId;
    m_enableHitEvents = true;

    memset( m_hitEvents, 0, sizeof( m_hitEvents ) );

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
    bodyDef.linearVelocity = { 10.0f, 20.0f };
    bodyDef.position = { 0.0f, 0.0f };
    bodyDef.gravityScale = 0.0f;

    // Circle shapes centered on the body can spin fast without risk of tunnelling.
    bodyDef.allowFastRotation = m_shapeType == e_circleShape;

    m_bodyId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    shapeDef.density = 1.0f;
    shapeDef.restitution = 1.2f;
    shapeDef.friction = 0.3f;
    shapeDef.enableHitEvents = m_enableHitEvents;

    if ( m_shapeType == e_circleShape )
    {
        b2Circle circle = { { 0.0f, 0.0f }, 0.5f };
        b2CreateCircleShape( m_bodyId, &shapeDef, &circle );
    }
    else if ( m_shapeType == e_capsuleShape )
    {
        b2Capsule capsule = { { -0.5f, 0.0f }, { 0.5f, 0.0 }, 0.25f };
        b2CreateCapsuleShape( m_bodyId, &shapeDef, &capsule );
    }
    else
    {
        float h = 0.1f;
        b2Polygon box = b2MakeBox( 20.0f * h, h );
        b2CreatePolygonShape( m_bodyId, &shapeDef, &box );
    }
}

public override void UpdateUI()
{
    float height = 100.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 240.0f, height ) );

    ImGui.Begin( "Bounce House", nullptr, ImGuiWindowFlags.NoResize );

    const char* shapeTypes[] = { "Circle", "Capsule", "Box" };
    int shapeType = int( m_shapeType );
    if ( ImGui.Combo( "Shape", &shapeType, shapeTypes, IM_ARRAYSIZE( shapeTypes ) ) )
    {
        m_shapeType = ShapeType( shapeType );
        Launch();
    }

    if ( ImGui.Checkbox( "hit events", &m_enableHitEvents ) )
    {
        b2Body_EnableHitEvents( m_bodyId, m_enableHitEvents );
    }

    ImGui.End();
}

public override void Step(Settings settings)
{
    base.Step( settings );

    b2ContactEvents events = b2World_GetContactEvents( m_worldId );
    for ( int i = 0; i < events.hitCount; ++i )
    {
        b2ContactHitEvent* event = events.hitEvents + i;

        HitEvent* e = m_hitEvents + 0;
        for ( int j = 1; j < 4; ++j )
        {
            if ( m_hitEvents[j].stepIndex < e->stepIndex )
            {
                e = m_hitEvents + j;
            }
        }

        e->point = event->point;
        e->speed = event->approachSpeed;
        e->stepIndex = m_stepCount;
    }

    for ( int i = 0; i < 4; ++i )
    {
        HitEvent* e = m_hitEvents + i;
        if ( e->stepIndex > 0 && m_stepCount <= e->stepIndex + 30 )
        {
            Draw.g_draw.DrawCircle( e->point, 0.1f, b2HexColor.b2_colorOrangeRed );
            Draw.g_draw.DrawString( e->point, "%.1f", e->speed );
        }
    }

    if ( m_stepCount == 1000 )
    {
        m_stepCount += 0;
    }
}

static Sample Create( Settings settings )
{
    return new BounceHouse( settings );
}

HitEvent m_hitEvents[4];
b2BodyId m_bodyId;
ShapeType m_shapeType;
bool m_enableHitEvents;
};

static int sampleBounceHouse = RegisterSample( "Continuous", "Bounce House", BounceHouse::Create );
