using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Joints;

// Test the distance joint and all options
public class DistanceJoint : Sample
{
enum
{
    e_maxCount = 10
};

explicit DistanceJoint( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 12.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.35f;
    }

    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        m_groundId = b2CreateBody( m_worldId, &bodyDef );
    }

    m_count = 0;
    m_hertz = 2.0f;
    m_dampingRatio = 0.5f;
    m_length = 1.0f;
    m_minLength = m_length;
    m_maxLength = m_length;
    m_enableSpring = false;
    m_enableLimit = false;

    for ( int i = 0; i < e_maxCount; ++i )
    {
        m_bodyIds[i] = b2_nullBodyId;
        m_jointIds[i] = b2_nullJointId;
    }

    CreateScene( 1 );
}

void CreateScene( int newCount )
{
    // Must destroy joints before bodies
    for ( int i = 0; i < m_count; ++i )
    {
        b2DestroyJoint( m_jointIds[i] );
        m_jointIds[i] = b2_nullJointId;
    }

    for ( int i = 0; i < m_count; ++i )
    {
        b2DestroyBody( m_bodyIds[i] );
        m_bodyIds[i] = b2_nullBodyId;
    }

    m_count = newCount;

    float radius = 0.25f;
    b2Circle circle = { { 0.0f, 0.0f }, radius };

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    shapeDef.density = 20.0f;

    float yOffset = 20.0f;

    b2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
    jointDef.hertz = m_hertz;
    jointDef.dampingRatio = m_dampingRatio;
    jointDef.length = m_length;
    jointDef.minLength = m_minLength;
    jointDef.maxLength = m_maxLength;
    jointDef.enableSpring = m_enableSpring;
    jointDef.enableLimit = m_enableLimit;

    b2BodyId prevBodyId = m_groundId;
    for ( int i = 0; i < m_count; ++i )
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.angularDamping = 0.1f;
        bodyDef.position = { m_length * ( i + 1.0f ), yOffset };
        m_bodyIds[i] = b2CreateBody( m_worldId, &bodyDef );
        b2CreateCircleShape( m_bodyIds[i], &shapeDef, &circle );

        b2Vec2 pivotA = { m_length * i, yOffset };
        b2Vec2 pivotB = { m_length * ( i + 1.0f ), yOffset };
        jointDef.bodyIdA = prevBodyId;
        jointDef.bodyIdB = m_bodyIds[i];
        jointDef.localAnchorA = b2Body_GetLocalPoint( jointDef.bodyIdA, pivotA );
        jointDef.localAnchorB = b2Body_GetLocalPoint( jointDef.bodyIdB, pivotB );
        m_jointIds[i] = b2CreateDistanceJoint( m_worldId, &jointDef );

        prevBodyId = m_bodyIds[i];
    }
}

void UpdateUI() override
{
    float height = 240.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 180.0f, height ) );

    ImGui.Begin( "Distance Joint", nullptr, ImGuiWindowFlags.NoResize );
    ImGui.PushItemWidth( 100.0f );

    if ( ImGui.SliderFloat( "Length", &m_length, 0.1f, 4.0f, "%3.1f" ) )
    {
        for ( int i = 0; i < m_count; ++i )
        {
            b2DistanceJoint_SetLength( m_jointIds[i], m_length );
            b2Joint_WakeBodies( m_jointIds[i] );
        }
    }

    if ( ImGui.Checkbox( "Spring", &m_enableSpring ) )
    {
        for ( int i = 0; i < m_count; ++i )
        {
            b2DistanceJoint_EnableSpring( m_jointIds[i], m_enableSpring );
            b2Joint_WakeBodies( m_jointIds[i] );
        }
    }

    if ( m_enableSpring )
    {
        if ( ImGui.SliderFloat( "Hertz", &m_hertz, 0.0f, 15.0f, "%3.1f" ) )
        {
            for ( int i = 0; i < m_count; ++i )
            {
                b2DistanceJoint_SetSpringHertz( m_jointIds[i], m_hertz );
                b2Joint_WakeBodies( m_jointIds[i] );
            }
        }

        if ( ImGui.SliderFloat( "Damping", &m_dampingRatio, 0.0f, 4.0f, "%3.1f" ) )
        {
            for ( int i = 0; i < m_count; ++i )
            {
                b2DistanceJoint_SetSpringDampingRatio( m_jointIds[i], m_dampingRatio );
                b2Joint_WakeBodies( m_jointIds[i] );
            }
        }
    }

    if ( ImGui.Checkbox( "Limit", &m_enableLimit ) )
    {
        for ( int i = 0; i < m_count; ++i )
        {
            b2DistanceJoint_EnableLimit( m_jointIds[i], m_enableLimit );
            b2Joint_WakeBodies( m_jointIds[i] );
        }
    }

    if ( m_enableLimit )
    {
        if ( ImGui.SliderFloat( "Min Length", &m_minLength, 0.1f, 4.0f, "%3.1f" ) )
        {
            for ( int i = 0; i < m_count; ++i )
            {
                b2DistanceJoint_SetLengthRange( m_jointIds[i], m_minLength, m_maxLength );
                b2Joint_WakeBodies( m_jointIds[i] );
            }
        }

        if ( ImGui.SliderFloat( "Max Length", &m_maxLength, 0.1f, 4.0f, "%3.1f" ) )
        {
            for ( int i = 0; i < m_count; ++i )
            {
                b2DistanceJoint_SetLengthRange( m_jointIds[i], m_minLength, m_maxLength );
                b2Joint_WakeBodies( m_jointIds[i] );
            }
        }
    }

    int count = m_count;
    if ( ImGui.SliderInt( "Count", &count, 1, e_maxCount ) )
    {
        CreateScene( count );
    }

    ImGui.PopItemWidth();
    ImGui.End();
}

static Sample* Create( Settings& settings )
{
    return new DistanceJoint( settings );
}

b2BodyId m_groundId;
b2BodyId m_bodyIds[e_maxCount];
b2JointId m_jointIds[e_maxCount];
int m_count;
float m_hertz;
float m_dampingRatio;
float m_length;
float m_minLength;
float m_maxLength;
bool m_enableSpring;
bool m_enableLimit;
};

static int sampleDistanceJoint = RegisterSample( "Joints", "Distance Joint", DistanceJoint::Create );
