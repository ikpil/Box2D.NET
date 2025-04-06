﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Numerics;
using Box2D.NET.Samples.Extensions;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.Shared.RandomSupports;
using static Box2D.NET.B2Timers;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkCast : Sample
{
    private static readonly int SampleBenchmarkCast = SampleFactory.Shared.RegisterSample("Benchmark", "Cast", Create);

    private QueryType m_queryType;

    private List<B2Vec2> m_origins = new List<B2Vec2>();
    private List<B2Vec2> m_translations = new List<B2Vec2>();
    private float m_minTime;
    private float m_buildTime;

    private int m_rowCount, m_columnCount;
    private int m_updateType;
    private int m_drawIndex;
    private float m_radius;
    private float m_fill;
    private float m_ratio;
    private float m_grid;
    private bool m_topDown;

    private int sampleCount;
    private int hitCount = 0;
    private int nodeVisits = 0;
    private int leafVisits = 0;
    private float ms = 0.0f;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BenchmarkCast(ctx, settings);
    }

    public BenchmarkCast(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(500.0f, 500.0f);
            m_context.camera.m_zoom = 25.0f * 21.0f;
            // settings.drawShapes = m_context.g_sampleDebug;
        }

        m_queryType = QueryType.e_circleCast;
        m_ratio = 5.0f;
        m_grid = 1.0f;
        m_fill = 0.1f;
        m_rowCount = m_context.sampleDebug ? 100 : 1000;
        m_columnCount = m_context.sampleDebug ? 100 : 1000;
        m_minTime = 1e6f;
        m_drawIndex = 0;
        m_topDown = false;
        m_buildTime = 0.0f;
        m_radius = 0.1f;

        g_seed = 1234;
        sampleCount = m_context.sampleDebug ? 100 : 10000;
        m_origins.Resize(sampleCount);
        m_translations.Resize(sampleCount);
        float extent = m_rowCount * m_grid;

        // Pre-compute rays to avoid randomizer overhead
        for (int i = 0; i < sampleCount; ++i)
        {
            B2Vec2 rayStart = RandomVec2(0.0f, extent);
            B2Vec2 rayEnd = RandomVec2(0.0f, extent);

            m_origins[i] = rayStart;
            m_translations[i] = rayEnd - rayStart;
        }

        BuildScene();
    }

    void BuildScene()
    {
        g_seed = 1234;
        b2DestroyWorld(m_worldId);
        B2WorldDef worldDef = b2DefaultWorldDef();
        m_worldId = b2CreateWorld(ref worldDef);

        ulong ticks = b2GetTicks();

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        float y = 0.0f;

        for (int i = 0; i < m_rowCount; ++i)
        {
            float x = 0.0f;

            for (int j = 0; j < m_columnCount; ++j)
            {
                float fillTest = RandomFloatRange(0.0f, 1.0f);
                if (fillTest <= m_fill)
                {
                    bodyDef.position = new B2Vec2(x, y);
                    B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

                    float ratio = RandomFloatRange(1.0f, m_ratio);
                    float halfWidth = RandomFloatRange(0.05f, 0.25f);

                    B2Polygon box;
                    if (RandomFloat() > 0.0f)
                    {
                        box = b2MakeBox(ratio * halfWidth, halfWidth);
                    }
                    else
                    {
                        box = b2MakeBox(halfWidth, ratio * halfWidth);
                    }

                    int category = (int)RandomIntRange(0, 2);
                    shapeDef.filter.categoryBits = (ulong)(1 << category);
                    if (category == 0)
                    {
                        shapeDef.material.customColor = (uint)B2HexColor.b2_colorBox2DBlue;
                    }
                    else if (category == 1)
                    {
                        shapeDef.material.customColor = (uint)B2HexColor.b2_colorBox2DYellow;
                    }
                    else
                    {
                        shapeDef.material.customColor = (uint)B2HexColor.b2_colorBox2DGreen;
                    }

                    b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
                }

                x += m_grid;
            }

            y += m_grid;
        }

        if (m_topDown)
        {
            b2World_RebuildStaticTree(m_worldId);
        }

        m_buildTime = b2GetMilliseconds(ticks);
        m_minTime = 1e6f;
    }

    static float CastCallback(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context)
    {
        CastResult result = context as CastResult;
        result.point = point;
        result.fraction = fraction;
        result.hit = true;
        return fraction;
    }


    static bool OverlapCallback(B2ShapeId shapeId, object context)
    {
        OverlapResult result = context as OverlapResult;
        if (result.count < 32)
        {
            B2AABB aabb = b2Shape_GetAABB(shapeId);
            result.points[result.count] = b2AABB_Center(aabb);
            result.count += 1;
        }

        return true;
    }


    public override void Step(Settings settings)
    {
        base.Step(settings);

        B2QueryFilter filter = b2DefaultQueryFilter();
        filter.maskBits = 1;
        hitCount = 0;
        nodeVisits = 0;
        leafVisits = 0;
        ms = 0.0f;
        sampleCount = (int)m_origins.Count;

        if (m_queryType == QueryType.e_rayCast)
        {
            ulong ticks = b2GetTicks();

            B2RayResult drawResult = new B2RayResult();

            for (int i = 0; i < sampleCount; ++i)
            {
                B2Vec2 origin = m_origins[i];
                B2Vec2 translation = m_translations[i];

                B2RayResult result = b2World_CastRayClosest(m_worldId, origin, translation, filter);

                if (i == m_drawIndex)
                {
                    drawResult = result;
                }

                nodeVisits += result.nodeVisits;
                leafVisits += result.leafVisits;
                hitCount += result.hit ? 1 : 0;
            }

            ms = b2GetMilliseconds(ticks);

            m_minTime = b2MinFloat(m_minTime, ms);

            B2Vec2 p1 = m_origins[m_drawIndex];
            B2Vec2 p2 = p1 + m_translations[m_drawIndex];
            m_context.draw.DrawSegment(p1, p2, B2HexColor.b2_colorWhite);
            m_context.draw.DrawPoint(p1, 5.0f, B2HexColor.b2_colorGreen);
            m_context.draw.DrawPoint(p2, 5.0f, B2HexColor.b2_colorRed);
            if (drawResult.hit)
            {
                m_context.draw.DrawPoint(drawResult.point, 5.0f, B2HexColor.b2_colorWhite);
            }
        }
        else if (m_queryType == QueryType.e_circleCast)
        {
            ulong ticks = b2GetTicks();

            CastResult drawResult = new CastResult();

            for (int i = 0; i < sampleCount; ++i)
            {
                B2Circle circle = new B2Circle(m_origins[i], m_radius);
                B2Vec2 translation = m_translations[i];

                CastResult result = new CastResult();
                B2TreeStats traversalResult = b2World_CastCircle(m_worldId, ref circle, translation, filter, CastCallback, result);

                if (i == m_drawIndex)
                {
                    drawResult = result;
                }

                nodeVisits += traversalResult.nodeVisits;
                leafVisits += traversalResult.leafVisits;
                hitCount += result.hit ? 1 : 0;
            }

            ms = b2GetMilliseconds(ticks);

            m_minTime = b2MinFloat(m_minTime, ms);

            B2Vec2 p1 = m_origins[m_drawIndex];
            B2Vec2 p2 = p1 + m_translations[m_drawIndex];
            m_context.draw.DrawSegment(p1, p2, B2HexColor.b2_colorWhite);
            m_context.draw.DrawPoint(p1, 5.0f, B2HexColor.b2_colorGreen);
            m_context.draw.DrawPoint(p2, 5.0f, B2HexColor.b2_colorRed);
            if (drawResult.hit)
            {
                B2Vec2 t = b2Lerp(p1, p2, drawResult.fraction);
                m_context.draw.DrawCircle(t, m_radius, B2HexColor.b2_colorWhite);
                m_context.draw.DrawPoint(drawResult.point, 5.0f, B2HexColor.b2_colorWhite);
            }
        }
        else if (m_queryType == QueryType.e_overlap)
        {
            ulong ticks = b2GetTicks();

            OverlapResult drawResult = new OverlapResult();
            B2Vec2 extent = new B2Vec2(m_radius, m_radius);
            OverlapResult result = new OverlapResult();

            for (int i = 0; i < sampleCount; ++i)
            {
                B2Vec2 origin = m_origins[i];
                B2AABB aabb = new B2AABB(origin - extent, origin + extent);

                result.count = 0;
                B2TreeStats traversalResult = b2World_OverlapAABB(m_worldId, aabb, filter, OverlapCallback, result);

                if (i == m_drawIndex)
                {
                    drawResult = result;
                }

                nodeVisits += traversalResult.nodeVisits;
                leafVisits += traversalResult.leafVisits;
                hitCount += result.count;
            }

            ms = b2GetMilliseconds(ticks);

            m_minTime = b2MinFloat(m_minTime, ms);

            {
                B2Vec2 origin = m_origins[m_drawIndex];
                B2AABB aabb = new B2AABB(origin - extent, origin + extent);

                m_context.draw.DrawAABB(aabb, B2HexColor.b2_colorWhite);
            }

            for (int i = 0; i < drawResult.count; ++i)
            {
                m_context.draw.DrawPoint(drawResult.points[i], 5.0f, B2HexColor.b2_colorHotPink);
            }
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float height = 240.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));

        ImGui.Begin("Cast", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        ImGui.PushItemWidth(100.0f);

        bool changed = false;

        string[] queryTypes = { "Ray", "Circle", "Overlap" };
        int queryType = (int)m_queryType;
        if (ImGui.Combo("Query", ref queryType, queryTypes, queryTypes.Length))
        {
            m_queryType = (QueryType)queryType;
            if (m_queryType == QueryType.e_overlap)
            {
                m_radius = 5.0f;
            }
            else
            {
                m_radius = 0.1f;
            }

            changed = true;
        }

        if (ImGui.SliderInt("rows", ref m_rowCount, 0, 1000, "%d"))
        {
            changed = true;
        }

        if (ImGui.SliderInt("columns", ref m_columnCount, 0, 1000, "%d"))
        {
            changed = true;
        }

        if (ImGui.SliderFloat("fill", ref m_fill, 0.0f, 1.0f, "%.2f"))
        {
            changed = true;
        }

        if (ImGui.SliderFloat("grid", ref m_grid, 0.5f, 2.0f, "%.2f"))
        {
            changed = true;
        }

        if (ImGui.SliderFloat("ratio", ref m_ratio, 1.0f, 10.0f, "%.2f"))
        {
            changed = true;
        }

        if (ImGui.Checkbox("top down", ref m_topDown))
        {
            changed = true;
        }

        if (ImGui.Button("Draw Next"))
        {
            m_drawIndex = (m_drawIndex + 1) % m_origins.Count;
        }

        ImGui.PopItemWidth();
        ImGui.End();

        if (changed)
        {
            BuildScene();
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        m_context.draw.DrawString(5, m_textLine, $"build time ms = {m_buildTime:g}");
        m_textLine += m_textIncrement;

        m_context.draw.DrawString(5, m_textLine, $"hit count = {hitCount}, node visits = {nodeVisits}, leaf visits = {leafVisits}");
        m_textLine += m_textIncrement;

        m_context.draw.DrawString(5, m_textLine, $"total ms = {ms:F3}");
        m_textLine += m_textIncrement;

        m_context.draw.DrawString(5, m_textLine, $"min total ms = {m_minTime:F3}");
        m_textLine += m_textIncrement;

        float aveRayCost = 1000.0f * m_minTime / (float)sampleCount;
        m_context.draw.DrawString(5, m_textLine, $"average us = {aveRayCost:F2}");
        m_textLine += m_textIncrement;
    }
}