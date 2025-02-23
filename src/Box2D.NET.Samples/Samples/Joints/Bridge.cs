using System.Diagnostics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.revolute_joint;

namespace Box2D.NET.Samples.Samples.Joints;


// A suspension bridge
class Bridge : Sample
{
public:
enum
{
    e_count = 160
};

explicit Bridge( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_zoom = 25.0f * 2.5f;
    }

    b2BodyId groundId = b2_nullBodyId;
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        groundId = b2CreateBody( m_worldId, &bodyDef );
    }

    {
        b2Polygon box = b2MakeBox( 0.5f, 0.125f );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 20.0f;

        b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
        int jointIndex = 0;
        m_frictionTorque = 200.0f;
        m_gravityScale = 1.0f;

        float xbase = -80.0f;

        b2BodyId prevBodyId = groundId;
        for ( int i = 0; i < e_count; ++i )
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = { xbase + 0.5f + 1.0f * i, 20.0f };
            bodyDef.linearDamping = 0.1f;
            bodyDef.angularDamping = 0.1f;
            m_bodyIds[i] = b2CreateBody( m_worldId, &bodyDef );
            b2CreatePolygonShape( m_bodyIds[i], &shapeDef, &box );

            b2Vec2 pivot = { xbase + 1.0f * i, 20.0f };
            jointDef.bodyIdA = prevBodyId;
            jointDef.bodyIdB = m_bodyIds[i];
            jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot );
            jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot );
            jointDef.enableMotor = true;
            jointDef.maxMotorTorque = m_frictionTorque;
            m_jointIds[jointIndex++] = b2CreateRevoluteJoint( m_worldId, &jointDef );

            prevBodyId = m_bodyIds[i];
        }

        b2Vec2 pivot = { xbase + 1.0f * e_count, 20.0f };
        jointDef.bodyIdA = prevBodyId;
        jointDef.bodyIdB = groundId;
        jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot );
        jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot );
        jointDef.enableMotor = true;
        jointDef.maxMotorTorque = m_frictionTorque;
        m_jointIds[jointIndex++] = b2CreateRevoluteJoint( m_worldId, &jointDef );

        Debug.Assert( jointIndex == e_count + 1 );
    }

    for ( int i = 0; i < 2; ++i )
    {
        b2Vec2 vertices[3] = { { -0.5f, 0.0f }, { 0.5f, 0.0f }, { 0.0f, 1.5f } };

        b2Hull hull = b2ComputeHull( vertices, 3 );
        b2Polygon triangle = b2MakePolygon( &hull, 0.0f );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 20.0f;

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { -8.0f + 8.0f * i, 22.0f };
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &triangle );
    }

    for ( int i = 0; i < 3; ++i )
    {
        b2Circle circle = { { 0.0f, 0.0f }, 0.5f };

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 20.0f;

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { -6.0f + 6.0f * i, 25.0f };
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreateCircleShape( bodyId, &shapeDef, &circle );
    }
}

void UpdateUI() override
{
    float height = 80.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 240.0f, height ) );

    ImGui.Begin( "Bridge", nullptr, ImGuiWindowFlags.NoResize );

    // Slider takes half the window
    ImGui.PushItemWidth( ImGui.GetWindowWidth() * 0.5f );
    bool updateFriction = ImGui.SliderFloat( "Joint Friction", &m_frictionTorque, 0.0f, 1000.0f, "%2.f" );
    if ( updateFriction )
    {
        for ( int i = 0; i <= e_count; ++i )
        {
            b2RevoluteJoint_SetMaxMotorTorque( m_jointIds[i], m_frictionTorque );
        }
    }

    if ( ImGui.SliderFloat( "Gravity scale", &m_gravityScale, -1.0f, 1.0f, "%.1f" ) )
    {
        for ( int i = 0; i < e_count; ++i )
        {
            b2Body_SetGravityScale( m_bodyIds[i], m_gravityScale );
        }
    }

    ImGui.End();
}

static Sample* Create( Settings& settings )
{
    return new Bridge( settings );
}

b2BodyId m_bodyIds[e_count];
b2JointId m_jointIds[e_count + 1];
float m_frictionTorque;
float m_gravityScale;
};

static int sampleBridgeIndex = RegisterSample( "Joints", "Bridge", Bridge::Create );

