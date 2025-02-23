using System.Diagnostics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Joints;

// This sample shows how to break joints when the internal reaction force becomes large.
class BreakableJoint : Sample
{
public:
enum
{
    e_count = 6
};

public BreakableJoint( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 8.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.7f;
    }

    b2BodyDef bodyDef = b2DefaultBodyDef();
    b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    b2Segment segment = { { -40.0f, 0.0f }, { 40.0f, 0.0f } };
    b2CreateSegmentShape( groundId, &shapeDef, &segment );

    for ( int i = 0; i < e_count; ++i )
    {
        m_jointIds[i] = b2_nullJointId;
    }

    b2Vec2 position = { -12.5f, 10.0f };
    bodyDef.type = b2BodyType.b2_dynamicBody;
    bodyDef.enableSleep = false;

    b2Polygon box = b2MakeBox( 1.0f, 1.0f );

    int index = 0;

    // distance joint
    {
        Debug.Assert( index < e_count );

        bodyDef.position = position;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        float length = 2.0f;
        b2Vec2 pivot1 = { position.x, position.y + 1.0f + length };
        b2Vec2 pivot2 = { position.x, position.y + 1.0f };
        b2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
        jointDef.bodyIdA = groundId;
        jointDef.bodyIdB = bodyId;
        jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot1 );
        jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot2 );
        jointDef.length = length;
        jointDef.collideConnected = true;
        m_jointIds[index] = b2CreateDistanceJoint( m_worldId, &jointDef );
    }

    position.x += 5.0f;
    ++index;

    // motor joint
    {
        Debug.Assert( index < e_count );

        bodyDef.position = position;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        b2MotorJointDef jointDef = b2DefaultMotorJointDef();
        jointDef.bodyIdA = groundId;
        jointDef.bodyIdB = bodyId;
        jointDef.linearOffset = position;
        jointDef.maxForce = 1000.0f;
        jointDef.maxTorque = 20.0f;
        jointDef.collideConnected = true;
        m_jointIds[index] = b2CreateMotorJoint( m_worldId, &jointDef );
    }

    position.x += 5.0f;
    ++index;

    // prismatic joint
    {
        Debug.Assert( index < e_count );

        bodyDef.position = position;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        b2Vec2 pivot = { position.x - 1.0f, position.y };
        b2PrismaticJointDef jointDef = b2DefaultPrismaticJointDef();
        jointDef.bodyIdA = groundId;
        jointDef.bodyIdB = bodyId;
        jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot );
        jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot );
        jointDef.localAxisA = b2Body_GetLocalVector( jointDef.bodyIdA, { 1.0f, 0.0f } );
        jointDef.collideConnected = true;
        m_jointIds[index] = b2CreatePrismaticJoint( m_worldId, &jointDef );
    }

    position.x += 5.0f;
    ++index;

    // revolute joint
    {
        Debug.Assert( index < e_count );

        bodyDef.position = position;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        b2Vec2 pivot = { position.x - 1.0f, position.y };
        b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
        jointDef.bodyIdA = groundId;
        jointDef.bodyIdB = bodyId;
        jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot );
        jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot );
        jointDef.collideConnected = true;
        m_jointIds[index] = b2CreateRevoluteJoint( m_worldId, &jointDef );
    }

    position.x += 5.0f;
    ++index;

    // weld joint
    {
        Debug.Assert( index < e_count );

        bodyDef.position = position;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        b2Vec2 pivot = { position.x - 1.0f, position.y };
        b2WeldJointDef jointDef = b2DefaultWeldJointDef();
        jointDef.bodyIdA = groundId;
        jointDef.bodyIdB = bodyId;
        jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot );
        jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot );
        jointDef.angularHertz = 2.0f;
        jointDef.angularDampingRatio = 0.5f;
        jointDef.linearHertz = 2.0f;
        jointDef.linearDampingRatio = 0.5f;
        jointDef.collideConnected = true;
        m_jointIds[index] = b2CreateWeldJoint( m_worldId, &jointDef );
    }

    position.x += 5.0f;
    ++index;

    // wheel joint
    {
        Debug.Assert( index < e_count );

        bodyDef.position = position;
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        b2Vec2 pivot = { position.x - 1.0f, position.y };
        b2WheelJointDef jointDef = b2DefaultWheelJointDef();
        jointDef.bodyIdA = groundId;
        jointDef.bodyIdB = bodyId;
        jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivot );
        jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivot );
        jointDef.localAxisA = b2Body_GetLocalVector( jointDef.bodyIdA, { 1.0f, 0.0f } );
        jointDef.hertz = 1.0f;
        jointDef.dampingRatio = 0.7f;
        jointDef.lowerTranslation = -1.0f;
        jointDef.upperTranslation = 1.0f;
        jointDef.enableLimit = true;
        jointDef.enableMotor = true;
        jointDef.maxMotorTorque = 10.0f;
        jointDef.motorSpeed = 1.0f;
        jointDef.collideConnected = true;
        m_jointIds[index] = b2CreateWheelJoint( m_worldId, &jointDef );
    }

    position.x += 5.0f;
    ++index;

    m_breakForce = 1000.0f;
}

public override void UpdateUI()
{
    float height = 100.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 240.0f, height ) );

    ImGui.Begin( "Breakable Joint", nullptr, ImGuiWindowFlags.NoResize );

    ImGui.SliderFloat( "break force", &m_breakForce, 0.0f, 10000.0f, "%.1f" );

    b2Vec2 gravity = b2World_GetGravity( m_worldId );
    if ( ImGui.SliderFloat( "gravity", &gravity.y, -50.0f, 50.0f, "%.1f" ) )
    {
        b2World_SetGravity( m_worldId, gravity );
    }

    ImGui.End();
}

public override void Step(Settings settings)
{
    for ( int i = 0; i < e_count; ++i )
    {
        if ( B2_IS_NULL( m_jointIds[i] ) )
        {
            continue;
        }

        b2Vec2 force = b2Joint_GetConstraintForce( m_jointIds[i] );
        if ( b2LengthSquared( force ) > m_breakForce * m_breakForce )
        {
            b2DestroyJoint( m_jointIds[i] );
            m_jointIds[i] = b2_nullJointId;
        }
        else
        {
            b2Vec2 point = b2Joint_GetLocalAnchorA( m_jointIds[i] );
            Draw.g_draw.DrawString( point, "(%.1f, %.1f)", force.x, force.y );
        }
    }

    base.Step( settings );
}

static Sample Create( Settings settings )
{
    return new BreakableJoint( settings );
}

b2JointId m_jointIds[e_count];
float m_breakForce;
};

static int sampleBreakableJoint = RegisterSample( "Joints", "Breakable", BreakableJoint::Create );

