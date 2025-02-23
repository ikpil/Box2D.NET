using System;
using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.motor_joint;

namespace Box2D.NET.Samples.Samples.Joints;

/// This test shows how to use a motor joint. A motor joint
/// can be used to animate a dynamic body. With finite motor forces
/// the body can be blocked by collision with other bodies.
/// By setting the correction factor to zero, the motor joint acts
/// like top-down dry friction.
public class MotorJoint : Sample
{
    b2JointId m_jointId;
    float m_time;
    float m_maxForce;
    float m_maxTorque;
    float m_correctionFactor;
    bool m_go;

    static int sampleMotorJoint = RegisterSample( "Joints", "Motor Joint", Create );
    static Sample Create( Settings settings )
    {
        return new MotorJoint( settings );
    }

public MotorJoint( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 7.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.4f;
    }

    b2BodyId groundId;
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        groundId = b2CreateBody( m_worldId, &bodyDef );
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Segment segment = { { -20.0f, 0.0f }, { 20.0f, 0.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    // Define motorized body
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { 0.0f, 8.0f };
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2Polygon box = b2MakeBox( 2.0f, 0.5f );
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        m_maxForce = 500.0f;
        m_maxTorque = 500.0f;
        m_correctionFactor = 0.3f;

        b2MotorJointDef jointDef = b2DefaultMotorJointDef();
        jointDef.bodyIdA = groundId;
        jointDef.bodyIdB = bodyId;
        jointDef.maxForce = m_maxForce;
        jointDef.maxTorque = m_maxTorque;
        jointDef.correctionFactor = m_correctionFactor;

        m_jointId = b2CreateMotorJoint( m_worldId, &jointDef );
    }

    m_go = true;
    m_time = 0.0f;
}

public override void UpdateUI()
{
    float height = 140.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 240.0f, height ) );

    ImGui.Begin( "Motor Joint", nullptr, ImGuiWindowFlags.NoResize );

    if ( ImGui.Checkbox( "Go", &m_go ) )
    {
    }

    if ( ImGui.SliderFloat( "Max Force", &m_maxForce, 0.0f, 1000.0f, "%.0f" ) )
    {
        b2MotorJoint_SetMaxForce( m_jointId, m_maxForce );
    }

    if ( ImGui.SliderFloat( "Max Torque", &m_maxTorque, 0.0f, 1000.0f, "%.0f" ) )
    {
        b2MotorJoint_SetMaxTorque( m_jointId, m_maxTorque );
    }

    if ( ImGui.SliderFloat( "Correction", &m_correctionFactor, 0.0f, 1.0f, "%.1f" ) )
    {
        b2MotorJoint_SetCorrectionFactor( m_jointId, m_correctionFactor );
    }

    ImGui.End();
}

public override void Step(Settings settings)
{
    if ( m_go && settings.hertz > 0.0f )
    {
        m_time += 1.0f / settings.hertz;
    }

    b2Vec2 linearOffset;
    linearOffset.x = 6.0f * MathF.Sin( 2.0f * m_time );
    linearOffset.y = 8.0f + 4.0f * MathF.Sin( 1.0f * m_time );

    float angularOffset = B2_PI * MathF.Sin( -0.5f * m_time );

    b2MotorJoint_SetLinearOffset( m_jointId, linearOffset );
    b2MotorJoint_SetAngularOffset( m_jointId, angularOffset );

    b2Transform transform = { linearOffset, b2MakeRot( angularOffset ) };
    Draw.g_draw.DrawTransform( transform );

    base.Step( settings );

    b2Vec2 force = b2Joint_GetConstraintForce( m_jointId );
    float torque = b2Joint_GetConstraintTorque( m_jointId );

    Draw.g_draw.DrawString( 5, m_textLine, "force = {%3.f, %3.f}, torque = %3.f", force.x, force.y, torque );
    m_textLine += 15;
}



}


