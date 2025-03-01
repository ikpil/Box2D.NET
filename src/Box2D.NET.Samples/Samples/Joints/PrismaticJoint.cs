// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2PrismaticJoints;

namespace Box2D.NET.Samples.Samples.Joints;

public class PrismaticJoint : Sample
{
    B2JointId m_jointId;
    float m_motorSpeed;
    float m_motorForce;
    float m_hertz;
    float m_dampingRatio;
    bool m_enableSpring;
    bool m_enableMotor;
    bool m_enableLimit;
    private static readonly int SamplePrismatic = SampleFactory.Shared.RegisterSample("Joints", "Prismatic", Create);

    private static Sample Create(Settings settings)
    {
        return new PrismaticJoint(settings);
    }

    public PrismaticJoint(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.0f, 8.0f);
            B2.g_camera.m_zoom = 25.0f * 0.5f;
        }

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);
        }

        m_enableSpring = false;
        m_enableLimit = true;
        m_enableMotor = false;
        m_motorSpeed = 2.0f;
        m_motorForce = 25.0f;
        m_hertz = 1.0f;
        m_dampingRatio = 0.5f;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, 10.0f);
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeBox(0.5f, 2.0f);
            b2CreatePolygonShape(bodyId, shapeDef, box);

            B2Vec2 pivot = new B2Vec2(0.0f, 9.0f);
            // B2Vec2 axis = b2Normalize({1.0f, 0.0f});
            B2Vec2 axis = b2Normalize(new B2Vec2(1.0f, 1.0f));
            B2PrismaticJointDef jointDef = b2DefaultPrismaticJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = bodyId;
            jointDef.localAxisA = b2Body_GetLocalVector(jointDef.bodyIdA, axis);
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.motorSpeed = m_motorSpeed;
            jointDef.maxMotorForce = m_motorForce;
            jointDef.enableMotor = m_enableMotor;
            jointDef.lowerTranslation = -10.0f;
            jointDef.upperTranslation = 10.0f;
            jointDef.enableLimit = m_enableLimit;
            jointDef.enableSpring = m_enableSpring;
            jointDef.hertz = m_hertz;
            jointDef.dampingRatio = m_dampingRatio;

            m_jointId = b2CreatePrismaticJoint(m_worldId, jointDef);
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 220.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, B2.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Prismatic Joint", ref open, ImGuiWindowFlags.NoResize);

        if (ImGui.Checkbox("Limit", ref m_enableLimit))
        {
            b2PrismaticJoint_EnableLimit(m_jointId, m_enableLimit);
            b2Joint_WakeBodies(m_jointId);
        }

        if (ImGui.Checkbox("Motor", ref m_enableMotor))
        {
            b2PrismaticJoint_EnableMotor(m_jointId, m_enableMotor);
            b2Joint_WakeBodies(m_jointId);
        }

        if (m_enableMotor)
        {
            if (ImGui.SliderFloat("Max Force", ref m_motorForce, 0.0f, 200.0f, "%.0f"))
            {
                b2PrismaticJoint_SetMaxMotorForce(m_jointId, m_motorForce);
                b2Joint_WakeBodies(m_jointId);
            }

            if (ImGui.SliderFloat("Speed", ref m_motorSpeed, -40.0f, 40.0f, "%.0f"))
            {
                b2PrismaticJoint_SetMotorSpeed(m_jointId, m_motorSpeed);
                b2Joint_WakeBodies(m_jointId);
            }
        }

        if (ImGui.Checkbox("Spring", ref m_enableSpring))
        {
            b2PrismaticJoint_EnableSpring(m_jointId, m_enableSpring);
            b2Joint_WakeBodies(m_jointId);
        }

        if (m_enableSpring)
        {
            if (ImGui.SliderFloat("Hertz", ref m_hertz, 0.0f, 10.0f, "%.1f"))
            {
                b2PrismaticJoint_SetSpringHertz(m_jointId, m_hertz);
                b2Joint_WakeBodies(m_jointId);
            }

            if (ImGui.SliderFloat("Damping", ref m_dampingRatio, 0.0f, 2.0f, "%.1f"))
            {
                b2PrismaticJoint_SetSpringDampingRatio(m_jointId, m_dampingRatio);
                b2Joint_WakeBodies(m_jointId);
            }
        }

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        float force = b2PrismaticJoint_GetMotorForce(m_jointId);
        B2.g_draw.DrawString(5, m_textLine, "Motor Force = %4.1f", force);
        m_textLine += m_textIncrement;

        float translation = b2PrismaticJoint_GetTranslation(m_jointId);
        B2.g_draw.DrawString(5, m_textLine, "Translation = %4.1f", translation);
        m_textLine += m_textIncrement;

        float speed = b2PrismaticJoint_GetSpeed(m_jointId);
        B2.g_draw.DrawString(5, m_textLine, "Speed = %4.8f", speed);
        m_textLine += m_textIncrement;
    }
}
