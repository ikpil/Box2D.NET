// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Samples.Collisions;

public class RayCast : Sample
{
    private static readonly int SampleIndex = SampleFactory.Shared.RegisterSample("Collision", "Ray Cast", Create);

    private B2Polygon m_box;
    private B2Polygon m_triangle;
    private B2Circle m_circle;
    private B2Capsule m_capsule;
    private B2Segment m_segment;

    private B2Transform m_transform;
    private float m_angle;

    private B2Vec2 m_rayStart;
    private B2Vec2 m_rayEnd;

    private B2Vec2 m_basePosition;
    private float m_baseAngle;

    private B2Vec2 m_startPosition;

    private bool m_rayDrag;
    private bool m_translating;
    private bool m_rotating;
    private bool m_showFraction;


    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new RayCast(ctx, settings);
    }

    public RayCast(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 20.0f);
            m_context.camera.m_zoom = 17.5f;
        }

        m_circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 2.0f);
        m_capsule = new B2Capsule(new B2Vec2(-1.0f, 1.0f), new B2Vec2(1.0f, -1.0f), 1.5f);
        m_box = b2MakeBox(2.0f, 2.0f);

        B2Vec2[] vertices = new B2Vec2[3] { new B2Vec2(-2.0f, 0.0f), new B2Vec2(2.0f, 0.0f), new B2Vec2(2.0f, 3.0f) };
        B2Hull hull = b2ComputeHull(vertices, 3);
        m_triangle = b2MakePolygon(ref hull, 0.0f);

        m_segment = new B2Segment(new B2Vec2(-3.0f, 0.0f), new B2Vec2(3.0f, 0.0f));

        m_transform = b2Transform_identity;
        m_angle = 0.0f;

        m_basePosition = new B2Vec2(0.0f, 0.0f);
        m_baseAngle = 0.0f;
        m_startPosition = new B2Vec2(0.0f, 0.0f);

        m_rayStart = new B2Vec2(0.0f, 30.0f);
        m_rayEnd = new B2Vec2(0.0f, 0.0f);

        m_rayDrag = false;
        m_translating = false;
        m_rotating = false;

        m_showFraction = false;
    }

    public override void UpdateUI()
    {
        base.UpdateUI();

        float height = 230.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));

        ImGui.Begin("Ray-cast", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        ImGui.PushItemWidth(100.0f);

        ImGui.SliderFloat("x offset", ref m_transform.p.X, -2.0f, 2.0f, "%.2f");
        ImGui.SliderFloat("y offset", ref m_transform.p.Y, -2.0f, 2.0f, "%.2f");

        if (ImGui.SliderFloat("angle", ref m_angle, -B2_PI, B2_PI, "%.2f"))
        {
            m_transform.q = b2MakeRot(m_angle);
        }

        // if (ImGui.SliderFloat("ray radius", &m_rayRadius, 0.0f, 1.0f, "%.1f"))
        //{
        // }

        ImGui.Checkbox("show fraction", ref m_showFraction);

        if (ImGui.Button("Reset"))
        {
            m_transform = b2Transform_identity;
            m_angle = 0.0f;
        }

        ImGui.Separator();

        ImGui.Text("mouse btn 1: ray cast");
        ImGui.Text("mouse btn 1 + shft: translate");
        ImGui.Text("mouse btn 1 + ctrl: rotate");

        ImGui.PopItemWidth();

        ImGui.End();
    }

    public override void MouseDown(B2Vec2 p, MouseButton button, KeyModifiers mods)
    {
        if (button == (int)MouseButton.Left)
        {
            m_startPosition = p;

            if (mods == 0)
            {
                m_rayStart = p;
                m_rayDrag = true;
            }
            else if (0 != ((uint)mods & (uint)KeyModifiers.Shift))
            {
                m_translating = true;
                m_basePosition = m_transform.p;
            }
            else if (0 != ((uint)mods & (uint)KeyModifiers.Control))
            {
                m_rotating = true;
                m_baseAngle = m_angle;
            }
        }
    }

    public override void MouseUp(B2Vec2 p, MouseButton button)
    {
        if (button == (int)MouseButton.Left)
        {
            m_rayDrag = false;
            m_rotating = false;
            m_translating = false;
        }
    }

    public override void MouseMove(B2Vec2 p)
    {
        if (m_rayDrag)
        {
            m_rayEnd = p;
        }
        else if (m_translating)
        {
            m_transform.p.X = m_basePosition.X + 0.5f * (p.X - m_startPosition.X);
            m_transform.p.Y = m_basePosition.Y + 0.5f * (p.Y - m_startPosition.Y);
        }
        else if (m_rotating)
        {
            float dx = p.X - m_startPosition.X;
            m_angle = b2ClampFloat(m_baseAngle + 0.5f * dx, -B2_PI, B2_PI);
            m_transform.q = b2MakeRot(m_angle);
        }
    }

    void DrawRay(ref B2CastOutput output)
    {
        B2Vec2 p1 = m_rayStart;
        B2Vec2 p2 = m_rayEnd;
        B2Vec2 d = b2Sub(p2, p1);

        if (output.hit)
        {
            B2Vec2 p = b2MulAdd(p1, output.fraction, d);
            m_context.draw.DrawSegment(p1, p, B2HexColor.b2_colorWhite);
            m_context.draw.DrawPoint(p1, 5.0f, B2HexColor.b2_colorGreen);
            m_context.draw.DrawPoint(output.point, 5.0f, B2HexColor.b2_colorWhite);

            B2Vec2 n = b2MulAdd(p, 1.0f, output.normal);
            m_context.draw.DrawSegment(p, n, B2HexColor.b2_colorViolet);

            // if (m_rayRadius > 0.0f)
            //{
            //	m_context.g_draw.DrawCircle(p1, m_rayRadius, b2HexColor.b2_colorGreen);
            //	m_context.g_draw.DrawCircle(p, m_rayRadius, b2HexColor.b2_colorRed);
            // }

            if (m_showFraction)
            {
                B2Vec2 ps = new B2Vec2(p.X + 0.05f, p.Y - 0.02f);
                m_context.draw.DrawString(ps, $"{output.fraction:F2}");
            }
        }
        else
        {
            m_context.draw.DrawSegment(p1, p2, B2HexColor.b2_colorWhite);
            m_context.draw.DrawPoint(p1, 5.0f, B2HexColor.b2_colorGreen);
            m_context.draw.DrawPoint(p2, 5.0f, B2HexColor.b2_colorRed);

            // if (m_rayRadius > 0.0f)
            //{
            //	m_context.g_draw.DrawCircle(p1, m_rayRadius, b2HexColor.b2_colorGreen);
            //	m_context.g_draw.DrawCircle(p2, m_rayRadius, b2HexColor.b2_colorRed);
            // }
        }
    }

    public override void Step(Settings _)
    {
        B2Vec2 offset = new B2Vec2(-20.0f, 20.0f);
        B2Vec2 increment = new B2Vec2(10.0f, 0.0f);

        B2HexColor color1 = B2HexColor.b2_colorYellow;

        B2CastOutput output = new B2CastOutput();
        float maxFraction = 1.0f;

        // circle
        {
            B2Transform transform = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);
            m_context.draw.DrawSolidCircle(ref transform, m_circle.center, m_circle.radius, color1);

            B2Vec2 start = b2InvTransformPoint(transform, m_rayStart);
            B2Vec2 translation = b2InvRotateVector(transform.q, b2Sub(m_rayEnd, m_rayStart));
            B2RayCastInput input = new B2RayCastInput(start, translation, maxFraction);

            B2CastOutput localOutput = b2RayCastCircle(ref input, m_circle);
            if (localOutput.hit)
            {
                output = localOutput;
                output.point = b2TransformPoint(ref transform, localOutput.point);
                output.normal = b2RotateVector(transform.q, localOutput.normal);
                maxFraction = localOutput.fraction;
            }

            offset = b2Add(offset, increment);
        }

        // capsule
        {
            B2Transform transform = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);
            B2Vec2 v1 = b2TransformPoint(ref transform, m_capsule.center1);
            B2Vec2 v2 = b2TransformPoint(ref transform, m_capsule.center2);
            m_context.draw.DrawSolidCapsule(v1, v2, m_capsule.radius, color1);

            B2Vec2 start = b2InvTransformPoint(transform, m_rayStart);
            B2Vec2 translation = b2InvRotateVector(transform.q, b2Sub(m_rayEnd, m_rayStart));
            B2RayCastInput input = new B2RayCastInput(start, translation, maxFraction);

            B2CastOutput localOutput = b2RayCastCapsule(ref input, ref m_capsule);
            if (localOutput.hit)
            {
                output = localOutput;
                output.point = b2TransformPoint(ref transform, localOutput.point);
                output.normal = b2RotateVector(transform.q, localOutput.normal);
                maxFraction = localOutput.fraction;
            }

            offset = b2Add(offset, increment);
        }

        // box
        {
            B2Transform transform = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);
            m_context.draw.DrawSolidPolygon(ref transform, m_box.vertices.AsSpan(), m_box.count, 0.0f, color1);

            B2Vec2 start = b2InvTransformPoint(transform, m_rayStart);
            B2Vec2 translation = b2InvRotateVector(transform.q, b2Sub(m_rayEnd, m_rayStart));
            B2RayCastInput input = new B2RayCastInput(start, translation, maxFraction);

            B2CastOutput localOutput = b2RayCastPolygon(ref input, ref m_box);
            if (localOutput.hit)
            {
                output = localOutput;
                output.point = b2TransformPoint(ref transform, localOutput.point);
                output.normal = b2RotateVector(transform.q, localOutput.normal);
                maxFraction = localOutput.fraction;
            }

            offset = b2Add(offset, increment);
        }

        // triangle
        {
            B2Transform transform = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);
            m_context.draw.DrawSolidPolygon(ref transform, m_triangle.vertices.AsSpan(), m_triangle.count, 0.0f, color1);

            B2Vec2 start = b2InvTransformPoint(transform, m_rayStart);
            B2Vec2 translation = b2InvRotateVector(transform.q, b2Sub(m_rayEnd, m_rayStart));
            B2RayCastInput input = new B2RayCastInput(start, translation, maxFraction);

            B2CastOutput localOutput = b2RayCastPolygon(ref input, ref m_triangle);
            if (localOutput.hit)
            {
                output = localOutput;
                output.point = b2TransformPoint(ref transform, localOutput.point);
                output.normal = b2RotateVector(transform.q, localOutput.normal);
                maxFraction = localOutput.fraction;
            }

            offset = b2Add(offset, increment);
        }

        // segment
        {
            B2Transform transform = new B2Transform(b2Add(m_transform.p, offset), m_transform.q);

            B2Vec2 p1 = b2TransformPoint(ref transform, m_segment.point1);
            B2Vec2 p2 = b2TransformPoint(ref transform, m_segment.point2);
            m_context.draw.DrawSegment(p1, p2, color1);

            B2Vec2 start = b2InvTransformPoint(transform, m_rayStart);
            B2Vec2 translation = b2InvRotateVector(transform.q, b2Sub(m_rayEnd, m_rayStart));
            B2RayCastInput input = new B2RayCastInput(start, translation, maxFraction);

            B2CastOutput localOutput = b2RayCastSegment(ref input, m_segment, false);
            if (localOutput.hit)
            {
                output = localOutput;
                output.point = b2TransformPoint(ref transform, localOutput.point);
                output.normal = b2RotateVector(transform.q, localOutput.normal);
                maxFraction = localOutput.fraction;
            }

            offset = b2Add(offset, increment);
        }

        DrawRay(ref output);
    }
}