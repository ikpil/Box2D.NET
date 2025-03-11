// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.Shared.RandomSupports;

namespace Box2D.NET.Samples.Samples.Continuous;

public class SkinnyBox : Sample
{
    B2BodyId m_bodyId, m_bulletId;
    float m_angularVelocity;
    float m_x;
    bool m_capsule;
    bool m_autoTest;
    bool m_bullet;

    private static readonly int SampleSkinnyBox = SampleFactory.Shared.RegisterSample("Continuous", "Skinny Box", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new SkinnyBox(ctx, settings);
    }

    public SkinnyBox(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(1.0f, 5.0f);
            m_context.camera.m_zoom = 25.0f * 0.25f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Segment segment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(10.0f, 0.0f));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.9f;
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

            B2Polygon box = b2MakeOffsetBox(0.1f, 1.0f, new B2Vec2(0.0f, 1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        m_autoTest = false;
        m_bullet = false;
        m_capsule = false;

        m_bodyId = b2_nullBodyId;
        m_bulletId = b2_nullBodyId;

        Launch();
    }

    void Launch()
    {
        if (B2_IS_NON_NULL(m_bodyId))
        {
            b2DestroyBody(m_bodyId);
        }

        if (B2_IS_NON_NULL(m_bulletId))
        {
            b2DestroyBody(m_bulletId);
        }

        m_angularVelocity = RandomFloatRange(-50.0f, 50.0f);
        // m_angularVelocity = -30.6695766f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(0.0f, 8.0f);
        bodyDef.angularVelocity = m_angularVelocity;
        bodyDef.linearVelocity = new B2Vec2(0.0f, -100.0f);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.friction = 0.9f;

        m_bodyId = b2CreateBody(m_worldId, ref bodyDef);

        if (m_capsule)
        {
            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -1.0f), new B2Vec2(0.0f, 1.0f), 0.1f);
            b2CreateCapsuleShape(m_bodyId, ref shapeDef, ref capsule);
        }
        else
        {
            B2Polygon polygon = b2MakeBox(2.0f, 0.05f);
            b2CreatePolygonShape(m_bodyId, ref shapeDef, ref polygon);
        }

        if (m_bullet)
        {
            B2Polygon polygon = b2MakeBox(0.25f, 0.25f);
            m_x = RandomFloatRange(-1.0f, 1.0f);
            bodyDef.position = new B2Vec2(m_x, 10.0f);
            bodyDef.linearVelocity = new B2Vec2(0.0f, -50.0f);
            m_bulletId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(m_bulletId, ref shapeDef, ref polygon);
        }
    }

    public override void UpdateUI()
    {
        
        float height = 110.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(140.0f, height));

        ImGui.Begin("Skinny Box", ImGuiWindowFlags.NoResize);

        ImGui.Checkbox("Capsule", ref m_capsule);

        if (ImGui.Button("Launch"))
        {
            Launch();
        }

        ImGui.Checkbox("Auto Test", ref m_autoTest);

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        if (m_autoTest && m_stepCount % 60 == 0)
        {
            Launch();
        }
    }
}
