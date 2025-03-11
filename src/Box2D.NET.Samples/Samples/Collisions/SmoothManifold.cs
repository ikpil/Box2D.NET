// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Manifolds;

namespace Box2D.NET.Samples.Samples.Collisions;

public class SmoothManifold : Sample
{
    private static readonly int SampleSmoothManifoldIndex = SampleFactory.Shared.RegisterSample("Collision", "Smooth Manifold", Create);

    private enum ShapeType
    {
        e_circleShape = 0,
        e_boxShape
    }

    private ShapeType m_shapeType;

    private B2ChainSegment[] m_segments;
    private int m_count;

    private B2Transform m_transform;
    private float m_angle;
    private float m_round;

    private B2Vec2 m_basePosition;
    private B2Vec2 m_startPoint;
    private float m_baseAngle;

    private bool m_dragging;
    private bool m_rotating;
    private bool m_showIds;
    private bool m_showAnchors;
    private bool m_showSeparation;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new SmoothManifold(ctx, settings);
    }

    public SmoothManifold(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(2.0f, 20.0f);
            m_context.camera.m_zoom = 21.0f;
        }

        m_shapeType = ShapeType.e_boxShape;
        m_transform = new B2Transform(new B2Vec2(0.0f, 20.0f), b2Rot_identity);
        m_angle = 0.0f;
        m_round = 0.0f;

        m_startPoint = new B2Vec2(0.0f, 00.0f);
        m_basePosition = new B2Vec2(0.0f, 0.0f);
        m_baseAngle = 0.0f;

        m_dragging = false;
        m_rotating = false;
        m_showIds = false;
        m_showAnchors = false;
        m_showSeparation = false;

        // https://betravis.github.io/shape-tools/path-to-polygon/
        m_count = 36;

        B2Vec2[] points = new B2Vec2[36];
        points[0] = new B2Vec2(-20.58325f, 14.54175f);
        points[1] = new B2Vec2(-21.90625f, 15.8645f);
        points[2] = new B2Vec2(-24.552f, 17.1875f);
        points[3] = new B2Vec2(-27.198f, 11.89575f);
        points[4] = new B2Vec2(-29.84375f, 15.8645f);
        points[5] = new B2Vec2(-29.84375f, 21.15625f);
        points[6] = new B2Vec2(-25.875f, 23.802f);
        points[7] = new B2Vec2(-20.58325f, 25.125f);
        points[8] = new B2Vec2(-25.875f, 29.09375f);
        points[9] = new B2Vec2(-20.58325f, 31.7395f);
        points[10] = new B2Vec2(-11.0089998f, 23.2290001f);
        points[11] = new B2Vec2(-8.67700005f, 21.15625f);
        points[12] = new B2Vec2(-6.03125f, 21.15625f);
        points[13] = new B2Vec2(-7.35424995f, 29.09375f);
        points[14] = new B2Vec2(-3.38549995f, 29.09375f);
        points[15] = new B2Vec2(1.90625f, 30.41675f);
        points[16] = new B2Vec2(5.875f, 17.1875f);
        points[17] = new B2Vec2(11.16675f, 25.125f);
        points[18] = new B2Vec2(9.84375f, 29.09375f);
        points[19] = new B2Vec2(13.8125f, 31.7395f);
        points[20] = new B2Vec2(21.75f, 30.41675f);
        points[21] = new B2Vec2(28.3644981f, 26.448f);
        points[22] = new B2Vec2(25.71875f, 18.5105f);
        points[23] = new B2Vec2(24.3957481f, 13.21875f);
        points[24] = new B2Vec2(17.78125f, 11.89575f);
        points[25] = new B2Vec2(15.1355f, 7.92700005f);
        points[26] = new B2Vec2(5.875f, 9.25f);
        points[27] = new B2Vec2(1.90625f, 11.89575f);
        points[28] = new B2Vec2(-3.25f, 11.89575f);
        points[29] = new B2Vec2(-3.25f, 9.9375f);
        points[30] = new B2Vec2(-4.70825005f, 9.25f);
        points[31] = new B2Vec2(-8.67700005f, 9.25f);
        points[32] = new B2Vec2(-11.323f, 11.89575f);
        points[33] = new B2Vec2(-13.96875f, 11.89575f);
        points[34] = new B2Vec2(-15.29175f, 14.54175f);
        points[35] = new B2Vec2(-19.2605f, 14.54175f);

        m_segments = new B2ChainSegment[m_count];
        for (int i = 0; i < m_segments.Length; ++i)
        {
            m_segments[i] = new B2ChainSegment();
        }

        for (int i = 0; i < m_count; ++i)
        {
            int i0 = i > 0 ? i - 1 : m_count - 1;
            int i1 = i;
            int i2 = i1 < m_count - 1 ? i1 + 1 : 0;
            int i3 = i2 < m_count - 1 ? i2 + 1 : 0;

            B2Vec2 g1 = points[i0];
            B2Vec2 p1 = points[i1];
            B2Vec2 p2 = points[i2];
            B2Vec2 g2 = points[i3];

            // { g1, { p1, p2 }, g2, -1 };
            m_segments[i].ghost1 = g1;
            m_segments[i].segment.point1 = p1;
            m_segments[i].segment.point2 = p2;
            m_segments[i].ghost2 = g2;
            m_segments[i].chainId = -1;
        }
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 290.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(180.0f, height));

        ImGui.Begin("Smooth Manifold", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        ImGui.PushItemWidth(100.0f);

        {
            string[] shapeTypes = { "Circle", "Box" };
            int shapeType = (int)m_shapeType;
            ImGui.Combo("Shape", ref shapeType, shapeTypes, shapeTypes.Length);
            m_shapeType = (ShapeType)shapeType;
        }

        ImGui.SliderFloat("x Offset", ref m_transform.p.x, -2.0f, 2.0f, "%.2f");
        ImGui.SliderFloat("y Offset", ref m_transform.p.y, -2.0f, 2.0f, "%.2f");

        if (ImGui.SliderFloat("Angle", ref m_angle, -B2_PI, B2_PI, "%.2f"))
        {
            m_transform.q = b2MakeRot(m_angle);
        }

        ImGui.SliderFloat("Round", ref m_round, 0.0f, 0.4f, "%.1f");
        ImGui.Checkbox("Show Ids", ref m_showIds);
        ImGui.Checkbox("Show Separation", ref m_showSeparation);
        ImGui.Checkbox("Show Anchors", ref m_showAnchors);

        if (ImGui.Button("Reset"))
        {
            m_transform = b2Transform_identity;
            m_angle = 0.0f;
        }

        ImGui.Separator();

        ImGui.Text("mouse button 1: drag");
        ImGui.Text("mouse button 1 + shift: rotate");

        ImGui.PopItemWidth();
        ImGui.End();
    }

    public override void MouseDown(B2Vec2 p, MouseButton button, KeyModifiers mods)
    {
        if (button == (int)MouseButton.Left)
        {
            if (mods == 0 && m_rotating == false)
            {
                m_dragging = true;
                m_startPoint = p;
                m_basePosition = m_transform.p;
            }
            else if (0 != ((uint)mods & (uint)KeyModifiers.Shift) && m_dragging == false)
            {
                m_rotating = true;
                m_startPoint = p;
                m_baseAngle = m_angle;
            }
        }
    }

    public override void MouseUp(B2Vec2 p, MouseButton button)
    {
        if (button == (int)MouseButton.Left)
        {
            m_dragging = false;
            m_rotating = false;
        }
    }

    public override void MouseMove(B2Vec2 p)
    {
        if (m_dragging)
        {
            m_transform.p.x = m_basePosition.x + (p.x - m_startPoint.x);
            m_transform.p.y = m_basePosition.y + (p.y - m_startPoint.y);
        }
        else if (m_rotating)
        {
            float dx = p.x - m_startPoint.x;
            m_angle = b2ClampFloat(m_baseAngle + 1.0f * dx, -B2_PI, B2_PI);
            m_transform.q = b2MakeRot(m_angle);
        }
    }

    void DrawManifold(ref B2Manifold manifold)
    {
        for (int i = 0; i < manifold.pointCount; ++i)
        {
            ref B2ManifoldPoint mp = ref manifold.points[i];

            B2Vec2 p1 = mp.point;
            B2Vec2 p2 = b2MulAdd(p1, 0.5f, manifold.normal);
            m_context.g_draw.DrawSegment(p1, p2, B2HexColor.b2_colorWhite);

            if (m_showAnchors)
            {
                m_context.g_draw.DrawPoint(p1, 5.0f, B2HexColor.b2_colorGreen);
            }
            else
            {
                m_context.g_draw.DrawPoint(p1, 5.0f, B2HexColor.b2_colorGreen);
            }

            if (m_showIds)
            {
                // uint indexA = mp.id >> 8;
                // uint indexB = 0xFF & mp.id;
                B2Vec2 p = new B2Vec2(p1.x + 0.05f, p1.y - 0.02f);
                m_context.g_draw.DrawString(p, $"0x{mp.id:X4}");
            }

            if (m_showSeparation)
            {
                B2Vec2 p = new B2Vec2(p1.x + 0.05f, p1.y + 0.03f);
                m_context.g_draw.DrawString(p, $"{mp.separation:F3}");
            }
        }
    }

    public override void Step(Settings _)
    {
        B2HexColor color1 = B2HexColor.b2_colorYellow;
        B2HexColor color2 = B2HexColor.b2_colorMagenta;

        B2Transform transform1 = b2Transform_identity;
        B2Transform transform2 = m_transform;

        for (int i = 0; i < m_count; ++i)
        {
            B2ChainSegment segment = m_segments[i];
            B2Vec2 p1 = b2TransformPoint(ref transform1, segment.segment.point1);
            B2Vec2 p2 = b2TransformPoint(ref transform1, segment.segment.point2);
            m_context.g_draw.DrawSegment(p1, p2, color1);
            m_context.g_draw.DrawPoint(p1, 4.0f, color1);
        }

        // chain-segment vs circle
        if (m_shapeType == ShapeType.e_circleShape)
        {
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            m_context.g_draw.DrawSolidCircle(ref transform2, circle.center, circle.radius, color2);

            for (int i = 0; i < m_count; ++i)
            {
                B2ChainSegment segment = m_segments[i];
                B2Manifold m = b2CollideChainSegmentAndCircle(segment, transform1, circle, transform2);
                DrawManifold(ref m);
            }
        }
        else if (m_shapeType == ShapeType.e_boxShape)
        {
            float h = 0.5f - m_round;
            B2Polygon rox = b2MakeRoundedBox(h, h, m_round);
            m_context.g_draw.DrawSolidPolygon(ref transform2, rox.vertices.AsSpan(), rox.count, rox.radius, color2);

            for (int i = 0; i < m_count; ++i)
            {
                B2ChainSegment segment = m_segments[i];
                B2SimplexCache cache = new B2SimplexCache();
                B2Manifold m = b2CollideChainSegmentAndPolygon(segment, transform1, rox, transform2, ref cache);
                DrawManifold(ref m);
            }
        }
    }
}