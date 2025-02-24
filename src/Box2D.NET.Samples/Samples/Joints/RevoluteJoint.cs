﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.revolute_joint;

namespace Box2D.NET.Samples.Samples.Joints;

public class RevoluteJoint : Sample
{
    b2BodyId m_ball;
    b2JointId m_jointId1;
    b2JointId m_jointId2;
    float m_motorSpeed;
    float m_motorTorque;
    float m_hertz;
    float m_dampingRatio;
    bool m_enableSpring;
    bool m_enableMotor;
    bool m_enableLimit;
    static int sampleRevolute = RegisterSample("Joints", "Revolute", Create);

    static Sample Create(Settings settings)
    {
        return new RevoluteJoint(settings);
    }

    public RevoluteJoint(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 15.5f);
            Draw.g_camera.m_zoom = 25.0f * 0.7f;
        }

        b2BodyId groundId = b2_nullBodyId;
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new b2Vec2(0.0f, -1.0f);
            groundId = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeBox(40.0f, 1.0f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        m_enableSpring = false;
        m_enableLimit = true;
        m_enableMotor = false;
        m_hertz = 1.0f;
        m_dampingRatio = 0.5f;
        m_motorSpeed = 1.0f;
        m_motorTorque = 1000.0f;

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = new b2Vec2(-10.0f, 20.0f);
            b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;

            b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -1.0f), new b2Vec2(0.0f, 6.0f), 0.5f);
            b2CreateCapsuleShape(bodyId, shapeDef, capsule);

            b2Vec2 pivot = new b2Vec2(-10.0f, 20.5f);
            b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

            m_jointId1 = b2CreateRevoluteJoint(m_worldId, jointDef);
        }

        {
            b2Circle circle = new b2Circle(new b2Vec2(), 0.0f);
            circle.radius = 2.0f;

            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = new b2Vec2(5.0f, 30.0f);
            m_ball = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;

            b2CreateCircleShape(m_ball, shapeDef, circle);
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new b2Vec2(20.0f, 10.0f);
            bodyDef.type = b2BodyType.b2_dynamicBody;
            b2BodyId body = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeOffsetBox(10.0f, 0.5f, new b2Vec2(-10.0f, 0.0f), b2Rot_identity);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            b2CreatePolygonShape(body, shapeDef, box);

            b2Vec2 pivot = new b2Vec2(19.0f, 10.0f);
            b2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
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

            m_jointId2 = b2CreateRevoluteJoint(m_worldId, jointDef);
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 220.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
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
        Draw.g_draw.DrawString(5, m_textLine, "Angle (Deg) 1 = %2.1f", angle1);
        m_textLine += m_textIncrement;

        float torque1 = b2RevoluteJoint_GetMotorTorque(m_jointId1);
        Draw.g_draw.DrawString(5, m_textLine, "Motor Torque 1 = %4.1f", torque1);
        m_textLine += m_textIncrement;

        float torque2 = b2RevoluteJoint_GetMotorTorque(m_jointId2);
        Draw.g_draw.DrawString(5, m_textLine, "Motor Torque 2 = %4.1f", torque2);
        m_textLine += m_textIncrement;
    }
}
