// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Continuous;

public class ChainDrop : Sample
{
    private static readonly int SampleChainDrop = SampleFactory.Shared.RegisterSample("Continuous", "Chain Drop", Create);

    private B2BodyId m_bodyId;
    private B2ShapeId m_shapeId;
    private float m_yOffset;
    private float m_speed;

    private static Sample Create(SampleContext context)
    {
        return new ChainDrop(context);
    }

    public ChainDrop(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 0.0f);
            m_camera.m_zoom = 25.0f * 0.35f;
        }

        // 
        //b2World_SetContactTuning( m_worldId, 30.0f, 1.0f, 100.0f );

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = new B2Vec2(0.0f, -6.0f);
        B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

        B2Vec2[] points = new B2Vec2[4] { new B2Vec2(-10.0f, -2.0f), new B2Vec2(10.0f, -2.0f), new B2Vec2(10.0f, 1.0f), new B2Vec2(-10.0f, 1.0f) };

        B2ChainDef chainDef = b2DefaultChainDef();
        chainDef.points = points;
        chainDef.count = 4;
        chainDef.isLoop = true;

        b2CreateChain(groundId, ref chainDef);

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

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.linearVelocity = new B2Vec2(0.0f, m_speed);
        bodyDef.position = new B2Vec2(0.0f, 10.0f + m_yOffset);
        bodyDef.rotation = b2MakeRot(0.5f * B2_PI);
        bodyDef.motionLocks.angularZ = true;
        m_bodyId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        m_shapeId = b2CreateCircleShape(m_bodyId, ref shapeDef, ref circle);

        //b2Capsule capsule = { { -0.5f, 0.0f }, { 0.5f, 0.0f }, 0.25f };
        //m_shapeId = b2CreateCapsuleShape( m_bodyId, &shapeDef, &capsule );

        //float h = 0.5f;
        //b2Polygon box = b2MakeBox( h, h );
        //m_shapeId = b2CreatePolygonShape( m_bodyId, &shapeDef, &box );
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 140.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.m_height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Chain Drop", ImGuiWindowFlags.NoResize);

        ImGui.SliderFloat("Speed", ref m_speed, -100.0f, 0.0f, "%.0f");
        ImGui.SliderFloat("Y Offset", ref m_yOffset, -1.0f, 1.0f, "%.1f");

        if (ImGui.Button("Launch"))
        {
            Launch();
        }

        ImGui.End();
    }
}