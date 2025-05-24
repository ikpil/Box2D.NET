// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2MotorJoints;

namespace Box2D.NET.Samples.Samples.Joints;

/// This test shows how to use a motor joint. A motor joint
/// can be used to animate a dynamic body. With finite motor forces
/// the body can be blocked by collision with other bodies.
/// By setting the correction factor to zero, the motor joint acts
/// like top-down dry friction.
public class MotorJoint : Sample
{
    private static readonly int SampleMotorJoint = SampleFactory.Shared.RegisterSample("Joints", "Motor Joint", Create);

    private B2BodyId m_bodyId;
    private B2JointId m_jointId;
    private float m_time;
    private float m_maxForce;
    private float m_maxTorque;
    private float m_correctionFactor;
    private bool m_go;

    private B2Transform _transform;

    private static Sample Create(SampleContext context)
    {
        return new MotorJoint(context);
    }

    public MotorJoint(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 7.0f);
            m_camera.m_zoom = 25.0f * 0.4f;
        }

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        // Define motorized body
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 8.0f);
            m_bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(2.0f, 0.5f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            b2CreatePolygonShape(m_bodyId, ref shapeDef, ref box);

            m_maxForce = 500.0f;
            m_maxTorque = 500.0f;
            m_correctionFactor = 0.3f;

            B2MotorJointDef jointDef = b2DefaultMotorJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = m_bodyId;
            jointDef.maxForce = m_maxForce;
            jointDef.maxTorque = m_maxTorque;
            jointDef.correctionFactor = m_correctionFactor;

            m_jointId = b2CreateMotorJoint(m_worldId, ref jointDef);
        }

        m_go = true;
        m_time = 0.0f;
    }

    public override void UpdateGui()
    {
        base.UpdateGui();


        float height = 180.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Motor Joint", ImGuiWindowFlags.NoResize);

        if (ImGui.Checkbox("Go", ref m_go))
        {
        }

        if (ImGui.SliderFloat("Max Force", ref m_maxForce, 0.0f, 1000.0f, "%.0f"))
        {
            b2MotorJoint_SetMaxForce(m_jointId, m_maxForce);
        }

        if (ImGui.SliderFloat("Max Torque", ref m_maxTorque, 0.0f, 1000.0f, "%.0f"))
        {
            b2MotorJoint_SetMaxTorque(m_jointId, m_maxTorque);
        }

        if (ImGui.SliderFloat("Correction", ref m_correctionFactor, 0.0f, 1.0f, "%.1f"))
        {
            b2MotorJoint_SetCorrectionFactor(m_jointId, m_correctionFactor);
        }

        if (ImGui.Button("Apply Impulse"))
        {
            b2Body_ApplyLinearImpulseToCenter(m_bodyId, new B2Vec2(100.0f, 0.0f), true);
        }

        ImGui.End();
    }


    public override void Step()
    {
        if (m_go && m_context.settings.hertz > 0.0f)
        {
            m_time += 1.0f / m_context.settings.hertz;
        }

        B2Vec2 linearOffset;
        linearOffset.X = 6.0f * MathF.Sin(2.0f * m_time);
        linearOffset.Y = 8.0f + 4.0f * MathF.Sin(1.0f * m_time);

        float angularOffset = B2_PI * MathF.Sin(-0.5f * m_time);

        b2MotorJoint_SetLinearOffset(m_jointId, linearOffset);
        b2MotorJoint_SetAngularOffset(m_jointId, angularOffset);

        _transform = new B2Transform(linearOffset, b2MakeRot(angularOffset));

        base.Step();
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        B2Vec2 force = b2Joint_GetConstraintForce(m_jointId);
        float torque = b2Joint_GetConstraintTorque(m_jointId);

        DrawTextLine($"force = {force.X:3,F0}, {force.Y:3,F0}, torque = {torque:3,F0}");
        m_draw.DrawTransform(_transform);
    }
}