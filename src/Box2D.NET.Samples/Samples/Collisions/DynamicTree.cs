// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.Shared.RandomSupports;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples.Collisions;

// Tests the Box2D bounding volume hierarchy (BVH). The dynamic tree
// can be used independently as a spatial data structure.
public class DynamicTree : Sample
{
    private static readonly int SampleDynamicTree = SampleFactory.Shared.RegisterSample("Collision", "Dynamic Tree", Create);

    private B2DynamicTree m_tree;
    private int m_rowCount, m_columnCount;
    private Proxy[] m_proxies;
    private int[] m_moveBuffer;
    private int m_moveCount;
    private int m_proxyCapacity;
    private int m_proxyCount;
    private int m_timeStamp;
    private int m_updateType;
    private float m_fill;
    private float m_moveFraction;
    private float m_moveDelta;
    private float m_ratio;
    private float m_grid;

    private B2Vec2 m_startPoint;
    private B2Vec2 m_endPoint;

    private bool m_rayDrag;
    private bool m_queryDrag;
    private bool m_validate;

    //
    private float _ms;
    private float _boxCount;

    static bool QueryCallback(int proxyId, ulong userData, ref DynamicTreeContext context)
    {
        ref DynamicTreeContext sample = ref context;
        Proxy proxy = sample.tree.m_proxies[userData];
        B2_ASSERT(proxy.proxyId == proxyId);
        proxy.queryStamp = sample.tree.m_timeStamp;
        return true;
    }

    static float RayCallback(ref B2RayCastInput input, int proxyId, ulong userData, ref DynamicTreeContext context)
    {
        DynamicTree sample = context.tree;
        Proxy proxy = sample.m_proxies[userData];
        B2_ASSERT(proxy.proxyId == proxyId);
        proxy.rayStamp = sample.m_timeStamp;
        return input.maxFraction;
    }


    private static Sample Create(SampleContext context)
    {
        return new DynamicTree(context);
    }


    public DynamicTree(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(500.0f, 500.0f);
            m_camera.m_zoom = 25.0f * 21.0f;
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

        m_rowCount = m_isDebug ? 100 : 1000;
        m_columnCount = m_isDebug ? 100 : 1000;
        //memset( &m_tree, 0, sizeof( m_tree ) );
        m_tree = new B2DynamicTree();
        BuildTree();
        m_timeStamp = 0;
        m_updateType = (int)UpdateType.Update_Incremental;

        m_startPoint = new B2Vec2(0.0f, 0.0f);
        m_endPoint = new B2Vec2(0.0f, 0.0f);
        m_queryDrag = false;
        m_rayDrag = false;
        m_validate = true;
    }

    public override void Dispose()
    {
        m_proxies = null;
        m_moveBuffer = null;
        b2DynamicTree_Destroy(m_tree);
        m_tree = null;

        base.Dispose();
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

        B2Vec2 aabbMargin = new B2Vec2(0.1f, 0.1f);

        for (int i = 0; i < m_rowCount; ++i)
        {
            float x = -40.0f;

            for (int j = 0; j < m_columnCount; ++j)
            {
                float fillTest = RandomFloatRange(0.0f, 1.0f);
                if (fillTest <= m_fill)
                {
                    B2_ASSERT(m_proxyCount <= m_proxyCapacity);
                    Proxy p = m_proxies[m_proxyCount];
                    p.position = new B2Vec2(x, y);

                    float ratio = RandomFloatRange(1.0f, m_ratio);
                    float width = RandomFloatRange(0.1f, 0.5f);
                    if (RandomFloat() > 0.0f)
                    {
                        p.width.X = ratio * width;
                        p.width.Y = width;
                    }
                    else
                    {
                        p.width.X = width;
                        p.width.Y = ratio * width;
                    }

                    p.box.lowerBound = new B2Vec2(x, y);
                    p.box.upperBound = new B2Vec2(x + p.width.X, y + p.width.Y);
                    p.fatBox.lowerBound = b2Sub(p.box.lowerBound, aabbMargin);
                    p.fatBox.upperBound = b2Add(p.box.upperBound, aabbMargin);

                    p.proxyId = b2DynamicTree_CreateProxy(m_tree, p.fatBox, B2_DEFAULT_CATEGORY_BITS, (ulong)m_proxyCount);
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

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 320.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.m_height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));

        ImGui.Begin("Dynamic Tree", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

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

    public override void MouseDown(B2Vec2 p, MouseButton button, KeyModifiers mods)
    {
        if (button == (int)MouseButton.Left)
        {
            if (mods == 0 && m_queryDrag == false)
            {
                m_rayDrag = true;
                m_startPoint = p;
                m_endPoint = p;
            }
            else if (0 != ((uint)mods & (uint)KeyModifiers.Shift) && m_rayDrag == false)
            {
                m_queryDrag = true;
                m_startPoint = p;
                m_endPoint = p;
            }
        }
    }

    public override void MouseUp(B2Vec2 p, MouseButton button)
    {
        if (button == (int)MouseButton.Left)
        {
            m_queryDrag = false;
            m_rayDrag = false;
        }
    }

    public override void MouseMove(B2Vec2 p)
    {
        m_endPoint = p;
    }


    public override void Step()
    {
        // m_startPoint = {-1.0f, 0.5f};
        // m_endPoint = {7.0f, 0.5f};

        B2HexColor c = B2HexColor.b2_colorBlue;
        B2HexColor qc = B2HexColor.b2_colorGreen;

        B2Vec2 aabbMargin = new B2Vec2(0.1f, 0.1f);

        for (int i = 0; i < m_proxyCount; ++i)
        {
            Proxy p = m_proxies[i];

            if (p.queryStamp == m_timeStamp || p.rayStamp == m_timeStamp)
            {
                m_draw.DrawBounds(p.box, qc);
            }
            else
            {
                m_draw.DrawBounds(p.box, c);
            }

            float moveTest = RandomFloatRange(0.0f, 1.0f);
            if (m_moveFraction > moveTest)
            {
                float dx = m_moveDelta * RandomFloat();
                float dy = m_moveDelta * RandomFloat();

                p.position.X += dx;
                p.position.Y += dy;

                p.box.lowerBound.X = p.position.X + dx;
                p.box.lowerBound.Y = p.position.Y + dy;
                p.box.upperBound.X = p.position.X + dx + p.width.X;
                p.box.upperBound.Y = p.position.Y + dy + p.width.Y;

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

                _ms = b2GetMilliseconds(ticks);
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
                _boxCount = b2DynamicTree_Rebuild(m_tree, true);
                _ms = b2GetMilliseconds(ticks);
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
                _boxCount = b2DynamicTree_Rebuild(m_tree, false);
                _ms = b2GetMilliseconds(ticks);
            }
                break;

            default:
                break;
        }


        b2DynamicTree_Validate(m_tree);

        m_timeStamp += 1;
    }


    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        if (m_queryDrag)
        {
            var dynamicTreeContext = new DynamicTreeContext();
            dynamicTreeContext.tree = this;

            B2AABB box = new B2AABB(b2Min(m_startPoint, m_endPoint), b2Max(m_startPoint, m_endPoint));

            b2DynamicTree_Query(m_tree, box, B2_DEFAULT_MASK_BITS, QueryCallback, ref dynamicTreeContext);

            m_draw.DrawBounds(box, B2HexColor.b2_colorWhite);
        }

        // m_startPoint = {-1.0f, 0.5f};
        // m_endPoint = {7.0f, 0.5f};

        if (m_rayDrag)
        {
            var dynamicTreeContext = new DynamicTreeContext();
            dynamicTreeContext.tree = this;

            B2RayCastInput input = new B2RayCastInput(m_startPoint, b2Sub(m_endPoint, m_startPoint), 1.0f);
            B2TreeStats result = b2DynamicTree_RayCast(m_tree, ref input, B2_DEFAULT_MASK_BITS, RayCallback, ref dynamicTreeContext);

            m_draw.DrawLine(m_startPoint, m_endPoint, B2HexColor.b2_colorWhite);
            m_draw.DrawPoint(m_startPoint, 5.0f, B2HexColor.b2_colorGreen);
            m_draw.DrawPoint(m_endPoint, 5.0f, B2HexColor.b2_colorRed);

            DrawTextLine($"node visits = {result.nodeVisits}, leaf visits = {result.leafVisits}");
        }

        switch ((UpdateType)m_updateType)
        {
            case UpdateType.Update_Incremental:
            {
                DrawTextLine($"incremental : {_ms:F3} ms");
            }
                break;

            case UpdateType.Update_FullRebuild:
            {
                DrawTextLine($"full build {_boxCount} : {_ms:F3} ms");
            }
                break;

            case UpdateType.Update_PartialRebuild:
            {
                DrawTextLine($"partial rebuild {_boxCount} : {_ms:F3} ms");
            }
                break;

            default:
                break;
        }

        //
        int height = b2DynamicTree_GetHeight(m_tree);
        float areaRatio = b2DynamicTree_GetAreaRatio(m_tree);

        int hmin = (int)(MathF.Ceiling(MathF.Log((float)m_proxyCount) / MathF.Log(2.0f) - 1.0f));
        DrawTextLine($"proxies = {m_proxyCount}, height = {height}, hmin = {hmin}, area ratio = {areaRatio:F1}");
    }
}