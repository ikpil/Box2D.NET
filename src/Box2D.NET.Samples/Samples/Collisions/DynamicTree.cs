﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.math_function;
using static Box2D.NET.timer;
using static Box2D.NET.dynamic_tree;
using static Box2D.NET.Shared.random;
using static Box2D.NET.constants;


namespace Box2D.NET.Samples.Samples.Collisions;

// Tests the Box2D bounding volume hierarchy (BVH). The dynamic tree
// can be used independently as a spatial data structure.
public class DynamicTree : Sample
{
    b2DynamicTree m_tree;
    int m_rowCount, m_columnCount;
    Proxy[] m_proxies;
    int[] m_moveBuffer;
    int m_moveCount;
    int m_proxyCapacity;
    int m_proxyCount;
    int m_timeStamp;
    int m_updateType;
    float m_fill;
    float m_moveFraction;
    float m_moveDelta;
    float m_ratio;
    float m_grid;

    b2Vec2 m_startPoint;
    b2Vec2 m_endPoint;

    bool m_rayDrag;
    bool m_queryDrag;
    bool m_validate;

    static bool QueryCallback(int proxyId, int userData, object context)
    {
        DynamicTree sample = context as DynamicTree;
        Proxy proxy = sample.m_proxies[userData];
        Debug.Assert(proxy.proxyId == proxyId);
        proxy.queryStamp = sample.m_timeStamp;
        return true;
    }

    static float RayCallback(b2RayCastInput input, int proxyId, int userData, object context)
    {
        DynamicTree sample = context as DynamicTree;
        Proxy proxy = sample.m_proxies[userData];
        Debug.Assert(proxy.proxyId == proxyId);
        proxy.rayStamp = sample.m_timeStamp;
        return input.maxFraction;
    }

    static int sampleDynamicTree = RegisterSample("Collision", "Dynamic Tree", Create);

    static Sample Create(Settings settings)
    {
        return new DynamicTree(settings);
    }


    public DynamicTree(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(500.0f, 500.0f);
            Draw.g_camera.m_zoom = 25.0f * 21.0f;
        }

        m_fill = 0.25f;
        m_moveFraction = 0.05f;
        m_moveDelta = 0.1f;
        m_proxies = null;
        m_proxyCount = 0;
        m_proxyCapacity = 0;
        m_ratio = 5.0f;
        m_grid = 1.0f;

        m_moveBuffer = null;
        m_moveCount = 0;

        m_rowCount = g_sampleDebug ? 100 : 1000;
        m_columnCount = g_sampleDebug ? 100 : 1000;
        //memset( &m_tree, 0, sizeof( m_tree ) );
        m_tree = new b2DynamicTree();
        BuildTree();
        m_timeStamp = 0;
        m_updateType = (int)UpdateType.Update_Incremental;

        m_startPoint = new b2Vec2(0.0f, 0.0f);
        m_endPoint = new b2Vec2(0.0f, 0.0f);
        m_queryDrag = false;
        m_rayDrag = false;
        m_validate = true;
    }

    public void Release()
    {
        m_proxies = null;
        m_moveBuffer = null;
        b2DynamicTree_Destroy(m_tree);
        m_tree = null;
    }

    void BuildTree()
    {
        b2DynamicTree_Destroy(m_tree);
        m_proxies = null;
        m_moveBuffer = null;

        m_proxyCapacity = m_rowCount * m_columnCount;
        m_proxies = new Proxy[m_proxyCapacity];
        for (int i = 0; i < m_proxies.Length; ++i)
        {
            m_proxies[i] = new Proxy();
        }

        m_proxyCount = 0;

        m_moveBuffer = new int[m_proxyCapacity];
        m_moveCount = 0;

        float y = -4.0f;

        m_tree = b2DynamicTree_Create();

        b2Vec2 aabbMargin = new b2Vec2(0.1f, 0.1f);

        for (int i = 0; i < m_rowCount; ++i)
        {
            float x = -40.0f;

            for (int j = 0; j < m_columnCount; ++j)
            {
                float fillTest = RandomFloatRange(0.0f, 1.0f);
                if (fillTest <= m_fill)
                {
                    Debug.Assert(m_proxyCount <= m_proxyCapacity);
                    Proxy p = m_proxies[m_proxyCount];
                    p.position = new b2Vec2(x, y);

                    float ratio = RandomFloatRange(1.0f, m_ratio);
                    float width = RandomFloatRange(0.1f, 0.5f);
                    if (RandomFloat() > 0.0f)
                    {
                        p.width.x = ratio * width;
                        p.width.y = width;
                    }
                    else
                    {
                        p.width.x = width;
                        p.width.y = ratio * width;
                    }

                    p.box.lowerBound = new b2Vec2(x, y);
                    p.box.upperBound = new b2Vec2(x + p.width.x, y + p.width.y);
                    p.fatBox.lowerBound = b2Sub(p.box.lowerBound, aabbMargin);
                    p.fatBox.upperBound = b2Add(p.box.upperBound, aabbMargin);

                    p.proxyId = b2DynamicTree_CreateProxy(m_tree, p.fatBox, B2_DEFAULT_CATEGORY_BITS, m_proxyCount);
                    p.rayStamp = -1;
                    p.queryStamp = -1;
                    p.moved = false;
                    ++m_proxyCount;
                }

                x += m_grid;
            }

            y += m_grid;
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 320.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));

        ImGui.Begin("Dynamic Tree", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        ImGui.PushItemWidth(100.0f);

        bool changed = false;
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

        if (ImGui.SliderFloat("move", ref m_moveFraction, 0.0f, 1.0f, "%.2f"))
        {
        }

        if (ImGui.SliderFloat("delta", ref m_moveDelta, 0.0f, 1.0f, "%.2f"))
        {
        }

        if (ImGui.RadioButton("Incremental", m_updateType == (int)UpdateType.Update_Incremental))
        {
            m_updateType = (int)UpdateType.Update_Incremental;
            changed = true;
        }

        if (ImGui.RadioButton("Full Rebuild", m_updateType == (int)UpdateType.Update_FullRebuild))
        {
            m_updateType = (int)UpdateType.Update_FullRebuild;
            changed = true;
        }

        if (ImGui.RadioButton("Partial Rebuild", m_updateType == (int)UpdateType.Update_PartialRebuild))
        {
            m_updateType = (int)UpdateType.Update_PartialRebuild;
            changed = true;
        }

        ImGui.Separator();

        ImGui.Text("mouse button 1: ray cast");
        ImGui.Text("mouse button 1 + shift: query");

        ImGui.PopItemWidth();
        ImGui.End();

        if (changed)
        {
            BuildTree();
        }
    }

    public override void MouseDown(b2Vec2 p, int button, int mods)
    {
        if (button == (int)MouseButton.Left)
        {
            if (mods == 0 && m_queryDrag == false)
            {
                m_rayDrag = true;
                m_startPoint = p;
                m_endPoint = p;
            }
            else if (0 != (mods & (uint)Keys.ShiftLeft) && m_rayDrag == false)
            {
                m_queryDrag = true;
                m_startPoint = p;
                m_endPoint = p;
            }
        }
    }

    public override void MouseUp(b2Vec2 _, int button)
    {
        if (button == (int)MouseButton.Left)
        {
            m_queryDrag = false;
            m_rayDrag = false;
        }
    }

    public override void MouseMove(b2Vec2 p)
    {
        m_endPoint = p;
    }

    public override void Step(Settings settings)
    {
        if (m_queryDrag)
        {
            b2AABB box = new b2AABB(b2Min(m_startPoint, m_endPoint), b2Max(m_startPoint, m_endPoint));
            b2DynamicTree_Query(m_tree, box, B2_DEFAULT_MASK_BITS, QueryCallback, this);

            Draw.g_draw.DrawAABB(box, b2HexColor.b2_colorWhite);
        }

        // m_startPoint = {-1.0f, 0.5f};
        // m_endPoint = {7.0f, 0.5f};

        if (m_rayDrag)
        {
            b2RayCastInput input = new b2RayCastInput(m_startPoint, b2Sub(m_endPoint, m_startPoint), 1.0f);
            b2TreeStats result = b2DynamicTree_RayCast(m_tree, input, B2_DEFAULT_MASK_BITS, RayCallback, this);

            Draw.g_draw.DrawSegment(m_startPoint, m_endPoint, b2HexColor.b2_colorWhite);
            Draw.g_draw.DrawPoint(m_startPoint, 5.0f, b2HexColor.b2_colorGreen);
            Draw.g_draw.DrawPoint(m_endPoint, 5.0f, b2HexColor.b2_colorRed);

            Draw.g_draw.DrawString(5, m_textLine, "node visits = %d, leaf visits = %d", result.nodeVisits, result.leafVisits);
            m_textLine += m_textIncrement;
        }

        b2HexColor c = b2HexColor.b2_colorBlue;
        b2HexColor qc = b2HexColor.b2_colorGreen;

        b2Vec2 aabbMargin = new b2Vec2(0.1f, 0.1f);

        for (int i = 0; i < m_proxyCount; ++i)
        {
            Proxy p = m_proxies[i];

            if (p.queryStamp == m_timeStamp || p.rayStamp == m_timeStamp)
            {
                Draw.g_draw.DrawAABB(p.box, qc);
            }
            else
            {
                Draw.g_draw.DrawAABB(p.box, c);
            }

            float moveTest = RandomFloatRange(0.0f, 1.0f);
            if (m_moveFraction > moveTest)
            {
                float dx = m_moveDelta * RandomFloat();
                float dy = m_moveDelta * RandomFloat();

                p.position.x += dx;
                p.position.y += dy;

                p.box.lowerBound.x = p.position.x + dx;
                p.box.lowerBound.y = p.position.y + dy;
                p.box.upperBound.x = p.position.x + dx + p.width.x;
                p.box.upperBound.y = p.position.y + dy + p.width.y;

                if (b2AABB_Contains(p.fatBox, p.box) == false)
                {
                    p.fatBox.lowerBound = b2Sub(p.box.lowerBound, aabbMargin);
                    p.fatBox.upperBound = b2Add(p.box.upperBound, aabbMargin);
                    p.moved = true;
                }
                else
                {
                    p.moved = false;
                }
            }
            else
            {
                p.moved = false;
            }
        }

        switch ((UpdateType)m_updateType)
        {
            case UpdateType.Update_Incremental:
            {
                ulong ticks = b2GetTicks();
                for (int i = 0; i < m_proxyCount; ++i)
                {
                    Proxy p = m_proxies[i];
                    if (p.moved)
                    {
                        b2DynamicTree_MoveProxy(m_tree, p.proxyId, p.fatBox);
                    }
                }

                float ms = b2GetMilliseconds(ticks);
                Draw.g_draw.DrawString(5, m_textLine, "incremental : %.3f ms", ms);
                m_textLine += m_textIncrement;
            }
                break;

            case UpdateType.Update_FullRebuild:
            {
                for (int i = 0; i < m_proxyCount; ++i)
                {
                    Proxy p = m_proxies[i];
                    if (p.moved)
                    {
                        b2DynamicTree_EnlargeProxy(m_tree, p.proxyId, p.fatBox);
                    }
                }

                ulong ticks = b2GetTicks();
                int boxCount = b2DynamicTree_Rebuild(m_tree, true);
                float ms = b2GetMilliseconds(ticks);
                Draw.g_draw.DrawString(5, m_textLine, "full build %d : %.3f ms", boxCount, ms);
                m_textLine += m_textIncrement;
            }
                break;

            case UpdateType.Update_PartialRebuild:
            {
                for (int i = 0; i < m_proxyCount; ++i)
                {
                    Proxy p = m_proxies[i];
                    if (p.moved)
                    {
                        b2DynamicTree_EnlargeProxy(m_tree, p.proxyId, p.fatBox);
                    }
                }

                ulong ticks = b2GetTicks();
                int boxCount = b2DynamicTree_Rebuild(m_tree, false);
                float ms = b2GetMilliseconds(ticks);
                Draw.g_draw.DrawString(5, m_textLine, "partial rebuild %d : %.3f ms", boxCount, ms);
                m_textLine += m_textIncrement;
            }
                break;

            default:
                break;
        }

        int height = b2DynamicTree_GetHeight(m_tree);
        float areaRatio = b2DynamicTree_GetAreaRatio(m_tree);

        int hmin = (int)(MathF.Ceiling(MathF.Log((float)m_proxyCount) / MathF.Log(2.0f) - 1.0f));
        Draw.g_draw.DrawString(5, m_textLine, "proxies = %d, height = %d, hmin = %d, area ratio = %.1f", m_proxyCount, height, hmin,
            areaRatio);
        m_textLine += m_textIncrement;

        b2DynamicTree_Validate(m_tree);

        m_timeStamp += 1;
    }
}
