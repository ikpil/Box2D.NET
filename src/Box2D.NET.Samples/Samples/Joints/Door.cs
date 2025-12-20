// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples.Samples.Joints;

// A top down door
public class Door : Sample
{
    private static readonly int SampleDoor = SampleFactory.Shared.RegisterSample("Joints", "Door", Create);

    private B2BodyId m_doorId;
    private B2JointId m_jointId;
    private float m_impulse;
    private float m_translationError;
    private float m_jointHertz;
    private float m_jointDampingRatio;
    private bool m_enableLimit;

    private static Sample Create(SampleContext context)
    {
        return new Door(context);
    }

    public Door(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 0.0f);
            m_context.camera.zoom = 4.0f;
        }

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, 0.0f);
            groundId = b2CreateBody(m_worldId, bodyDef);
        }

        m_enableLimit = true;
        m_impulse = 50000.0f;
        m_translationError = 0.0f;
        m_jointHertz = 240.0f;
        m_jointDampingRatio = 1.0f;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 1.5f);
            bodyDef.gravityScale = 0.0f;

            m_doorId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1000.0f;

            B2Polygon box = b2MakeBox(0.1f, 1.5f);
            b2CreatePolygonShape(m_doorId, ref shapeDef, ref box);

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = m_doorId;
            jointDef.@base.localFrameA.p = new B2Vec2(0.0f, 0.0f);
            jointDef.@base.localFrameB.p = new B2Vec2(0.0f, -1.5f);
            jointDef.@base.constraintHertz = m_jointHertz;
            jointDef.@base.constraintDampingRatio = m_jointDampingRatio;
            jointDef.targetAngle = 0.0f;
            jointDef.enableSpring = true;
            jointDef.hertz = 1.0f;
            jointDef.dampingRatio = 0.5f;
            jointDef.motorSpeed = 0.0f;
            jointDef.maxMotorTorque = 0.0f;
            jointDef.enableMotor = false;
            jointDef.lowerAngle = -0.5f * B2_PI;
            jointDef.upperAngle = 0.5f * B2_PI;
            jointDef.enableLimit = m_enableLimit;

            m_jointId = b2CreateRevoluteJoint(m_worldId, ref jointDef);
        }
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 220.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Door", ImGuiWindowFlags.NoResize);

        if (ImGui.Button("impulse"))
        {
            B2Vec2 p = b2Body_GetWorldPoint(m_doorId, new B2Vec2(0.0f, 1.5f));
            b2Body_ApplyLinearImpulse(m_doorId, new B2Vec2(m_impulse, 0.0f), p, true);
            m_translationError = 0.0f;
        }

        ImGui.SliderFloat("magnitude", ref m_impulse, 1000.0f, 100000.0f, "%.0f");

        if (ImGui.Checkbox("limit", ref m_enableLimit))
        {
            b2RevoluteJoint_EnableLimit(m_jointId, m_enableLimit);
        }

        if (ImGui.SliderFloat("hertz", ref m_jointHertz, 15.0f, 480.0f, "%.0f"))
        {
            b2Joint_SetConstraintTuning(m_jointId, m_jointHertz, m_jointDampingRatio);
        }

        if (ImGui.SliderFloat("damping", ref m_jointDampingRatio, 0.0f, 10.0f, "%.1f"))
        {
            b2Joint_SetConstraintTuning(m_jointId, m_jointHertz, m_jointDampingRatio);
        }

        ImGui.End();
    }

    public override void Draw()
    {
        base.Draw();

        B2Vec2 p = b2Body_GetWorldPoint(m_doorId, new B2Vec2(0.0f, 1.5f));
        DrawPoint(m_draw, p, 5.0f, B2HexColor.b2_colorDarkKhaki);

        float translationError = b2Joint_GetLinearSeparation(m_jointId);
        m_translationError = b2MaxFloat(m_translationError, translationError);

        DrawTextLine($"translation error = {m_translationError}");
    }
}