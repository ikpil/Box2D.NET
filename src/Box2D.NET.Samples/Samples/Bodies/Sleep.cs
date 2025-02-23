using System.Numerics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.wheel_joint;
using static Box2D.NET.world;
using static Box2D.NET.mouse_joint;

namespace Box2D.NET.Samples.Primitives;

class Sleep : Sample
{
public:
explicit Sleep( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 3.0f, 50.0f };
        Draw.g_camera.m_zoom = 25.0f * 2.2f;
    }

    b2BodyId groundId = b2_nullBodyId;
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        groundId = b2CreateBody( m_worldId, &bodyDef );

        b2Segment segment = { { -20.0f, 0.0f }, { 20.0f, 0.0f } };
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        m_groundShapeId = b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    // Sleeping body with sensors
    for ( int i = 0; i < 2; ++i )
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { -4.0f, 3.0f + 2.0f * i };
        bodyDef.isAwake = false;
        bodyDef.enableSleep = true;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2Capsule capsule = { { 0.0f, 1.0f }, { 1.0f, 1.0f }, 0.75f };
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreateCapsuleShape( bodyId, &shapeDef, &capsule );

        shapeDef.isSensor = true;
        capsule.radius = 1.0f;
        m_sensorIds[i] = b2CreateCapsuleShape( bodyId, &shapeDef, &capsule );
        m_sensorTouching[i] = false;
    }

    // Sleeping body but sleep is disabled
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { 0.0f, 3.0f };
        bodyDef.isAwake = false;
        bodyDef.enableSleep = false;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2Circle circle = { { 1.0f, 1.0f }, 1.0f };
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreateCircleShape( bodyId, &shapeDef, &circle );
    }

    // Awake body and sleep is disabled
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { 5.0f, 3.0f };
        bodyDef.isAwake = true;
        bodyDef.enableSleep = false;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2Polygon box = b2MakeOffsetBox( 1.0f, 1.0f, { 0.0f, 1.0f }, b2MakeRot( 0.25f * B2_PI ) );
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape( bodyId, &shapeDef, &box );
    }

    // A sleeping body to test waking on collision
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { 5.0f, 1.0f };
        bodyDef.isAwake = false;
        bodyDef.enableSleep = true;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2Polygon box = b2MakeSquare( 1.0f );
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape( bodyId, &shapeDef, &box );
    }

    // A long pendulum
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { 0.0f, 100.0f };
        bodyDef.angularDamping = 0.5f;
        bodyDef.sleepThreshold = 0.05f;
        m_pendulumId = b2CreateBody( m_worldId, &bodyDef );

        b2Capsule capsule = { { 0.0f, 0.0f }, { 90.0f, 0.0f }, 0.25f };
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreateCapsuleShape( m_pendulumId, &shapeDef, &capsule );

        b2Vec2 pivot = bodyDef.position;
        b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
        jointDef.bodyIdA = groundId;
        jointDef.bodyIdB = m_pendulumId;
        jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot );
        jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot );
        b2CreateRevoluteJoint( m_worldId, &jointDef );
    }
}

void UpdateUI() override
{
    float height = 100.0f;
    ImGui.SetNextWindowPos( ImVec2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( ImVec2( 240.0f, height ) );
    ImGui.Begin( "Sleep", nullptr, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize );

    ImGui.PushItemWidth( 120.0f );

    ImGui.Text( "Pendulum Tuning" );

    float sleepVelocity = b2Body_GetSleepThreshold( m_pendulumId );
    if ( ImGui.SliderFloat( "sleep velocity", &sleepVelocity, 0.0f, 1.0f, "%.2f" ) )
    {
        b2Body_SetSleepThreshold( m_pendulumId, sleepVelocity );
        b2Body_SetAwake( m_pendulumId, true );
    }

    float angularDamping = b2Body_GetAngularDamping( m_pendulumId );
    if ( ImGui.SliderFloat( "angular damping", &angularDamping, 0.0f, 2.0f, "%.2f" ) )
    {
        b2Body_SetAngularDamping( m_pendulumId, angularDamping );
    }

    ImGui.PopItemWidth();

    ImGui.End();
}

void Step( Settings& settings ) override
{
    Sample::Step( settings );

    // Detect sensors touching the ground
    b2SensorEvents sensorEvents = b2World_GetSensorEvents( m_worldId );

    for ( int i = 0; i < sensorEvents.beginCount; ++i )
    {
        b2SensorBeginTouchEvent* event = sensorEvents.beginEvents + i;
        if ( B2_ID_EQUALS( event->visitorShapeId, m_groundShapeId ) )
        {
            if ( B2_ID_EQUALS( event->sensorShapeId, m_sensorIds[0] ) )
            {
                m_sensorTouching[0] = true;
            }
            else if ( B2_ID_EQUALS( event->sensorShapeId, m_sensorIds[1] ) )
            {
                m_sensorTouching[1] = true;
            }
        }
    }

    for ( int i = 0; i < sensorEvents.endCount; ++i )
    {
        b2SensorEndTouchEvent* event = sensorEvents.endEvents + i;
        if ( B2_ID_EQUALS( event->visitorShapeId, m_groundShapeId ) )
        {
            if ( B2_ID_EQUALS( event->sensorShapeId, m_sensorIds[0] ) )
            {
                m_sensorTouching[0] = false;
            }
            else if ( B2_ID_EQUALS( event->sensorShapeId, m_sensorIds[1] ) )
            {
                m_sensorTouching[1] = false;
            }
        }
    }

    for ( int i = 0; i < 2; ++i )
    {
        Draw.g_draw.DrawString( 5, m_textLine, "sensor touch %d = %s", i, m_sensorTouching[i] ? "true" : "false" );
        m_textLine += m_textIncrement;
    }
}

static Sample* Create( Settings& settings )
{
    return new Sleep( settings );
}

b2BodyId m_pendulumId;
b2ShapeId m_groundShapeId;
b2ShapeId m_sensorIds[2];
bool m_sensorTouching[2];
};

static int sampleSleep = RegisterSample( "Bodies", "Sleep", Sleep::Create );
