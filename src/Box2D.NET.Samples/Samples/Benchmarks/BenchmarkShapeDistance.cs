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
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 0.0f);
            m_context.camera.m_zoom = 3.0f;
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
            m_polygonA = b2MakePolygon(ref hull, 0.0f);
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
            m_polygonB = b2MakePolygon(ref hull, 0.1f);
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
        float height = 80.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(220.0f, height));
        ImGui.Begin("Benchmark: Shape Distance", ImGuiWindowFlags.NoResize);

        ImGui.SliderInt("draw index", ref m_drawIndex, 0, m_count - 1);

        ImGui.End();
    }

    public override void Step()
    {
        if (m_context.settings.pause == false || m_context.settings.singleStep == true)
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

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        if (m_context.settings.pause == false || m_context.settings.singleStep == true)
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
        m_context.draw.DrawSolidPolygon(ref xfA, m_polygonA.vertices.AsSpan(), m_polygonA.count, m_polygonA.radius, B2HexColor.b2_colorBox2DGreen);
        m_context.draw.DrawSolidPolygon(ref xfB, m_polygonB.vertices.AsSpan(), m_polygonB.count, m_polygonB.radius, B2HexColor.b2_colorBox2DBlue);
        m_context.draw.DrawSegment(output.pointA, output.pointB, B2HexColor.b2_colorDimGray);
        m_context.draw.DrawPoint(output.pointA, 10.0f, B2HexColor.b2_colorWhite);
        m_context.draw.DrawPoint(output.pointB, 10.0f, B2HexColor.b2_colorWhite);
        m_context.draw.DrawSegment(output.pointA, output.pointA + 0.5f * output.normal, B2HexColor.b2_colorYellow);
        DrawTextLine($"distance = {output.distance}");
    }
}
