using Box2D.NET.Primitives;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Stackings;

// A simple circle stack that also shows how to collect hit events
class CircleStack : Sample
{
public:
struct Event
{
    int indexA, indexB;
};

explicit CircleStack( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 5.0f };
        Draw.g_camera.m_zoom = 6.0f;
    }

    int shapeIndex = 0;

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.userData = reinterpret_cast<void*>( intptr_t( shapeIndex ) );
        shapeIndex += 1;

        b2Segment segment = { { -10.0f, 0.0f }, { 10.0f, 0.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    b2World_SetGravity( m_worldId, { 0.0f, -20.0f } );
    b2World_SetContactTuning( m_worldId, 0.25f * 360.0f, 10.0f, 3.0f );

    b2Circle circle = {};
    circle.radius = 0.25f;

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    shapeDef.enableHitEvents = true;
    shapeDef.rollingResistance = 0.2f;

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;

    float y = 0.5f;

    for ( int i = 0; i < 1; ++i )
    {
        bodyDef.position.y = y;

        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        shapeDef.userData = reinterpret_cast<void*>( intptr_t( shapeIndex ) );
        shapeIndex += 1;
        b2CreateCircleShape( bodyId, &shapeDef, &circle );

        y += 2.0f;
    }
}

void Step( Settings& settings ) override
{
    Sample::Step( settings );

    b2ContactEvents events = b2World_GetContactEvents( m_worldId );
    for ( int i = 0; i < events.hitCount; ++i )
    {
        b2ContactHitEvent* event = events.hitEvents + i;

        void* userDataA = b2Shape_GetUserData( event->shapeIdA );
        void* userDataB = b2Shape_GetUserData( event->shapeIdB );
        int indexA = static_cast<int>( reinterpret_cast<intptr_t>( userDataA ) );
        int indexB = static_cast<int>( reinterpret_cast<intptr_t>( userDataB ) );

        Draw.g_draw.DrawPoint( event->point, 10.0f, b2HexColor.b2_colorWhite );

        m_events.push_back( { indexA, indexB } );
    }

    int eventCount = (int)m_events.size();
    for ( int i = 0; i < eventCount; ++i )
    {
        Draw.g_draw.DrawString( 5, m_textLine, "%d, %d", m_events[i].indexA, m_events[i].indexB );
        m_textLine += m_textIncrement;
    }
}

static Sample* Create( Settings& settings )
{
    return new CircleStack( settings );
}

std::vector<Event> m_events;
};

static int sampleCircleStack = RegisterSample( "Stacking", "Circle Stack", CircleStack::Create );

