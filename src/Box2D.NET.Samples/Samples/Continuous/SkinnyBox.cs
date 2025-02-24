﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.Shared.random;

namespace Box2D.NET.Samples.Samples.Continuous;

public class SkinnyBox : Sample
{
    b2BodyId m_bodyId, m_bulletId;
    float m_angularVelocity;
    float m_x;
    bool m_capsule;
    bool m_autoTest;
    bool m_bullet;

    static int sampleSkinnyBox = RegisterSample("Continuous", "Skinny Box", Create);

    static Sample Create(Settings settings)
    {
        return new SkinnyBox(settings);
    }

    public SkinnyBox(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(1.0f, 5.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.25f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2Segment segment = new b2Segment(new b2Vec2(-10.0f, 0.0f), new b2Vec2(10.0f, 0.0f));
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.9f;
            b2CreateSegmentShape(groundId, shapeDef, segment);

            b2Polygon box = b2MakeOffsetBox(0.1f, 1.0f, new b2Vec2(0.0f, 1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);
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

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = new b2Vec2(0.0f, 8.0f);
        bodyDef.angularVelocity = m_angularVelocity;
        bodyDef.linearVelocity = new b2Vec2(0.0f, -100.0f);

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.friction = 0.9f;

        m_bodyId = b2CreateBody(m_worldId, bodyDef);

        if (m_capsule)
        {
            b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -1.0f), new b2Vec2(0.0f, 1.0f), 0.1f);
            b2CreateCapsuleShape(m_bodyId, shapeDef, capsule);
        }
        else
        {
            b2Polygon polygon = b2MakeBox(2.0f, 0.05f);
            b2CreatePolygonShape(m_bodyId, shapeDef, polygon);
        }

        if (m_bullet)
        {
            b2Polygon polygon = b2MakeBox(0.25f, 0.25f);
            m_x = RandomFloatRange(-1.0f, 1.0f);
            bodyDef.position = new b2Vec2(m_x, 10.0f);
            bodyDef.linearVelocity = new b2Vec2(0.0f, -50.0f);
            m_bulletId = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(m_bulletId, shapeDef, polygon);
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 110.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(140.0f, height));

        ImGui.Begin("Skinny Box", ref open, ImGuiWindowFlags.NoResize);

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
