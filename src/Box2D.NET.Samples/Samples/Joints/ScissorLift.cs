namespace Box2D.NET.Samples.Samples.Joints;

class ScissorLift : Sample
{
public:
explicit ScissorLift( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 9.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.4f;
    }

    // Need 8 sub-steps for smoother operation
    settings.subStepCount = 8;

    b2BodyId groundId;
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        groundId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Segment segment = { { -20.0f, 0.0f }, { 20.0f, 0.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;
    bodyDef.sleepThreshold = 0.01f;

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    b2Capsule capsule = { { -2.5f, 0.0f }, { 2.5f, 0.0f }, 0.15f };

    b2BodyId baseId1 = groundId;
    b2BodyId baseId2 = groundId;
    b2Vec2 baseAnchor1 = { -2.5f, 0.2f };
    b2Vec2 baseAnchor2 = { 2.5f, 0.2f };
    float y = 0.5f;

    b2BodyId linkId1;
    int N = 3;

    for ( int i = 0; i < N; ++i )
    {
        bodyDef.position = { 0.0f, y };
        bodyDef.rotation = b2MakeRot( 0.15f );
        b2BodyId bodyId1 = b2CreateBody( m_worldId, &bodyDef );
        b2CreateCapsuleShape( bodyId1, &shapeDef, &capsule );

        bodyDef.position = { 0.0f, y };
        bodyDef.rotation = b2MakeRot( -0.15f );

        b2BodyId bodyId2 = b2CreateBody( m_worldId, &bodyDef );
        b2CreateCapsuleShape( bodyId2, &shapeDef, &capsule );

        if ( i == 1 )
        {
            linkId1 = bodyId2;
        }

        b2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();

        // left pin
        revoluteDef.bodyIdA = baseId1;
        revoluteDef.bodyIdB = bodyId1;
        revoluteDef.localAnchorA = baseAnchor1;
        revoluteDef.localAnchorB = { -2.5f, 0.0f };
        revoluteDef.enableMotor = false;
        revoluteDef.maxMotorTorque = 1.0f;
        revoluteDef.collideConnected = ( i == 0 ) ? true : false;

        b2CreateRevoluteJoint( m_worldId, &revoluteDef );

        // right pin
        if ( i == 0 )
        {
            b2WheelJointDef wheelDef = b2DefaultWheelJointDef();
            wheelDef.bodyIdA = baseId2;
            wheelDef.bodyIdB = bodyId2;
            wheelDef.localAxisA = { 1.0f, 0.0f };
            wheelDef.localAnchorA = baseAnchor2;
            wheelDef.localAnchorB = { 2.5f, 0.0f };
            wheelDef.enableSpring = false;
            wheelDef.collideConnected = true;

            b2CreateWheelJoint( m_worldId, &wheelDef );
        }
        else
        {
            revoluteDef.bodyIdA = baseId2;
            revoluteDef.bodyIdB = bodyId2;
            revoluteDef.localAnchorA = baseAnchor2;
            revoluteDef.localAnchorB = { 2.5f, 0.0f };
            revoluteDef.enableMotor = false;
            revoluteDef.maxMotorTorque = 1.0f;
            revoluteDef.collideConnected = false;

            b2CreateRevoluteJoint( m_worldId, &revoluteDef );
        }

        // middle pin
        revoluteDef.bodyIdA = bodyId1;
        revoluteDef.bodyIdB = bodyId2;
        revoluteDef.localAnchorA = { 0.0f, 0.0f };
        revoluteDef.localAnchorB = { 0.0f, 0.0f };
        revoluteDef.enableMotor = false;
        revoluteDef.maxMotorTorque = 1.0f;
        revoluteDef.collideConnected = false;

        b2CreateRevoluteJoint( m_worldId, &revoluteDef );

        baseId1 = bodyId2;
        baseId2 = bodyId1;
        baseAnchor1 = { -2.5f, 0.0f };
        baseAnchor2 = { 2.5f, 0.0f };
        y += 1.0f;
    }

    bodyDef.position = { 0.0f, y };
    bodyDef.rotation = b2Rot_identity;
    b2BodyId platformId = b2CreateBody( m_worldId, &bodyDef );

    b2Polygon box = b2MakeBox( 3.0f, 0.2f );
    b2CreatePolygonShape( platformId, &shapeDef, &box );

    // left pin
    b2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
    revoluteDef.bodyIdA = platformId;
    revoluteDef.bodyIdB = baseId1;
    revoluteDef.localAnchorA = { -2.5f, -0.4f };
    revoluteDef.localAnchorB = baseAnchor1;
    revoluteDef.enableMotor = false;
    revoluteDef.maxMotorTorque = 1.0f;
    revoluteDef.collideConnected = true;
    b2CreateRevoluteJoint( m_worldId, &revoluteDef );

    // right pin
    b2WheelJointDef wheelDef = b2DefaultWheelJointDef();
    wheelDef.bodyIdA = platformId;
    wheelDef.bodyIdB = baseId2;
    wheelDef.localAxisA = { 1.0f, 0.0f };
    wheelDef.localAnchorA = { 2.5f, -0.4f };
    wheelDef.localAnchorB = baseAnchor2;
    wheelDef.enableSpring = false;
    wheelDef.collideConnected = true;
    b2CreateWheelJoint( m_worldId, &wheelDef );

    m_enableMotor = false;
    m_motorSpeed = 0.25f;
    m_motorForce = 2000.0f;

    b2DistanceJointDef distanceDef = b2DefaultDistanceJointDef();
    distanceDef.bodyIdA = groundId;
    distanceDef.bodyIdB = linkId1;
    distanceDef.localAnchorA = { -2.5f, 0.2f };
    distanceDef.localAnchorB = { 0.5f, 0.0f };
    distanceDef.enableSpring = true;
    distanceDef.minLength = 0.2f;
    distanceDef.maxLength = 5.5f;
    distanceDef.enableLimit = true;
    distanceDef.enableMotor = m_enableMotor;
    distanceDef.motorSpeed = m_motorSpeed;
    distanceDef.maxMotorForce = m_motorForce;
    m_liftJointId = b2CreateDistanceJoint( m_worldId, &distanceDef );

    Car car;
    car.Spawn( m_worldId, { 0.0f, y + 2.0f }, 1.0f, 3.0f, 0.7f, 0.0f, nullptr );
}

void UpdateUI() override
{
    float height = 140.0f;
    ImGui.SetNextWindowPos( ImVec2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( ImVec2( 240.0f, height ) );

    ImGui.Begin( "Scissor Lift", nullptr, ImGuiWindowFlags.NoResize );

    if ( ImGui.Checkbox( "Motor", &m_enableMotor ) )
    {
        b2DistanceJoint_EnableMotor( m_liftJointId, m_enableMotor );
        b2Joint_WakeBodies( m_liftJointId );
    }

    if ( ImGui.SliderFloat( "Max Force", &m_motorForce, 0.0f, 3000.0f, "%.0f" ) )
    {
        b2DistanceJoint_SetMaxMotorForce( m_liftJointId, m_motorForce );
        b2Joint_WakeBodies( m_liftJointId );
    }

    if ( ImGui.SliderFloat( "Speed", &m_motorSpeed, -0.3f, 0.3f, "%.2f" ) )
    {
        b2DistanceJoint_SetMotorSpeed( m_liftJointId, m_motorSpeed );
        b2Joint_WakeBodies( m_liftJointId );
    }

    ImGui.End();
}

void Step( Settings& settings ) override
{
    Sample::Step( settings );
}

static Sample* Create( Settings& settings )
{
    return new ScissorLift( settings );
}

b2JointId m_liftJointId;
float m_motorForce;
float m_motorSpeed;
bool m_enableMotor;
};

static int sampleScissorLift = RegisterSample( "Joints", "Scissor Lift", ScissorLift::Create );