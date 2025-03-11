// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Manifolds;
using static Box2D.NET.B2Collisions;
using MouseButton = Silk.NET.GLFW.MouseButton;

namespace Box2D.NET.Samples.Samples.Collisions;

// Tests manifolds and contact points
public class Manifold : Sample
{
    B2SimplexCache m_smgroxCache1;
    B2SimplexCache m_smgroxCache2;
    B2SimplexCache m_smgcapCache1;
    B2SimplexCache m_smgcapCache2;

    B2Hull m_wedge;

    B2Transform m_transform;
    float m_angle;
    float m_round;

    B2Vec2 m_basePosition;
    B2Vec2 m_startPoint;
    float m_baseAngle;

    bool m_dragging;
    bool m_rotating;
    bool m_showIds;
    bool m_showAnchors;
    bool m_showSeparation;
    bool m_enableCaching;

    private static readonly int SampleManifoldIndex = SampleFactory.Shared.RegisterSample("Collision", "Manifold", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new Manifold(ctx, settings);
    }

    public Manifold(SampleAppContext ctx, Settings settings)
        : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            // m_context.g_camera.m_center = {1.8f, 15.0f};
            m_context.g_camera.m_center = new B2Vec2(1.8f, 0.0f);
            m_context.g_camera.m_zoom = 25.0f * 0.45f;
        }

        m_smgroxCache1 = b2_emptySimplexCache;
        m_smgroxCache2 = b2_emptySimplexCache;
        m_smgcapCache1 = b2_emptySimplexCache;
        m_smgcapCache2 = b2_emptySimplexCache;

        m_transform = b2Transform_identity;
        m_transform.p.x = 1.0f;
        m_transform.p.y = 0.0f;
        // m_transform.q = b2MakeRot( 0.5f * b2_pi );
        m_angle = 0.0f;
        m_round = 0.1f;

        m_startPoint = new B2Vec2(0.0f, 0.0f);
        m_basePosition = new B2Vec2(0.0f, 0.0f);
        m_baseAngle = 0.0f;

        m_dragging = false;
        m_rotating = false;
        m_showIds = false;
        m_showSeparation = false;
        m_showAnchors = false;
        m_enableCaching = true;

        B2Vec2[] points = new B2Vec2[3] { new B2Vec2(-0.1f, -0.5f), new B2Vec2(0.1f, -0.5f), new B2Vec2(0.0f, 0.5f) };
        m_wedge = b2ComputeHull(points, 3);
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 300.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Manifold", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        ImGui.SliderFloat("x offset", ref m_transform.p.x, -2.0f, 2.0f, "%.2f");
        ImGui.SliderFloat("y offset", ref m_transform.p.y, -2.0f, 2.0f, "%.2f");

        if (ImGui.SliderFloat("angle", ref m_angle, -B2_PI, B2_PI, "%.2f"))
        {
            m_transform.q = b2MakeRot(m_angle);
        }

        ImGui.SliderFloat("round", ref m_round, 0.0f, 0.4f, "%.1f");
        ImGui.Checkbox("show ids", ref m_showIds);
        ImGui.Checkbox("show separation", ref m_showSeparation);
        ImGui.Checkbox("show anchors", ref m_showAnchors);
        ImGui.Checkbox("enable caching", ref m_enableCaching);

        if (ImGui.Button("Reset"))
        {
            m_transform = b2Transform_identity;
            m_angle = 0.0f;
        }

        ImGui.Separator();

        ImGui.Text("mouse button 1: drag");
        ImGui.Text("mouse button 1 + shift: rotate");

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
            m_transform.p.x = m_basePosition.x + 0.5f * (p.x - m_startPoint.x);
            m_transform.p.y = m_basePosition.y + 0.5f * (p.y - m_startPoint.y);
        }
        else if (m_rotating)
        {
            float dx = p.x - m_startPoint.x;
            m_angle = b2ClampFloat(m_baseAngle + 1.0f * dx, -B2_PI, B2_PI);
            m_transform.q = b2MakeRot(m_angle);
        }
    }

    void DrawManifold(ref B2Manifold manifold, B2Vec2 origin1, B2Vec2 origin2)
    {
        for (int i = 0; i < manifold.pointCount; ++i)
        {
            ref B2ManifoldPoint mp = ref manifold.points[i];

            B2Vec2 p1 = mp.point;
            B2Vec2 p2 = b2MulAdd(p1, 0.5f, manifold.normal);
            m_context.g_draw.DrawSegment(p1, p2, B2HexColor.b2_colorWhite);

            if (m_showAnchors)
            {
                m_context.g_draw.DrawPoint(b2Add(origin1, mp.anchorA), 5.0f, B2HexColor.b2_colorRed);
                m_context.g_draw.DrawPoint(b2Add(origin2, mp.anchorB), 5.0f, B2HexColor.b2_colorGreen);
            }
            else
            {
                m_context.g_draw.DrawPoint(p1, 10.0f, B2HexColor.b2_colorBlue);
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

    public override void Step(Settings settings)
    {
        B2Vec2 offset = new B2Vec2(-10.0f, -5.0f);
        B2Vec2 increment = new B2Vec2(4.0f, 0.0f);

        B2HexColor color1 = B2HexColor.b2_colorAquamarine;
        B2HexColor color2 = B2HexColor.b2_colorPaleGoldenRod;

        if (m_enableCaching == false)
        {
            m_smgroxCache1 = b2_emptySimplexCache;
            m_smgroxCache2 = b2_emptySimplexCache;
            m_smgcapCache1 = b2_emptySimplexCache;
            m_smgcapCache2 = b2_emptySimplexCache;
        }

        // circle-circle
        {
            B2Circle circle1 = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            B2Circle circle2 = new B2Circle(new B2Vec2(0.0f, 0.0f), 1.0f);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Manifold m = b2CollideCircles(circle1, transform1, circle2, transform2);

            m_context.g_draw.DrawSolidCircle(ref transform1, circle1.center, circle1.radius, color1);
            m_context.g_draw.DrawSolidCircle(ref transform2, circle2.center, circle2.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // capsule-circle
        {
            B2Capsule capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Manifold m = b2CollideCapsuleAndCircle(capsule, transform1, circle, transform2);

            B2Vec2 v1 = b2TransformPoint(ref transform1, capsule.center1);
            B2Vec2 v2 = b2TransformPoint(ref transform1, capsule.center2);
            m_context.g_draw.DrawSolidCapsule(v1, v2, capsule.radius, color1);

            m_context.g_draw.DrawSolidCircle(ref transform2, circle.center, circle.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // segment-circle
        {
            B2Segment segment = new B2Segment(new B2Vec2(-1.0f, 0.0f), new B2Vec2(1.0f, 0.0f));
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Manifold m = b2CollideSegmentAndCircle(segment, transform1, circle, transform2);

            B2Vec2 p1 = b2TransformPoint(ref transform1, segment.point1);
            B2Vec2 p2 = b2TransformPoint(ref transform1, segment.point2);
            m_context.g_draw.DrawSegment(p1, p2, color1);

            m_context.g_draw.DrawSolidCircle(ref transform2, circle.center, circle.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // box-circle
        {
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            B2Polygon box = b2MakeSquare(0.5f);
            box.radius = m_round;

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Manifold m = b2CollidePolygonAndCircle(box, transform1, circle, transform2);

            m_context.g_draw.DrawSolidPolygon(ref transform1, box.vertices.AsSpan(), box.count, m_round, color1);
            m_context.g_draw.DrawSolidCircle(ref transform2, circle.center, circle.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // capsule-capsule
        {
            B2Capsule capsule1 = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);
            B2Capsule capsule2 = new B2Capsule(new B2Vec2(0.25f, 0.0f), new B2Vec2(1.0f, 0.0f), 0.1f);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Manifold m = b2CollideCapsules(capsule1, transform1, capsule2, transform2);

            B2Vec2 v1 = b2TransformPoint(ref transform1, capsule1.center1);
            B2Vec2 v2 = b2TransformPoint(ref transform1, capsule1.center2);
            m_context.g_draw.DrawSolidCapsule(v1, v2, capsule1.radius, color1);

            v1 = b2TransformPoint(ref transform2, capsule2.center1);
            v2 = b2TransformPoint(ref transform2, capsule2.center2);
            m_context.g_draw.DrawSolidCapsule(v1, v2, capsule2.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // box-capsule
        {
            B2Capsule capsule = new B2Capsule(new B2Vec2(-0.4f, 0.0f), new B2Vec2(-0.1f, 0.0f), 0.1f);
            B2Polygon box = b2MakeOffsetBox(0.25f, 1.0f, new B2Vec2(1.0f, -1.0f), b2MakeRot(0.25f * B2_PI));

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Manifold m = b2CollidePolygonAndCapsule(box, transform1, capsule, transform2);

            m_context.g_draw.DrawSolidPolygon(ref transform1, box.vertices.AsSpan(), box.count, box.radius, color1);

            B2Vec2 v1 = b2TransformPoint(ref transform2, capsule.center1);
            B2Vec2 v2 = b2TransformPoint(ref transform2, capsule.center2);
            m_context.g_draw.DrawSolidCapsule(v1, v2, capsule.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // segment-capsule
        {
            B2Segment segment = new B2Segment(new B2Vec2(-1.0f, 0.0f), new B2Vec2(1.0f, 0.0f));
            B2Capsule capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Manifold m = b2CollideSegmentAndCapsule(segment, transform1, capsule, transform2);

            B2Vec2 p1 = b2TransformPoint(ref transform1, segment.point1);
            B2Vec2 p2 = b2TransformPoint(ref transform1, segment.point2);
            m_context.g_draw.DrawSegment(p1, p2, color1);

            p1 = b2TransformPoint(ref transform2, capsule.center1);
            p2 = b2TransformPoint(ref transform2, capsule.center2);
            m_context.g_draw.DrawSolidCapsule(p1, p2, capsule.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        offset = new B2Vec2(-10.0f, 0.0f);

        // square-square
        {
            B2Polygon box1 = b2MakeSquare(0.5f);
            B2Polygon box = b2MakeSquare(0.5f);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Manifold m = b2CollidePolygons(box1, transform1, box, transform2);

            m_context.g_draw.DrawSolidPolygon(ref transform1, box1.vertices.AsSpan(), box1.count, box1.radius, color1);
            m_context.g_draw.DrawSolidPolygon(ref transform2, box.vertices.AsSpan(), box.count, box.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // box-box
        {
            B2Polygon box1 = b2MakeBox(2.0f, 0.1f);
            B2Polygon box = b2MakeSquare(0.25f);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);
            // b2Transform transform2 = {b2Add({0.0f, -0.1f}, offset), {0.0f, 1.0f}};

            B2Manifold m = b2CollidePolygons(box1, transform1, box, transform2);

            m_context.g_draw.DrawSolidPolygon(ref transform1, box1.vertices.AsSpan(), box1.count, box1.radius, color1);
            m_context.g_draw.DrawSolidPolygon(ref transform2, box.vertices.AsSpan(), box.count, box.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // box-rox
        {
            B2Polygon box = b2MakeSquare(0.5f);
            float h = 0.5f - m_round;
            B2Polygon rox = b2MakeRoundedBox(h, h, m_round);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);
            // b2Transform transform2 = {b2Add({0.0f, -0.1f}, offset), {0.0f, 1.0f}};

            B2Manifold m = b2CollidePolygons(box, transform1, rox, transform2);

            m_context.g_draw.DrawSolidPolygon(ref transform1, box.vertices.AsSpan(), box.count, box.radius, color1);
            m_context.g_draw.DrawSolidPolygon(ref transform2, rox.vertices.AsSpan(), rox.count, rox.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // rox-rox
        {
            float h = 0.5f - m_round;
            B2Polygon rox = b2MakeRoundedBox(h, h, m_round);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);
            // b2Transform transform1 = {{6.48024225f, 2.07872653f}, {-0.938356698f, 0.345668465f}};
            // b2Transform transform2 = {{5.52862263f, 2.51146317f}, {-0.859374702f, -0.511346340f}};

            B2Manifold m = b2CollidePolygons(rox, transform1, rox, transform2);

            m_context.g_draw.DrawSolidPolygon(ref transform1, rox.vertices.AsSpan(), rox.count, rox.radius, color1);
            m_context.g_draw.DrawSolidPolygon(ref transform2, rox.vertices.AsSpan(), rox.count, rox.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // segment-rox
        {
            B2Segment segment = new B2Segment(new B2Vec2(-1.0f, 0.0f), new B2Vec2(1.0f, 0.0f));
            float h = 0.5f - m_round;
            B2Polygon rox = b2MakeRoundedBox(h, h, m_round);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);
            // b2Transform transform2 = {b2Add({-1.44583416f, 0.397352695f}, offset), m_transform.q};

            B2Manifold m = b2CollideSegmentAndPolygon(segment, transform1, rox, transform2);

            B2Vec2 p1 = b2TransformPoint(ref transform1, segment.point1);
            B2Vec2 p2 = b2TransformPoint(ref transform1, segment.point2);
            m_context.g_draw.DrawSegment(p1, p2, color1);
            m_context.g_draw.DrawSolidPolygon(ref transform2, rox.vertices.AsSpan(), rox.count, rox.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // wox-wox
        {
            B2Polygon wox = b2MakePolygon(m_wedge, m_round);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);
            // b2Transform transform2 = {b2Add({0.0f, -0.1f}, offset), {0.0f, 1.0f}};

            B2Manifold m = b2CollidePolygons(wox, transform1, wox, transform2);

            m_context.g_draw.DrawSolidPolygon(ref transform1, wox.vertices.AsSpan(), wox.count, wox.radius, color1);
            m_context.g_draw.DrawSolidPolygon(ref transform1, wox.vertices.AsSpan(), wox.count, 0.0f, color1);
            m_context.g_draw.DrawSolidPolygon(ref transform2, wox.vertices.AsSpan(), wox.count, wox.radius, color2);
            m_context.g_draw.DrawSolidPolygon(ref transform2, wox.vertices.AsSpan(), wox.count, 0.0f, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // wox-wox
        {
            B2Vec2[] p1s = new B2Vec2[3] { new B2Vec2(0.175740838f, 0.224936664f), new B2Vec2(-0.301293969f, 0.194021404f), new B2Vec2(-0.105151534f, -0.432157338f) };
            B2Vec2[] p2s = new B2Vec2[3] { new B2Vec2(-0.427884758f, -0.225028217f), new B2Vec2(0.0566576123f, -0.128772855f), new B2Vec2(0.176625848f, 0.338923335f) };

            B2Hull h1 = b2ComputeHull(p1s, 3);
            B2Hull h2 = b2ComputeHull(p2s, 3);
            B2Polygon w1 = b2MakePolygon(h1, 0.158798501f);
            B2Polygon w2 = b2MakePolygon(h2, 0.205900759f);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);
            // b2Transform transform2 = {b2Add({0.0f, -0.1f}, offset), {0.0f, 1.0f}};

            B2Manifold m = b2CollidePolygons(w1, transform1, w2, transform2);

            m_context.g_draw.DrawSolidPolygon(ref transform1, w1.vertices.AsSpan(), w1.count, w1.radius, color1);
            m_context.g_draw.DrawSolidPolygon(ref transform1, w1.vertices.AsSpan(), w1.count, 0.0f, color1);
            m_context.g_draw.DrawSolidPolygon(ref transform2, w2.vertices.AsSpan(), w2.count, w2.radius, color2);
            m_context.g_draw.DrawSolidPolygon(ref transform2, w2.vertices.AsSpan(), w2.count, 0.0f, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        offset = new B2Vec2(-10.0f, 5.0f);

        // box-triangle
        {
            B2Polygon box = b2MakeBox(1.0f, 1.0f);
            B2Vec2[] points = new B2Vec2[3] { new B2Vec2(-0.05f, 0.0f), new B2Vec2(0.05f, 0.0f), new B2Vec2(0.0f, 0.1f) };
            B2Hull hull = b2ComputeHull(points, 3);
            B2Polygon tri = b2MakePolygon(hull, 0.0f);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);
            // b2Transform transform2 = {b2Add({0.0f, -0.1f}, offset), {0.0f, 1.0f}};

            B2Manifold m = b2CollidePolygons(box, transform1, tri, transform2);

            m_context.g_draw.DrawSolidPolygon(ref transform1, box.vertices.AsSpan(), box.count, 0.0f, color1);
            m_context.g_draw.DrawSolidPolygon(ref transform2, tri.vertices.AsSpan(), tri.count, 0.0f, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset = b2Add(offset, increment);
        }

        // chain-segment vs circle
        {
            B2ChainSegment segment = new B2ChainSegment();
            segment.ghost1 = new B2Vec2(2.0f, 1.0f);
            segment.segment.point1 = new B2Vec2(1.0f, 1.0f);
            segment.segment.point2 = new B2Vec2(-1.0f, 0.0f);
            segment.ghost2 = new B2Vec2(-2.0f, 0.0f);
            segment.chainId = -1;
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Manifold m = b2CollideChainSegmentAndCircle(segment, transform1, circle, transform2);

            B2Vec2 g1 = b2TransformPoint(ref transform1, segment.ghost1);
            B2Vec2 g2 = b2TransformPoint(ref transform1, segment.ghost2);
            B2Vec2 p1 = b2TransformPoint(ref transform1, segment.segment.point1);
            B2Vec2 p2 = b2TransformPoint(ref transform1, segment.segment.point2);
            m_context.g_draw.DrawSegment(g1, p1, B2HexColor.b2_colorLightGray);
            m_context.g_draw.DrawSegment(p1, p2, color1);
            m_context.g_draw.DrawSegment(p2, g2, B2HexColor.b2_colorLightGray);
            m_context.g_draw.DrawSolidCircle(ref transform2, circle.center, circle.radius, color2);

            DrawManifold(ref m, transform1.p, transform2.p);

            offset.x += 2.0f * increment.x;
        }

        // chain-segment vs rounded polygon
        {
            B2ChainSegment segment1 = new B2ChainSegment();
            segment1.ghost1 = new B2Vec2(2.0f, 1.0f);
            segment1.segment.point1 = new B2Vec2(1.0f, 1.0f);
            segment1.segment.point2 = new B2Vec2(-1.0f, 0.0f);
            segment1.ghost2 = new B2Vec2(-2.0f, 0.0f);
            segment1.chainId = -1;

            B2ChainSegment segment2 = new B2ChainSegment();
            segment2.ghost1 = new B2Vec2(3.0f, 1.0f);
            segment2.segment.point1 = new B2Vec2(2.0f, 1.0f);
            segment2.segment.point2 = new B2Vec2(1.0f, 1.0f);
            segment2.ghost2 = new B2Vec2(-1.0f, 0.0f);
            segment2.chainId = -1;

            // b2ChainSegment segment1 = {{2.0f, 0.0f}, {{1.0f, 0.0f}, {-1.0f, 0.0f}}, {-2.0f, 0.0f}, -1};
            // b2ChainSegment segment2 = {{3.0f, 0.0f}, {{2.0f, 0.0f}, {1.0f, 0.0f}}, {-1.0f, 0.0f}, -1};
            // b2ChainSegment segment1 = {{0.5f, 1.0f}, {{0.0f, 2.0f}, {-0.5f, 1.0f}}, {-1.0f, 0.0f}, -1};
            // b2ChainSegment segment2 = {{1.0f, 0.0f}, {{0.5f, 1.0f}, {0.0f, 2.0f}}, {-0.5f, 1.0f}, -1};
            float h = 0.5f - m_round;
            B2Polygon rox = b2MakeRoundedBox(h, h, m_round);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Manifold m1 = b2CollideChainSegmentAndPolygon(segment1, transform1, rox, transform2, ref m_smgroxCache1);
            B2Manifold m2 = b2CollideChainSegmentAndPolygon(segment2, transform1, rox, transform2, ref m_smgroxCache2);

            {
                B2Vec2 g2 = b2TransformPoint(ref transform1, segment1.ghost2);
                B2Vec2 p1 = b2TransformPoint(ref transform1, segment1.segment.point1);
                B2Vec2 p2 = b2TransformPoint(ref transform1, segment1.segment.point2);
                m_context.g_draw.DrawSegment(p1, p2, color1);
                m_context.g_draw.DrawPoint(p1, 4.0f, color1);
                m_context.g_draw.DrawPoint(p2, 4.0f, color1);
                m_context.g_draw.DrawSegment(p2, g2, B2HexColor.b2_colorLightGray);
            }

            {
                B2Vec2 g1 = b2TransformPoint(ref transform1, segment2.ghost1);
                B2Vec2 p1 = b2TransformPoint(ref transform1, segment2.segment.point1);
                B2Vec2 p2 = b2TransformPoint(ref transform1, segment2.segment.point2);
                m_context.g_draw.DrawSegment(g1, p1, B2HexColor.b2_colorLightGray);
                m_context.g_draw.DrawSegment(p1, p2, color1);
                m_context.g_draw.DrawPoint(p1, 4.0f, color1);
                m_context.g_draw.DrawPoint(p2, 4.0f, color1);
            }

            m_context.g_draw.DrawSolidPolygon(ref transform2, rox.vertices.AsSpan(), rox.count, rox.radius, color2);
            m_context.g_draw.DrawPoint(b2TransformPoint(ref transform2, rox.centroid), 5.0f, B2HexColor.b2_colorGainsboro);

            DrawManifold(ref m1, transform1.p, transform2.p);
            DrawManifold(ref m2, transform1.p, transform2.p);

            offset.x += 2.0f * increment.x;
        }

        // chain-segment vs capsule
        {
            //{ { 2.0f, 1.0f }, { { 1.0f, 1.0f }, { -1.0f, 0.0f } }, { -2.0f, 0.0f }, -1 };
            B2ChainSegment segment1 = new B2ChainSegment();
            segment1.ghost1 = new B2Vec2(2.0f, 1.0f);
            segment1.segment.point1 = new B2Vec2(1.0f, 1.0f);
            segment1.segment.point2 = new B2Vec2(-1.0f, 0.0f);
            segment1.ghost2 = new B2Vec2(-2.0f, 0.0f);
            segment1.chainId = -1;

            // { { 3.0f, 1.0f }, { { 2.0f, 1.0f }, { 1.0f, 1.0f } }, { -1.0f, 0.0f }, -1 };
            B2ChainSegment segment2 = new B2ChainSegment();
            segment2.ghost1 = new B2Vec2(3.0f, 1.0f);
            segment2.segment.point1 = new B2Vec2(2.0f, 1.0f);
            segment2.segment.point2 = new B2Vec2(1.0f, 0.0f);
            segment2.ghost2 = new B2Vec2(-1.0f, 0.0f);
            segment2.chainId = -1;

            B2Capsule capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);

            B2Transform transform1 = new B2Transform(offset, b2Rot_identity);
            B2Transform transform2 = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Manifold m1 = b2CollideChainSegmentAndCapsule(segment1, transform1, capsule, transform2, ref m_smgcapCache1);
            B2Manifold m2 = b2CollideChainSegmentAndCapsule(segment2, transform1, capsule, transform2, ref m_smgcapCache2);

            {
                B2Vec2 g2 = b2TransformPoint(ref transform1, segment1.ghost2);
                B2Vec2 p1 = b2TransformPoint(ref transform1, segment1.segment.point1);
                B2Vec2 p2 = b2TransformPoint(ref transform1, segment1.segment.point2);
                // m_context.g_draw.DrawSegment(g1, p1, b2HexColor.b2_colorLightGray);
                m_context.g_draw.DrawSegment(p1, p2, color1);
                m_context.g_draw.DrawPoint(p1, 4.0f, color1);
                m_context.g_draw.DrawPoint(p2, 4.0f, color1);
                m_context.g_draw.DrawSegment(p2, g2, B2HexColor.b2_colorLightGray);
            }

            {
                B2Vec2 g1 = b2TransformPoint(ref transform1, segment2.ghost1);
                B2Vec2 p1 = b2TransformPoint(ref transform1, segment2.segment.point1);
                B2Vec2 p2 = b2TransformPoint(ref transform1, segment2.segment.point2);
                m_context.g_draw.DrawSegment(g1, p1, B2HexColor.b2_colorLightGray);
                m_context.g_draw.DrawSegment(p1, p2, color1);
                m_context.g_draw.DrawPoint(p1, 4.0f, color1);
                m_context.g_draw.DrawPoint(p2, 4.0f, color1);
                // m_context.g_draw.DrawSegment(p2, g2, b2HexColor.b2_colorLightGray);
            }

            {
                B2Vec2 p1 = b2TransformPoint(ref transform2, capsule.center1);
                B2Vec2 p2 = b2TransformPoint(ref transform2, capsule.center2);
                m_context.g_draw.DrawSolidCapsule(p1, p2, capsule.radius, color2);

                m_context.g_draw.DrawPoint(b2Lerp(p1, p2, 0.5f), 5.0f, B2HexColor.b2_colorGainsboro);
            }

            DrawManifold(ref m1, transform1.p, transform2.p);
            DrawManifold(ref m2, transform1.p, transform2.p);

            offset.x += 2.0f * increment.x;
        }
    }
}
