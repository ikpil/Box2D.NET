// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Continuous;

public class ChainDrop : Sample
{
    b2BodyId m_bodyId;
    b2ShapeId m_shapeId;
    float m_yOffset;
    float m_speed;

    static int sampleChainDrop = RegisterSample("Continuous", "Chain Drop", Create);

    static Sample Create(Settings settings)
    {
        return new ChainDrop(settings);
    }

    public ChainDrop(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 0.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.35f;
        }

        // 
        //b2World_SetContactTuning( m_worldId, 30.0f, 1.0f, 100.0f );

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = new b2Vec2(0.0f, -6.0f);
        b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        b2Vec2[] points = new b2Vec2[4] { new b2Vec2(-10.0f, -2.0f), new b2Vec2(10.0f, -2.0f), new b2Vec2(10.0f, 1.0f), new b2Vec2(-10.0f, 1.0f) };

        b2ChainDef chainDef = b2DefaultChainDef();
        chainDef.points = points;
        chainDef.count = 4;
        chainDef.isLoop = true;

        b2CreateChain(groundId, chainDef);

        m_bodyId = b2_nullBodyId;
        m_yOffset = -0.1f;
        m_speed = -42.0f;

        Launch();
    }

    void Launch()
    {
        if (B2_IS_NON_NULL(m_bodyId))
        {
            b2DestroyBody(m_bodyId);
        }

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.linearVelocity = new b2Vec2(0.0f, m_speed);
        bodyDef.position = new b2Vec2(0.0f, 10.0f + m_yOffset);
        bodyDef.rotation = b2MakeRot(0.5f * B2_PI);
        bodyDef.fixedRotation = true;
        m_bodyId = b2CreateBody(m_worldId, bodyDef);

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.5f);
        m_shapeId = b2CreateCircleShape(m_bodyId, shapeDef, circle);

        //b2Capsule capsule = { { -0.5f, 0.0f }, { 0.5f, 0.0 }, 0.25f };
        //m_shapeId = b2CreateCapsuleShape( m_bodyId, &shapeDef, &capsule );

        //float h = 0.5f;
        //b2Polygon box = b2MakeBox( h, h );
        //m_shapeId = b2CreatePolygonShape( m_bodyId, &shapeDef, &box );
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 140.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Chain Drop", ref open, ImGuiWindowFlags.NoResize);

        ImGui.SliderFloat("Speed", ref m_speed, -100.0f, 0.0f, "%.0f");
        ImGui.SliderFloat("Y Offset", ref m_yOffset, -1.0f, 1.0f, "%.1f");

        if (ImGui.Button("Launch"))
        {
            Launch();
        }

        ImGui.End();
    }
}
