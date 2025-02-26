// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2WheelJoints;

namespace Box2D.NET.Samples.Samples.Joints;

public class WheelJoint : Sample
{
    B2JointId m_jointId;
    float m_hertz;
    float m_dampingRatio;
    float m_motorSpeed;
    float m_motorTorque;
    bool m_enableSpring;
    bool m_enableMotor;
    bool m_enableLimit;

    static int sampleWheel = RegisterSample("Joints", "Wheel", Create);

    static Sample Create(Settings settings)
    {
        return new WheelJoint(settings);
    }

    public WheelJoint(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new B2Vec2(0.0f, 10.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.15f;
        }

        B2BodyId groundId;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);
        }

        m_enableSpring = true;
        m_enableLimit = true;
        m_enableMotor = true;
        m_motorSpeed = 2.0f;
        m_motorTorque = 5.0f;
        m_hertz = 1.0f;
        m_dampingRatio = 0.7f;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, 10.25f);
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.5f), new B2Vec2(0.0f, 0.5f), 0.5f);
            b2CreateCapsuleShape(bodyId, shapeDef, capsule);

            B2Vec2 pivot = new B2Vec2(0.0f, 10.0f);
            B2Vec2 axis = b2Normalize(new B2Vec2(1.0f, 1.0f));
            B2WheelJointDef jointDef = b2DefaultWheelJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = bodyId;
            jointDef.localAxisA = b2Body_GetLocalVector(jointDef.bodyIdA, axis);
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.motorSpeed = m_motorSpeed;
            jointDef.maxMotorTorque = m_motorTorque;
            jointDef.enableMotor = m_enableMotor;
            jointDef.lowerTranslation = -3.0f;
            jointDef.upperTranslation = 3.0f;
            jointDef.enableLimit = m_enableLimit;
            jointDef.hertz = m_hertz;
            jointDef.dampingRatio = m_dampingRatio;

            m_jointId = b2CreateWheelJoint(m_worldId, jointDef);
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 220.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Wheel Joint", ref open, ImGuiWindowFlags.NoResize);

        if (ImGui.Checkbox("Limit", ref m_enableLimit))
        {
            b2WheelJoint_EnableLimit(m_jointId, m_enableLimit);
        }

        if (ImGui.Checkbox("Motor", ref m_enableMotor))
        {
            b2WheelJoint_EnableMotor(m_jointId, m_enableMotor);
        }

        if (m_enableMotor)
        {
            if (ImGui.SliderFloat("Torque", ref m_motorTorque, 0.0f, 20.0f, "%.0f"))
            {
                b2WheelJoint_SetMaxMotorTorque(m_jointId, m_motorTorque);
            }

            if (ImGui.SliderFloat("Speed", ref m_motorSpeed, -20.0f, 20.0f, "%.0f"))
            {
                b2WheelJoint_SetMotorSpeed(m_jointId, m_motorSpeed);
            }
        }

        if (ImGui.Checkbox("Spring", ref m_enableSpring))
        {
            b2WheelJoint_EnableSpring(m_jointId, m_enableSpring);
        }

        if (m_enableSpring)
        {
            if (ImGui.SliderFloat("Hertz", ref m_hertz, 0.0f, 10.0f, "%.1f"))
            {
                b2WheelJoint_SetSpringHertz(m_jointId, m_hertz);
            }

            if (ImGui.SliderFloat("Damping", ref m_dampingRatio, 0.0f, 2.0f, "%.1f"))
            {
                b2WheelJoint_SetSpringDampingRatio(m_jointId, m_dampingRatio);
            }
        }

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        float torque = b2WheelJoint_GetMotorTorque(m_jointId);
        Draw.g_draw.DrawString(5, m_textLine, "Motor Torque = %4.1f", torque);
        m_textLine += m_textIncrement;
    }
}
