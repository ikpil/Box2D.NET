// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Bodies;

public class WakeTouching : Sample
{
    private static readonly int SampleWakeTouching = SampleFactory.Shared.RegisterSample("Bodies", "Wake Touching", Create);

    private const int m_count = 10;

    private B2BodyId m_groundId;

    private static Sample Create(SampleContext context)
    {
        return new WakeTouching(context);
    }


    public WakeTouching(SampleContext context)
        : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 4.0f);
            m_context.camera.zoom = 8.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            m_groundId = b2CreateBody(m_worldId, bodyDef);

            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(m_groundId, shapeDef, segment);
        }

        {
            B2Polygon box = b2MakeBox(0.5f, 0.5f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            float x = -1.0f * (m_count - 1);

            for (int i = 0; i < m_count; ++i)
            {
                bodyDef.position = new B2Vec2(x, 4.0f);
                B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                b2CreatePolygonShape(bodyId, shapeDef, box);
                x += 2.0f;
            }
        }
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 5.0f * fontSize;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(10.0f * fontSize, height));

        ImGui.Begin("Wake Touching", ImGuiWindowFlags.NoResize);

        if (ImGui.Button("Wake Touching"))
        {
            b2Body_WakeTouching(m_groundId);
        }

        ImGui.End();
    }
}