namespace Box2D.NET.Samples.Samples.Joints;

class WheelJoint : Sample
{
public:
explicit WheelJoint( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 10.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.15f;
    }

    b2BodyId groundId;

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        groundId = b2CreateBody( m_worldId, &bodyDef );
    }

    m_enableSpring = true;
    m_enableLimit = true;
    m_enableMotor = true;
    m_motorSpeed = 2.0f;
    m_motorTorque = 5.0f;
    m_hertz = 1.0f;
    m_dampingRatio = 0.7f;

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = { 0.0f, 10.25f };
        bodyDef.type = b2BodyType.b2_dynamicBody;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Capsule capsule = { { 0.0f, -0.5f }, { 0.0f, 0.5f }, 0.5f };
        b2CreateCapsuleShape( bodyId, &shapeDef, &capsule );

        b2Vec2 pivot = { 0.0f, 10.0f };
        b2Vec2 axis = b2Normalize( { 1.0f, 1.0f } );
        b2WheelJointDef jointDef = b2DefaultWheelJointDef();
        jointDef.bodyIdA = groundId;
        jointDef.bodyIdB = bodyId;
        jointDef.localAxisA = b2Body_GetLocalVector( jointDef.bodyIdA, axis );
        jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot );
        jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot );
        jointDef.motorSpeed = m_motorSpeed;
        jointDef.maxMotorTorque = m_motorTorque;
        jointDef.enableMotor = m_enableMotor;
        jointDef.lowerTranslation = -3.0f;
        jointDef.upperTranslation = 3.0f;
        jointDef.enableLimit = m_enableLimit;
        jointDef.hertz = m_hertz;
        jointDef.dampingRatio = m_dampingRatio;

        m_jointId = b2CreateWheelJoint( m_worldId, &jointDef );
    }
}

void UpdateUI() override
{
    float height = 220.0f;
    ImGui.SetNextWindowPos( ImVec2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( ImVec2( 240.0f, height ) );

    ImGui.Begin( "Wheel Joint", nullptr, ImGuiWindowFlags.NoResize );

    if ( ImGui.Checkbox( "Limit", &m_enableLimit ) )
    {
        b2WheelJoint_EnableLimit( m_jointId, m_enableLimit );
    }

    if ( ImGui.Checkbox( "Motor", &m_enableMotor ) )
    {
        b2WheelJoint_EnableMotor( m_jointId, m_enableMotor );
    }

    if ( m_enableMotor )
    {
        if ( ImGui.SliderFloat( "Torque", &m_motorTorque, 0.0f, 20.0f, "%.0f" ) )
        {
            b2WheelJoint_SetMaxMotorTorque( m_jointId, m_motorTorque );
        }

        if ( ImGui.SliderFloat( "Speed", &m_motorSpeed, -20.0f, 20.0f, "%.0f" ) )
        {
            b2WheelJoint_SetMotorSpeed( m_jointId, m_motorSpeed );
        }
    }

    if ( ImGui.Checkbox( "Spring", &m_enableSpring ) )
    {
        b2WheelJoint_EnableSpring( m_jointId, m_enableSpring );
    }

    if ( m_enableSpring )
    {
        if ( ImGui.SliderFloat( "Hertz", &m_hertz, 0.0f, 10.0f, "%.1f" ) )
        {
            b2WheelJoint_SetSpringHertz( m_jointId, m_hertz );
        }

        if ( ImGui.SliderFloat( "Damping", &m_dampingRatio, 0.0f, 2.0f, "%.1f" ) )
        {
            b2WheelJoint_SetSpringDampingRatio( m_jointId, m_dampingRatio );
        }
    }

    ImGui.End();
}

void Step( Settings& settings ) override
{
    Sample::Step( settings );

    float torque = b2WheelJoint_GetMotorTorque( m_jointId );
    Draw.g_draw.DrawString( 5, m_textLine, "Motor Torque = %4.1f", torque );
    m_textLine += m_textIncrement;
}

static Sample* Create( Settings& settings )
{
    return new WheelJoint( settings );
}

b2JointId m_jointId;
float m_hertz;
float m_dampingRatio;
float m_motorSpeed;
float m_motorTorque;
bool m_enableSpring;
bool m_enableMotor;
bool m_enableLimit;
};

static int sampleWheel = RegisterSample( "Joints", "Wheel", WheelJoint::Create );

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
    ImGui.SetNextWindowPos( ImVec2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( ImVec2( 240.0f, height ) );

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

