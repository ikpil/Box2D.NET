// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2RevoluteJoints;

namespace Box2D.NET.Samples.Samples.Joints;

public class RevoluteJoint : Sample
{
    B2BodyId m_ball;
    B2JointId m_jointId1;
    B2JointId m_jointId2;
    float m_motorSpeed;
    float m_motorTorque;
    float m_hertz;
    float m_dampingRatio;
    bool m_enableSpring;
    bool m_enableMotor;
    bool m_enableLimit;
    private static readonly int SampleRevolute = SampleFactory.Shared.RegisterSample("Joints", "Revolute", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new RevoluteJoint(ctx, settings);
    }

    public RevoluteJoint(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.0f, 15.5f);
            B2.g_camera.m_zoom = 25.0f * 0.7f;
        }

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, -1.0f);
            groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(40.0f, 1.0f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        m_enableSpring = false;
        m_enableLimit = true;
        m_enableMotor = false;
        m_hertz = 1.0f;
        m_dampingRatio = 0.5f;
        m_motorSpeed = 1.0f;
        m_motorTorque = 1000.0f;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-10.0f, 20.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -1.0f), new B2Vec2(0.0f, 6.0f), 0.5f);
            b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);

            B2Vec2 pivot = new B2Vec2(-10.0f, 20.5f);
            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = bodyId;
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.enableSpring = m_enableSpring;
            jointDef.hertz = m_hertz;
            jointDef.dampingRatio = m_dampingRatio;
            jointDef.motorSpeed = m_motorSpeed;
            jointDef.maxMotorTorque = m_motorTorque;
            jointDef.enableMotor = m_enableMotor;
            jointDef.referenceAngle = 0.5f * B2_PI;
            jointDef.lowerAngle = -0.5f * B2_PI;
            jointDef.upperAngle = 0.75f * B2_PI;
            jointDef.enableLimit = m_enableLimit;

            m_jointId1 = b2CreateRevoluteJoint(m_worldId, ref jointDef);
        }

        {
            B2Circle circle = new B2Circle(new B2Vec2(), 0.0f);
            circle.radius = 2.0f;

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(5.0f, 30.0f);
            m_ball = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;

            b2CreateCircleShape(m_ball, ref shapeDef, ref circle);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(20.0f, 10.0f);
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2BodyId body = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeOffsetBox(10.0f, 0.5f, new B2Vec2(-10.0f, 0.0f), b2Rot_identity);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            b2CreatePolygonShape(body, ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(19.0f, 10.0f);
            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = body;
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.lowerAngle = -0.25f * B2_PI;
            jointDef.upperAngle = 0.0f * B2_PI;
            jointDef.enableLimit = true;
            jointDef.enableMotor = true;
            jointDef.motorSpeed = 0.0f;
            jointDef.maxMotorTorque = m_motorTorque;

            m_jointId2 = b2CreateRevoluteJoint(m_worldId, ref jointDef);
        }
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 220.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, B2.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Revolute Joint", ref open, ImGuiWindowFlags.NoResize);

        if (ImGui.Checkbox("Limit", ref m_enableLimit))
        {
            b2RevoluteJoint_EnableLimit(m_jointId1, m_enableLimit);
            b2Joint_WakeBodies(m_jointId1);
        }

        if (ImGui.Checkbox("Motor", ref m_enableMotor))
        {
            b2RevoluteJoint_EnableMotor(m_jointId1, m_enableMotor);
            b2Joint_WakeBodies(m_jointId1);
        }

        if (m_enableMotor)
        {
            if (ImGui.SliderFloat("Max Torque", ref m_motorTorque, 0.0f, 5000.0f, "%.0f"))
            {
                b2RevoluteJoint_SetMaxMotorTorque(m_jointId1, m_motorTorque);
                b2Joint_WakeBodies(m_jointId1);
            }

            if (ImGui.SliderFloat("Speed", ref m_motorSpeed, -20.0f, 20.0f, "%.0f"))
            {
                b2RevoluteJoint_SetMotorSpeed(m_jointId1, m_motorSpeed);
                b2Joint_WakeBodies(m_jointId1);
            }
        }

        if (ImGui.Checkbox("Spring", ref m_enableSpring))
        {
            b2RevoluteJoint_EnableSpring(m_jointId1, m_enableSpring);
            b2Joint_WakeBodies(m_jointId1);
        }

        if (m_enableSpring)
        {
            if (ImGui.SliderFloat("Hertz", ref m_hertz, 0.0f, 10.0f, "%.1f"))
            {
                b2RevoluteJoint_SetSpringHertz(m_jointId1, m_hertz);
                b2Joint_WakeBodies(m_jointId1);
            }

            if (ImGui.SliderFloat("Damping", ref m_dampingRatio, 0.0f, 2.0f, "%.1f"))
            {
                b2RevoluteJoint_SetSpringDampingRatio(m_jointId1, m_dampingRatio);
                b2Joint_WakeBodies(m_jointId1);
            }
        }

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        float angle1 = b2RevoluteJoint_GetAngle(m_jointId1);
        B2.g_draw.DrawString(5, m_textLine, $"Angle (Deg) 1 = {angle1:2,F1}");
        m_textLine += m_textIncrement;

        float torque1 = b2RevoluteJoint_GetMotorTorque(m_jointId1);
        B2.g_draw.DrawString(5, m_textLine, $"Motor Torque 1 = {torque1:4,F1}");
        m_textLine += m_textIncrement;

        float torque2 = b2RevoluteJoint_GetMotorTorque(m_jointId2);
        B2.g_draw.DrawString(5, m_textLine, $"Motor Torque 2 = {torque2:4,F1}");
        m_textLine += m_textIncrement;
    }
}