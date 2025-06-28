// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Samples.Robustness;

// High gravity and high mass ratio. This shows how to tune contact and joint stiffness values to
// achieve an improved result at a high sub-step count.
// There is still a fair bit of bounce with some settings.
public class Cart : Sample
{
    private static readonly int SampleCart = SampleFactory.Shared.RegisterSample("Robustness", "Cart", Create);

    private B2BodyId m_chassisId;
    private B2BodyId m_wheelId1;
    private B2BodyId m_wheelId2;
    private B2JointId m_jointId1;
    private B2JointId m_jointId2;

    private float m_contactHertz;
    private float m_contactDampingRatio;
    private float m_contactSpeed;
    private float m_constraintHertz;
    private float m_constraintDampingRatio;

    private static Sample Create(SampleContext context)
    {
        return new Cart(context);
    }

    public Cart(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 1.0f);
            m_context.camera.m_zoom = 1.5f;
            m_context.settings.subStepCount = 12;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, -1.0f);
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon groundBox = b2MakeBox(20.0f, 1.0f);
            b2CreatePolygonShape(groundId, ref shapeDef, ref groundBox);
        }

        b2World_SetGravity(m_worldId, new B2Vec2(0.0f, -22.0f));

        m_contactHertz = 240.0f;
        m_contactDampingRatio = 10.0f;
        m_contactSpeed = 0.5f;
        b2World_SetContactTuning(m_worldId, m_contactHertz, m_contactDampingRatio, m_contactSpeed);

        m_constraintHertz = 240.0f;
        m_constraintDampingRatio = 0.0f;

        m_chassisId = new B2BodyId();
        m_wheelId1 = new B2BodyId();
        m_wheelId2 = new B2BodyId();
        m_jointId1 = new B2JointId();
        m_jointId2 = new B2JointId();

        CreateScene();
    }

    void CreateScene()
    {
        if (B2_IS_NON_NULL(m_chassisId))
        {
            b2DestroyBody(m_chassisId);
        }

        if (B2_IS_NON_NULL(m_wheelId1))
        {
            b2DestroyBody(m_wheelId1);
        }

        if (B2_IS_NON_NULL(m_wheelId2))
        {
            b2DestroyBody(m_wheelId2);
        }

        float yBase = 2.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(0.0f, yBase);
        m_chassisId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1000.0f;

        B2Polygon box = b2MakeOffsetBox(1.0f, 0.25f, new B2Vec2(0.0f, 0.25f), b2Rot_identity);
        b2CreatePolygonShape(m_chassisId, ref shapeDef, ref box);

        shapeDef = b2DefaultShapeDef();
        shapeDef.material.rollingResistance = 0.02f;
        shapeDef.density = 50.0f;

        B2Circle circle = new B2Circle(b2Vec2_zero, 0.1f);
        bodyDef.position = new B2Vec2(-0.9f, yBase - 0.15f);
        m_wheelId1 = b2CreateBody(m_worldId, ref bodyDef);
        b2CreateCircleShape(m_wheelId1, ref shapeDef, ref circle);

        bodyDef.position = new B2Vec2(0.9f, yBase - 0.15f);
        m_wheelId2 = b2CreateBody(m_worldId, ref bodyDef);
        b2CreateCircleShape(m_wheelId2, ref shapeDef, ref circle);

        B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
        jointDef.@base.constraintHertz = 120.0f;
        jointDef.@base.constraintDampingRatio = 0.0f;

        jointDef.@base.bodyIdA = m_chassisId;
        jointDef.@base.bodyIdB = m_wheelId1;
        jointDef.@base.localFrameA.p = new B2Vec2(-0.9f, -0.15f);
        jointDef.@base.localFrameB.p = new B2Vec2(0.0f, 0.0f);

        m_jointId1 = b2CreateRevoluteJoint(m_worldId, ref jointDef);
        b2Joint_SetConstraintTuning(m_jointId1, m_constraintHertz, m_constraintDampingRatio);

        jointDef.@base.bodyIdA = m_chassisId;
        jointDef.@base.bodyIdB = m_wheelId2;
        jointDef.@base.localFrameA.p = new B2Vec2(0.9f, -0.15f);
        jointDef.@base.localFrameB.p = new B2Vec2(0.0f, 0.0f);

        m_jointId2 = b2CreateRevoluteJoint(m_worldId, ref jointDef);
        b2Joint_SetConstraintTuning(m_jointId2, m_constraintHertz, m_constraintDampingRatio);
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 240.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.m_height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(320.0f, height));

        ImGui.Begin("Cart", ImGuiWindowFlags.NoResize);
        ImGui.PushItemWidth(200.0f);

        bool changed = false;
        ImGui.Text("Contact");
        changed = changed || ImGui.SliderFloat("Hertz##contact", ref m_contactHertz, 0.0f, 240.0f, "%.f");
        changed = changed || ImGui.SliderFloat("Damping Ratio##contact", ref m_contactDampingRatio, 0.0f, 100.0f, "%.f");
        changed = changed || ImGui.SliderFloat("Speed", ref m_contactSpeed, 0.0f, 5.0f, "%.1f");

        if (changed)
        {
            b2World_SetContactTuning(m_worldId, m_contactHertz, m_contactDampingRatio, m_contactSpeed);
            CreateScene();
        }

        ImGui.Separator();

        changed = false;
        ImGui.Text("Joint");
        changed = changed || ImGui.SliderFloat("Hertz##joint", ref m_constraintHertz, 0.0f, 240.0f, "%.f");
        changed = changed || ImGui.SliderFloat("Damping Ratio##joint", ref m_constraintDampingRatio, 0.0f, 20.0f, "%.f");

        ImGui.Separator();

        changed = changed || ImGui.Button("Reset Scene");

        if (changed)
        {
            b2Joint_SetConstraintTuning(m_jointId1, m_constraintHertz, m_constraintDampingRatio);
            b2Joint_SetConstraintTuning(m_jointId2, m_constraintHertz, m_constraintDampingRatio);
            CreateScene();
        }

        ImGui.PopItemWidth();
        ImGui.End();
    }
}