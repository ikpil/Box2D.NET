using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Joints;

// This is a fun demo that shows off the wheel joint
public class Driving : Sample
{
    Car m_car;

    float m_throttle;
    float m_hertz;
    float m_dampingRatio;
    float m_torque;
    float m_speed;

    static int sampleDriving = RegisterSample( "Joints", "Driving", Create );
    static Sample Create( Settings settings )
    {
        return new Driving( settings );
    }

public Driving( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center.y = 5.0f;
        Draw.g_camera.m_zoom = 25.0f * 0.4f;
        settings.drawJoints = false;
    }

    b2BodyId groundId;
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        groundId = b2CreateBody( m_worldId, &bodyDef );

        b2Vec2 points[25];
        int count = 24;

        // fill in reverse to match line list convention
        points[count--] = { -20.0f, -20.0f };
        points[count--] = { -20.0f, 0.0f };
        points[count--] = { 20.0f, 0.0f };

        float hs[10] = { 0.25f, 1.0f, 4.0f, 0.0f, 0.0f, -1.0f, -2.0f, -2.0f, -1.25f, 0.0f };
        float x = 20.0f, dx = 5.0f;

        for ( int j = 0; j < 2; ++j )
        {
            for ( int i = 0; i < 10; ++i )
            {
                float y2 = hs[i];
                points[count--] = { x + dx, y2 };
                x += dx;
            }
        }

        // flat before bridge
        points[count--] = { x + 40.0f, 0.0f };
        points[count--] = { x + 40.0f, -20.0f };

        Debug.Assert( count == -1 );

        b2ChainDef chainDef = b2DefaultChainDef();
        chainDef.points = points;
        chainDef.count = 25;
        chainDef.isLoop = true;
        b2CreateChain( groundId, &chainDef );

        // flat after bridge
        x += 80.0f;
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Segment segment = { { x, 0.0f }, { x + 40.0f, 0.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );

        // jump ramp
        x += 40.0f;
        segment = { { x, 0.0f }, { x + 10.0f, 5.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );

        // final corner
        x += 20.0f;
        segment = { { x, 0.0f }, { x + 40.0f, 0.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );

        x += 40.0f;
        segment = { { x, 0.0f }, { x, 20.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    // Teeter
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = { 140.0f, 1.0f };
        bodyDef.angularVelocity = 1.0f;
        bodyDef.type = b2BodyType.b2_dynamicBody;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Polygon box = b2MakeBox( 10.0f, 0.25f );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        b2Vec2 pivot = bodyDef.position;
        b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
        jointDef.bodyIdA = groundId;
        jointDef.bodyIdB = bodyId;
        jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot );
        jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot );
        jointDef.lowerAngle = -8.0f * B2_PI / 180.0f;
        jointDef.upperAngle = 8.0f * B2_PI / 180.0f;
        jointDef.enableLimit = true;
        b2CreateRevoluteJoint( m_worldId, &jointDef );
    }

    // Bridge
    {
        int N = 20;
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Capsule capsule = { { -1.0f, 0.0f }, { 1.0f, 0.0f }, 0.125f };

        b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();

        b2BodyId prevBodyId = groundId;
        for ( int i = 0; i < N; ++i )
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = { 161.0f + 2.0f * i, -0.125f };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2CreateCapsuleShape( bodyId, &shapeDef, &capsule );

            b2Vec2 pivot = { 160.0f + 2.0f * i, -0.125f };
            jointDef.bodyIdA = prevBodyId;
            jointDef.bodyIdB = bodyId;
            jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot );
            jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot );
            b2CreateRevoluteJoint( m_worldId, &jointDef );

            prevBodyId = bodyId;
        }

        b2Vec2 pivot = { 160.0f + 2.0f * N, -0.125f };
        jointDef.bodyIdA = prevBodyId;
        jointDef.bodyIdB = groundId;
        jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot );
        jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot );
        jointDef.enableMotor = true;
        jointDef.maxMotorTorque = 50.0f;
        b2CreateRevoluteJoint( m_worldId, &jointDef );
    }

    // Boxes
    {
        b2Polygon box = b2MakeBox( 0.5f, 0.5f );

        b2BodyId bodyId;
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.friction = 0.25f;
        shapeDef.restitution = 0.25f;
        shapeDef.density = 0.25f;

        bodyDef.position = { 230.0f, 0.5f };
        bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        bodyDef.position = { 230.0f, 1.5f };
        bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        bodyDef.position = { 230.0f, 2.5f };
        bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        bodyDef.position = { 230.0f, 3.5f };
        bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        bodyDef.position = { 230.0f, 4.5f };
        bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );
    }

    // Car

    m_throttle = 0.0f;
    m_speed = 35.0f;
    m_torque = 5.0f;
    m_hertz = 5.0f;
    m_dampingRatio = 0.7f;

    m_car.Spawn( m_worldId, { 0.0f, 0.0f }, 1.0f, m_hertz, m_dampingRatio, m_torque, nullptr );
}

public override void UpdateUI()
{
    float height = 140.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 200.0f, height ) );

    ImGui.Begin( "Driving", nullptr, ImGuiWindowFlags.NoResize );

    ImGui.PushItemWidth( 100.0f );
    if ( ImGui.SliderFloat( "Spring Hertz", &m_hertz, 0.0f, 20.0f, "%.0f" ) )
    {
        m_car.SetHertz( m_hertz );
    }

    if ( ImGui.SliderFloat( "Damping Ratio", &m_dampingRatio, 0.0f, 10.0f, "%.1f" ) )
    {
        m_car.SetDampingRadio( m_dampingRatio );
    }

    if ( ImGui.SliderFloat( "Speed", &m_speed, 0.0f, 50.0f, "%.0f" ) )
    {
        m_car.SetSpeed( m_throttle * m_speed );
    }

    if ( ImGui.SliderFloat( "Torque", &m_torque, 0.0f, 10.0f, "%.1f" ) )
    {
        m_car.SetTorque( m_torque );
    }
    ImGui.PopItemWidth();

    ImGui.End();
}

public override void Step(Settings settings)
{
    if ( glfwGetKey( g_mainWindow, GLFW_KEY_A ) == GLFW_PRESS )
    {
        m_throttle = 1.0f;
        m_car.SetSpeed( m_speed );
    }

    if ( glfwGetKey( g_mainWindow, GLFW_KEY_S ) == GLFW_PRESS )
    {
        m_throttle = 0.0f;
        m_car.SetSpeed( 0.0f );
    }

    if ( glfwGetKey( g_mainWindow, GLFW_KEY_D ) == GLFW_PRESS )
    {
        m_throttle = -1.0f;
        m_car.SetSpeed( -m_speed );
    }

    Draw.g_draw.DrawString( 5, m_textLine, "Keys: left = a, brake = s, right = d" );
    m_textLine += m_textIncrement;

    b2Vec2 linearVelocity = b2Body_GetLinearVelocity( m_car.m_chassisId );
    float kph = linearVelocity.x * 3.6f;
    Draw.g_draw.DrawString( 5, m_textLine, "speed in kph: %.2g", kph );
    m_textLine += m_textIncrement;

    b2Vec2 carPosition = b2Body_GetPosition( m_car.m_chassisId );
    Draw.g_camera.m_center.x = carPosition.x;

    base.Step( settings );
}



}

