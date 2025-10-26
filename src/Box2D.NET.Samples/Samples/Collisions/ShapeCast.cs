// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples.Samples.Collisions;

public class ShapeCast : Sample
{
    private static readonly int SampleShapeCast = SampleFactory.Shared.RegisterSample("Collision", "Shape Cast", Create);

    public enum ShapeType
    {
        e_point,
        e_segment,
        e_triangle,
        e_box
    };

    private B2Polygon m_box;
    private B2Polygon m_triangle;
    private B2Vec2 m_point;
    private B2Segment m_segment;

    private ShapeType m_typeA;
    private ShapeType m_typeB;
    private float m_radiusA;
    private float m_radiusB;
    private B2ShapeProxy m_proxyA;
    private B2ShapeProxy m_proxyB;

    private B2Transform m_transform;
    private float m_angle;
    private B2Vec2 m_translation;

    private B2Vec2 m_basePosition;
    private B2Vec2 m_startPoint;
    private float m_baseAngle;

    private bool m_dragging;
    private bool m_sweeping;
    private bool m_rotating;
    private bool m_showIndices;
    private bool m_drawSimplex;
    private bool m_encroach;

    // for step
    private B2CastOutput output;
    private B2Transform inputTransform;
    private B2DistanceOutput _distanceOutput;

    private static Sample Create(SampleContext context)
    {
        return new ShapeCast(context);
    }


    public ShapeCast(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(-0.0f, 0.25f);
            m_camera.zoom = 3.0f;
        }

        m_point = b2Vec2_zero;
        m_segment = new B2Segment(new B2Vec2(0.0f, 0.0f), new B2Vec2(0.5f, 0.0f));

        {
            B2Vec2[] points = [new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), new B2Vec2(0.0f, 1.0f)];
            B2Hull hull = b2ComputeHull(points, 3);
            m_triangle = b2MakePolygon(ref hull, 0.0f);
        }

#if ZERO
        {
            b2Vec2 points[4] = {};
            points[0].x = -0.599999964;
            points[0].y = -0.700000048;
            points[1].x = 0.449999988;
            points[1].y = -0.700000048;
            points[2].x = 0.449999988;
            points[2].y = 0.350000024;
            points[3].x = -0.599999964;
            points[3].y = 0.350000024;
            b2Hull hull = b2ComputeHull( points, 4 );
            m_triangle = b2MakePolygon( &hull, 0.0f );
        }
#endif

        m_box = b2MakeOffsetBox(0.5f, 0.5f, new B2Vec2(0.0f, 0.0f), b2Rot_identity);

#if ZERO
        {
            b2Vec2 points[4] = {};
            points[0].x = 0.449999988;
            points[0].y = -0.100000001;
            points[1].x = 0.550000012;
            points[1].y = -0.100000001;
            points[2].x = 0.550000012;
            points[2].y = 0.100000001;
            points[3].x = 0.449999988;
            points[3].y = 0.100000001;
            b2Hull hull = b2ComputeHull( points, 4 );
            m_box = b2MakePolygon( &hull, 0.0f );
        }
#endif

        m_transform = new B2Transform(new B2Vec2(-0.6f, 0.0f), b2Rot_identity);
        m_translation = new B2Vec2(2.0f, 0.0f);
        m_angle = 0.0f;
        m_startPoint = new B2Vec2(0.0f, 0.0f);
        m_basePosition = new B2Vec2(0.0f, 0.0f);
        m_baseAngle = 0.0f;

        m_dragging = false;
        m_sweeping = false;
        m_rotating = false;
        m_showIndices = false;
        m_drawSimplex = false;
        m_encroach = false;

        m_typeA = ShapeType.e_box;
        m_typeB = ShapeType.e_point;
        m_radiusA = 0.0f;
        m_radiusB = 0.2f;

        m_proxyA = MakeProxy(m_typeA, m_radiusA);
        m_proxyB = MakeProxy(m_typeB, m_radiusB);
    }

    private B2ShapeProxy MakeProxy(ShapeType type, float radius)
    {
        B2ShapeProxy proxy = new B2ShapeProxy();
        proxy.radius = radius;

        switch (type)
        {
            case ShapeType.e_point:
                proxy.points[0] = b2Vec2_zero;
                proxy.count = 1;
                break;

            case ShapeType.e_segment:
                proxy.points[0] = m_segment.point1;
                proxy.points[1] = m_segment.point2;
                proxy.count = 2;
                break;

            case ShapeType.e_triangle:
                for (int i = 0; i < m_triangle.count; ++i)
                {
                    proxy.points[i] = m_triangle.vertices[i];
                }

                proxy.count = m_triangle.count;
                break;

            case ShapeType.e_box:
                proxy.points[0] = m_box.vertices[0];
                proxy.points[1] = m_box.vertices[1];
                proxy.points[2] = m_box.vertices[2];
                proxy.points[3] = m_box.vertices[3];
                proxy.count = 4;
                break;

            default:
                B2_ASSERT(false);
                break;
        }

        return proxy;
    }

    private void DrawShape(ShapeType type, B2Transform transform, float radius, B2HexColor color)
    {
        switch (type)
        {
            case ShapeType.e_point:
            {
                B2Vec2 p = b2TransformPoint(ref transform, m_point);
                if (radius > 0.0f)
                {
                    DrawSolidCircle(m_draw, new B2Transform(m_point, transform.q), radius, color);
                }
                else
                {
                    DrawPoint(m_draw, p, 5.0f, color);
                }
            }
                break;

            case ShapeType.e_segment:
            {
                B2Vec2 p1 = b2TransformPoint(ref transform, m_segment.point1);
                B2Vec2 p2 = b2TransformPoint(ref transform, m_segment.point2);

                if (radius > 0.0f)
                {
                    DrawSolidCapsule(m_draw, p1, p2, radius, color);
                }
                else
                {
                    DrawLine(m_draw, p1, p2, color);
                }
            }
                break;

            case ShapeType.e_triangle:
                DrawSolidPolygon(m_draw, ref transform, m_triangle.vertices.AsSpan(), m_triangle.count, radius, color);
                break;

            case ShapeType.e_box:
                DrawSolidPolygon(m_draw, ref transform, m_box.vertices.AsSpan(), m_box.count, radius, color);
                break;

            default:
                B2_ASSERT(false);
                break;
        }
    }

    public override void MouseDown(B2Vec2 p, MouseButton button, KeyModifiers mods)
    {
        if (button == (int)MouseButton.Left)
        {
            if (mods == 0)
            {
                m_dragging = true;
                m_sweeping = false;
                m_rotating = false;
                m_startPoint = p;
                m_basePosition = m_transform.p;
            }
            else if (mods == KeyModifiers.Shift)
            {
                m_dragging = false;
                m_sweeping = false;
                m_rotating = true;
                m_startPoint = p;
                m_baseAngle = m_angle;
            }
            else if (mods == KeyModifiers.Control)
            {
                m_dragging = false;
                m_sweeping = true;
                m_rotating = false;
                m_startPoint = p;
                m_basePosition = b2Vec2_zero;
            }
        }
    }

    public override void MouseUp(B2Vec2 p, MouseButton button)
    {
        if (button == (int)MouseButton.Left)
        {
            m_dragging = false;
            m_sweeping = false;
            m_rotating = false;
        }
    }

    public override void MouseMove(B2Vec2 p)
    {
        if (m_dragging)
        {
            m_transform.p = m_basePosition + 0.5f * (p - m_startPoint);
        }
        else if (m_rotating)
        {
            float dx = p.X - m_startPoint.X;
            m_angle = b2ClampFloat(m_baseAngle + 1.0f * dx, -B2_PI, B2_PI);
            m_transform.q = b2MakeRot(m_angle);
        }
        else if (m_sweeping)
        {
            m_translation = p - m_startPoint;
        }
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 300.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Shape Distance", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        string[] shapeTypes = { "point", "segment", "triangle", "box" };
        int shapeType = (int)m_typeA;
        if (ImGui.Combo("shape A", ref shapeType, shapeTypes, shapeTypes.Length))
        {
            m_typeA = (ShapeType)shapeType;
            m_proxyA = MakeProxy(m_typeA, m_radiusA);
        }

        if (ImGui.SliderFloat("radius A", ref m_radiusA, 0.0f, 0.5f, "%.2f"))
        {
            m_proxyA.radius = m_radiusA;
        }

        shapeType = (int)m_typeB;
        if (ImGui.Combo("shape B", ref shapeType, shapeTypes, shapeTypes.Length))
        {
            m_typeB = (ShapeType)shapeType;
            m_proxyB = MakeProxy(m_typeB, m_radiusB);
        }

        if (ImGui.SliderFloat("radius B", ref m_radiusB, 0.0f, 0.5f, "%.2f"))
        {
            m_proxyB.radius = m_radiusB;
        }

        ImGui.Separator();

        ImGui.SliderFloat("x offset", ref m_transform.p.X, -2.0f, 2.0f, "%.2f");
        ImGui.SliderFloat("y offset", ref m_transform.p.Y, -2.0f, 2.0f, "%.2f");

        if (ImGui.SliderFloat("angle", ref m_angle, -B2_PI, B2_PI, "%.2f"))
        {
            m_transform.q = b2MakeRot(m_angle);
        }

        ImGui.Separator();

        ImGui.Checkbox("show indices", ref m_showIndices);
        ImGui.Checkbox("encroach", ref m_encroach);

        ImGui.End();
    }

    public override void Step()
    {
        base.Step();

        B2ShapeCastPairInput input = new B2ShapeCastPairInput();
        input.proxyA = m_proxyA;
        input.proxyB = m_proxyB;
        input.transformA = b2Transform_identity;
        input.transformB = m_transform;
        input.translationB = m_translation;
        input.maxFraction = 1.0f;
        input.canEncroach = m_encroach;

        output = b2ShapeCast(ref input);

        B2Transform transform;
        transform.q = m_transform.q;
        transform.p = b2MulAdd(m_transform.p, output.fraction, input.translationB);

        B2DistanceInput distanceInput;
        distanceInput.proxyA = m_proxyA;
        distanceInput.proxyB = m_proxyB;
        distanceInput.transformA = b2Transform_identity;
        distanceInput.transformB = transform;
        distanceInput.useRadii = false;
        B2SimplexCache distanceCache = new B2SimplexCache();
        distanceCache.count = 0;
        B2DistanceOutput distanceOutput = b2ShapeDistance(ref distanceInput, ref distanceCache, null, 0);


        inputTransform = transform;
        _distanceOutput = distanceOutput;
    }

    public override void Draw()
    {
        base.Draw();

        DrawTextLine($"hit = {output.hit}, iterations = {output.iterations}, fraction = {output.fraction}, distance = {_distanceOutput.distance}");

        DrawShape(m_typeA, b2Transform_identity, m_radiusA, B2HexColor.b2_colorCyan);
        DrawShape(m_typeB, m_transform, m_radiusB, B2HexColor.b2_colorLightGreen);
        B2Transform transform2 = new B2Transform(m_transform.p + m_translation, m_transform.q);
        DrawShape(m_typeB, transform2, m_radiusB, B2HexColor.b2_colorIndianRed);

        if (output.hit)
        {
            DrawShape(m_typeB, inputTransform, m_radiusB, B2HexColor.b2_colorPlum);


            if (output.fraction > 0.0f)
            {
                DrawPoint(m_draw, output.point, 5.0f, B2HexColor.b2_colorWhite);
                DrawLine(m_draw, output.point, output.point + 0.5f * output.normal, B2HexColor.b2_colorYellow);
            }
            else
            {
                DrawPoint(m_draw, output.point, 5.0f, B2HexColor.b2_colorPeru);
            }
        }

        if (m_showIndices)
        {
            for (int i = 0; i < m_proxyA.count; ++i)
            {
                B2Vec2 p = m_proxyA.points[i];
                DrawWorldString(m_draw, m_camera, p, B2HexColor.b2_colorWhite, $" {i}");
            }

            for (int i = 0; i < m_proxyB.count; ++i)
            {
                B2Vec2 p = b2TransformPoint(ref m_transform, m_proxyB.points[i]);
                DrawWorldString(m_draw, m_camera, p, B2HexColor.b2_colorWhite, $" {i}");
            }
        }

        DrawTextLine("mouse button 1: drag");
        DrawTextLine("mouse button 1 + shift: rotate");
        DrawTextLine("mouse button 1 + control: sweep");
        DrawTextLine($"distance = {_distanceOutput.distance:F2}, iterations = {output.iterations}");
    }
}