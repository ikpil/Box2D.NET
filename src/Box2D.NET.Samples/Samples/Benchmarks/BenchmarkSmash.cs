// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using static Box2D.NET.Shared.Benchmarks;
using static Box2D.NET.B2Bodies;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkSmash : Sample
{
    private static readonly int SampleBenchmarkSmash = SampleFactory.Shared.RegisterSample("Benchmark", "Smash", Create);

    private int m_rowCount;
    private int m_columnCount;
    private List<B2BodyId> _bodyIds;

    private const int MaxRowCount = 160;
    private const int MaxColumnCount = 1200;

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkSmash(context);
    }

    public BenchmarkSmash(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(60.0f, 6.0f);
            m_camera.m_zoom = 25.0f * 1.6f;
        }

        m_rowCount = m_isDebug ? 10 : 80;
        m_columnCount = m_isDebug ? 20 : 120;

        _bodyIds = new List<B2BodyId>();

        CreateScene();
    }

    private void CreateScene()
    {
        foreach (var bodyId in _bodyIds)
        {
            b2DestroyBody(bodyId);
        }

        _bodyIds.Clear();
        CreateSmash(m_worldId, m_rowCount, m_columnCount, _bodyIds);
    }


    public override void UpdateGui()
    {
        base.UpdateGui();

        float height = 110.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(220.0f, height));
        ImGui.Begin("Benchmark: Smash", ImGuiWindowFlags.NoResize);

        bool changed = false;
        if (ImGui.SliderInt("rows", ref m_rowCount, 1, MaxRowCount, "%d"))
        {
            changed = true;
        }

        if (ImGui.SliderInt("columns", ref m_columnCount, 1, MaxColumnCount, "%d"))
        {
            changed = true;
        }

        if (ImGui.Button("Reset Scene"))
        {
            changed = true;
        }
        
        if (changed)
        {
            CreateScene();
        }

        ImGui.End();
    }
}