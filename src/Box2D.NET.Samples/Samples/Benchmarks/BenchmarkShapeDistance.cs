// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.Shared.RandomSupports;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Timers;
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkShapeDistance : Sample
{
    private static readonly int SampleBenchmarkShapeDistance = SampleFactory.Shared.RegisterSample("Benchmark", "Shape Distance", Create);

    private int m_count;
    private B2Transform[] m_transformAs;
    private B2Transform[] m_transformBs;
    private B2DistanceOutput[] m_outputs;
    private B2Polygon m_polygonA;
    private B2Polygon m_polygonB;
    private float m_minMilliseconds;
    private int m_drawIndex;
    private int m_minCycles;

    // for draw
    private int totalIterations = 0;

    public static Sample Create(SampleContext context)
    {
        return new BenchmarkShapeDistance(context);
    }

    public BenchmarkShapeDistance(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 0.0f);
            m_camera.zoom = 3.0f;
        }

        {
            B2Vec2[] points = new B2Vec2[8];
            B2Rot q = b2MakeRot(2.0f * B2_PI / 8.0f);
            B2Vec2 p = new B2Vec2(0.5f, 0.0f);
            points[0] = p;
            for (int i = 1; i < 8; ++i)
            {
                points[i] = b2RotateVector(q, points[i - 1]);
            }

            B2Hull hull = b2ComputeHull(points, 8);
            m_polygonA = b2MakePolygon(hull, 0.0f);
        }

        {
            B2Vec2[] points = new B2Vec2[8];
            B2Rot q = b2MakeRot(2.0f * B2_PI / 8.0f);
            B2Vec2 p = new B2Vec2(0.5f, 0.0f);
            points[0] = p;
            for (int i = 1; i < 8; ++i)
            {
                points[i] = b2RotateVector(q, points[i - 1]);
            }

            B2Hull hull = b2ComputeHull(points, 8);
            m_polygonB = b2MakePolygon(hull, 0.1f);
        }

        m_count = m_isDebug ? 100 : 10000;

        // todo arena
        m_transformAs = new B2Transform[m_count];
        m_transformBs = new B2Transform[m_count];
        m_outputs = new B2DistanceOutput[m_count];

        g_randomSeed = 42;
        for (int i = 0; i < m_count; ++i)
        {
            m_transformAs[i] = new B2Transform(RandomVec2(-0.1f, 0.1f), RandomRot());
            m_transformBs[i] = new B2Transform(RandomVec2(0.25f, 2.0f), RandomRot());
        }

        m_drawIndex = 0;
        m_minCycles = int.MaxValue;
        m_minMilliseconds = float.MaxValue;
    }

    public override void Dispose()
    {
        base.Dispose();
        m_transformAs = null;
        m_transformBs = null;
        m_outputs = null;
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 5.0f * fontSize;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(17.0f * fontSize, height));
        ImGui.Begin("Benchmark: Shape Distance", ImGuiWindowFlags.NoResize);

        ImGui.SliderInt("draw index", ref m_drawIndex, 0, m_count - 1);

        ImGui.End();
    }

    public override void Step()
    {
        if (m_context.pause == false || m_context.singleStep == true)
        {
            B2DistanceInput input = new B2DistanceInput();
            input.proxyA = b2MakeProxy(m_polygonA.vertices.AsSpan(), m_polygonA.count, m_polygonA.radius);
            input.proxyB = b2MakeProxy(m_polygonB.vertices.AsSpan(), m_polygonB.count, m_polygonB.radius);
            input.useRadii = true;
            totalIterations = 0;

            ulong start = b2GetTicks();
            ulong startCycles = b2GetTicks();
            for (int i = 0; i < m_count; ++i)
            {
                B2SimplexCache cache = new B2SimplexCache();
                input.transformA = m_transformAs[i];
                input.transformB = m_transformBs[i];
                m_outputs[i] = b2ShapeDistance(ref input, ref cache, null, 0);
                totalIterations += m_outputs[i].iterations;
            }

            ulong endCycles = b2GetTicks();

            float ms = b2GetMilliseconds(start);
            m_minCycles = b2MinInt(m_minCycles, (int)endCycles - (int)startCycles);
            m_minMilliseconds = b2MinFloat(m_minMilliseconds, ms);
        }


        base.Step();
    }

    public override void Draw()
    {
        base.Draw();

        if (m_context.pause == false || m_context.singleStep == true)
        {
            DrawTextLine($"count = {m_count}");
            DrawTextLine($"min cycles = {m_minCycles}");
            DrawTextLine($"ave cycles = {(float)m_minCycles / m_count}");
            DrawTextLine($"min ms = {m_minMilliseconds}, ave us = {1000.0f * m_minMilliseconds / (float)m_count}");
            DrawTextLine($"average iterations = {totalIterations / (float)m_count}");
        }

        B2Transform xfA = m_transformAs[m_drawIndex];
        B2Transform xfB = m_transformBs[m_drawIndex];
        B2DistanceOutput output = m_outputs[m_drawIndex];
        DrawSolidPolygon(m_draw, xfA, m_polygonA.vertices.AsSpan(), m_polygonA.count, m_polygonA.radius, B2HexColor.b2_colorBox2DGreen);
        DrawSolidPolygon(m_draw, xfB, m_polygonB.vertices.AsSpan(), m_polygonB.count, m_polygonB.radius, B2HexColor.b2_colorBox2DBlue);
        DrawLine(m_draw, output.pointA, output.pointB, B2HexColor.b2_colorDimGray);
        DrawPoint(m_draw, output.pointA, 10.0f, B2HexColor.b2_colorWhite);
        DrawPoint(m_draw, output.pointB, 10.0f, B2HexColor.b2_colorWhite);
        DrawLine(m_draw, output.pointA, output.pointA + 0.5f * output.normal, B2HexColor.b2_colorYellow);
        DrawTextLine($"distance = {output.distance}");
    }
}