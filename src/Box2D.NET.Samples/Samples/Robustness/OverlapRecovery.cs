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
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples.Robustness;

public class OverlapRecovery : Sample
{
    private static readonly int SampleOverlapRecovery = SampleFactory.Shared.RegisterSample("Robustness", "Overlap Recovery", Create);

    private B2BodyId[] m_bodyIds;
    private int m_bodyCount;
    private int m_baseCount;
    private float m_overlap;
    private float m_extent;
    private float m_speed;
    private float m_hertz;
    private float m_dampingRatio;

    private static Sample Create(SampleContext context)
    {
        return new OverlapRecovery(context);
    }

    public OverlapRecovery(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 2.5f);
            m_camera.zoom = 3.75f;
        }

        m_bodyIds = null;
        m_bodyCount = 0;
        m_baseCount = 4;
        m_overlap = 0.25f;
        m_extent = 0.5f;
        m_speed = 3.0f;
        m_hertz = 30.0f;
        m_dampingRatio = 10.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
        b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

        CreateScene();
    }


    void CreateScene()
    {
        for (int i = 0; i < m_bodyCount; ++i)
        {
            b2DestroyBody(m_bodyIds[i]);
        }

        b2World_SetContactTuning(m_worldId, m_hertz, m_dampingRatio, m_speed);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;

        B2Polygon box = b2MakeBox(m_extent, m_extent);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;

        m_bodyCount = m_baseCount * (m_baseCount + 1) / 2;
        m_bodyIds = new B2BodyId[m_bodyCount];

        int bodyIndex = 0;
        float fraction = 1.0f - m_overlap;
        float y = m_extent;
        for (int i = 0; i < m_baseCount; ++i)
        {
            float x = fraction * m_extent * (i - m_baseCount);
            for (int j = i; j < m_baseCount; ++j)
            {
                bodyDef.position = new B2Vec2(x, y);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

                m_bodyIds[bodyIndex++] = bodyId;

                x += 2.0f * fraction * m_extent;
            }

            y += 2.0f * fraction * m_extent;
        }

        B2_ASSERT(bodyIndex == m_bodyCount);
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 210.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(220.0f, height));

        ImGui.Begin("Overlap Recovery", ImGuiWindowFlags.NoResize);
        ImGui.PushItemWidth(100.0f);

        bool changed = false;
        changed = changed || ImGui.SliderFloat("Extent", ref m_extent, 0.1f, 1.0f, "%.1f");
        changed = changed || ImGui.SliderInt("Base Count", ref m_baseCount, 1, 10);
        changed = changed || ImGui.SliderFloat("Overlap", ref m_overlap, 0.0f, 1.0f, "%.2f");
        changed = changed || ImGui.SliderFloat("Speed", ref m_speed, 0.0f, 10.0f, "%.1f");
        changed = changed || ImGui.SliderFloat("Hertz", ref m_hertz, 0.0f, 240.0f, "%.f");
        changed = changed || ImGui.SliderFloat("Damping Ratio", ref m_dampingRatio, 0.0f, 20.0f, "%.1f");
        changed = changed || ImGui.Button("Reset Scene");

        if (changed)
        {
            CreateScene();
        }

        ImGui.PopItemWidth();
        ImGui.End();
    }
}