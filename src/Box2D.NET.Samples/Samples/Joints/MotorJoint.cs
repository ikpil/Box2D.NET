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
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples.Samples.Joints;

/// This test shows how to use a motor joint. A motor joint
/// can be used to animate a dynamic body. With finite motor forces
/// the body can be blocked by collision with other bodies.
public class MotorJoint : Sample
{
    private static readonly int SampleMotorJoint = SampleFactory.Shared.RegisterSample("Joints", "Motor Joint", Create);

    private B2BodyId m_targetId;
    private B2BodyId m_bodyId;
    private B2JointId m_jointId;
    private B2Transform m_transform;
    private float m_time;
    private float m_speed;
    private float m_maxForce;
    private float m_maxTorque;

    private B2Transform _transform;

    private static Sample Create(SampleContext context)
    {
        return new MotorJoint(context);
    }

    public MotorJoint(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 7.0f);
            m_camera.zoom = 25.0f * 0.4f;
        }

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        m_transform = new B2Transform(new B2Vec2(0.0f, 8.0f), b2Rot_identity);

        // Define a target body
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_kinematicBody;
            bodyDef.position = m_transform.p;
            m_targetId = b2CreateBody(m_worldId, bodyDef);
        }

        // Define motorized body
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = m_transform.p;
            m_bodyId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeBox(2.0f, 0.5f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyId, ref shapeDef, ref box);

            m_maxForce = 5000.0f;
            m_maxTorque = 500.0f;

            B2MotorJointDef jointDef = b2DefaultMotorJointDef();
            jointDef.@base.bodyIdA = m_targetId;
            jointDef.@base.bodyIdB = m_bodyId;
            jointDef.linearHertz = 4.0f;
            jointDef.linearDampingRatio = 0.7f;
            jointDef.angularHertz = 4.0f;
            jointDef.angularDampingRatio = 0.7f;
            jointDef.maxSpringForce = m_maxForce;
            jointDef.maxSpringTorque = m_maxTorque;

            m_jointId = b2CreateMotorJoint(m_worldId, ref jointDef);
        }

        // Define spring body
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-2.0f, 2.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeSquare(0.5f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            B2MotorJointDef jointDef = b2DefaultMotorJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = bodyId;
            jointDef.@base.localFrameA.p = b2Add(bodyDef.position, new B2Vec2(0.25f, 0.25f));
            jointDef.@base.localFrameB.p = new B2Vec2(0.25f, 0.25f);
            jointDef.linearHertz = 7.5f;
            jointDef.linearDampingRatio = 0.7f;
            jointDef.angularHertz = 7.5f;
            jointDef.angularDampingRatio = 0.7f;
            jointDef.maxSpringForce = 500.0f;
            jointDef.maxSpringTorque = 10.0f;

            b2CreateMotorJoint(m_worldId, ref jointDef);
        }

        m_speed = 1.0f;
        m_time = 0.0f;
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 180.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Motor Joint", ImGuiWindowFlags.NoResize);

        if (ImGui.SliderFloat("Speed", ref m_speed, -5.0f, 5.0f, "%.0f"))
        {
        }

        if (ImGui.SliderFloat("Max Force", ref m_maxForce, 0.0f, 10000.0f, "%.0f"))
        {
            b2MotorJoint_SetMaxSpringForce(m_jointId, m_maxForce);
        }

        if (ImGui.SliderFloat("Max Torque", ref m_maxTorque, 0.0f, 10000.0f, "%.0f"))
        {
            b2MotorJoint_SetMaxVelocityTorque(m_jointId, m_maxTorque);
        }

        if (ImGui.Button("Apply Impulse"))
        {
            b2Body_ApplyLinearImpulseToCenter(m_bodyId, new B2Vec2(100.0f, 0.0f), true);
        }

        ImGui.End();
    }


    public override void Step()
    {
        float timeStep = m_context.hertz > 0.0f ? 1.0f / m_context.hertz : 0.0f;

        if (m_context.pause)
        {
            if (m_context.singleStep == false)
            {
                timeStep = 0.0f;
            }
        }

        if (timeStep > 0.0f)
        {
            m_time += m_speed * timeStep;

            B2Vec2 linearOffset;
            linearOffset.X = 6.0f * MathF.Sin(2.0f * m_time);
            linearOffset.Y = 8.0f + 4.0f * MathF.Sin(1.0f * m_time);

            float angularOffset = 2.0f * m_time;
            m_transform = new B2Transform(linearOffset, b2MakeRot(angularOffset));

            bool wake = true;
            b2Body_SetTargetTransform(m_targetId, m_transform, timeStep, wake);
        }

        DrawTransform(m_draw, m_transform, 1.0f);

        base.Step();
    }

    public override void Draw()
    {
        base.Draw();

        B2Vec2 force = b2Joint_GetConstraintForce(m_jointId);
        float torque = b2Joint_GetConstraintTorque(m_jointId);

        DrawTextLine($"force = {force.X:3,F0}, {force.Y:3,F0}, torque = {torque:3,F0}");
        DrawTransform(m_draw, _transform, 1.0f);
    }
}