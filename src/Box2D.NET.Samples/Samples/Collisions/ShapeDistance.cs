// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Collisions;

namespace Box2D.NET.Samples.Samples.Collisions;

public class ShapeDistance : Sample
{
    public const int SIMPLEX_CAPACITY = 20;

    B2Polygon m_box;
    B2Polygon m_triangle;
    B2Vec2 m_point;
    B2Segment m_segment;

    ShapeType m_typeA;
    ShapeType m_typeB;
    float m_radiusA;
    float m_radiusB;
    B2ShapeProxy m_proxyA;
    B2ShapeProxy m_proxyB;

    B2SimplexCache m_cache;
    B2Simplex[] m_simplexes = new B2Simplex[SIMPLEX_CAPACITY];
    int m_simplexCount;
    int m_simplexIndex;

    B2Transform m_transform;
    float m_angle;

    B2Vec2 m_basePosition;
    B2Vec2 m_startPoint;
    float m_baseAngle;

    bool m_dragging;
    bool m_rotating;
    bool m_showIndices;
    bool m_useCache;
    bool m_drawSimplex;

    private static readonly int SampleShapeDistance = SampleFactory.Shared.RegisterSample("Collision", "Shape Distance", Create);

    private static Sample Create(Settings settings)
    {
        return new ShapeDistance(settings);
    }

    enum ShapeType
    {
        e_point,
        e_segment,
        e_triangle,
        e_box
    };

    public ShapeDistance(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.0f, 0.0f);
            B2.g_camera.m_zoom = 3.0f;
        }

        m_point = b2Vec2_zero;
        m_segment = new B2Segment(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f));

        {
            B2Vec2[] points = new B2Vec2[3] { new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), new B2Vec2(0.0f, 1.0f) };
            B2Hull hull = b2ComputeHull(points, 3);
            m_triangle = b2MakePolygon(hull, 0.0f);
        }

        m_box = b2MakeBox(0.5f, 0.5f);

        m_transform = new B2Transform(new B2Vec2(1.5f, -1.5f), b2Rot_identity);
        m_angle = 0.0f;

        m_cache = b2_emptySimplexCache;
        m_simplexCount = 0;
        m_startPoint = new B2Vec2(0.0f, 0.0f);
        m_basePosition = new B2Vec2(0.0f, 0.0f);
        m_baseAngle = 0.0f;

        m_dragging = false;
        m_rotating = false;
        m_showIndices = false;
        m_useCache = false;
        m_drawSimplex = false;

        m_typeA = ShapeType.e_box;
        m_typeB = ShapeType.e_box;
        m_radiusA = 0.0f;
        m_radiusB = 0.0f;

        m_proxyA = MakeProxy(m_typeA, m_radiusA);
        m_proxyB = MakeProxy(m_typeB, m_radiusB);
    }

    B2ShapeProxy MakeProxy(ShapeType type, float radius)
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
                proxy.points[0] = m_triangle.vertices[0];
                proxy.points[1] = m_triangle.vertices[1];
                proxy.points[2] = m_triangle.vertices[2];
                proxy.count = 3;
                break;

            case ShapeType.e_box:
                proxy.points[0] = m_box.vertices[0];
                proxy.points[1] = m_box.vertices[1];
                proxy.points[2] = m_box.vertices[2];
                proxy.points[3] = m_box.vertices[3];
                proxy.count = 4;
                break;

            default:
                Debug.Assert(false);
                break;
        }

        return proxy;
    }

    void DrawShape(ShapeType type, ref B2Transform transform, float radius, B2HexColor color)
    {
        switch (type)
        {
            case ShapeType.e_point:
            {
                B2Vec2 p = b2TransformPoint(ref transform, m_point);
                if (radius > 0.0f)
                {
                    B2.g_draw.DrawSolidCircle(ref transform, m_point, radius, color);
                }
                else
                {
                    B2.g_draw.DrawPoint(p, 5.0f, color);
                }
            }
                break;

            case ShapeType.e_segment:
            {
                B2Vec2 p1 = b2TransformPoint(ref transform, m_segment.point1);
                B2Vec2 p2 = b2TransformPoint(ref transform, m_segment.point2);

                if (radius > 0.0f)
                {
                    B2.g_draw.DrawSolidCapsule(p1, p2, radius, color);
                }
                else
                {
                    B2.g_draw.DrawSegment(p1, p2, color);
                }
            }
                break;

            case ShapeType.e_triangle:
                B2.g_draw.DrawSolidPolygon(ref transform, m_triangle.vertices, 3, radius, color);
                break;

            case ShapeType.e_box:
                B2.g_draw.DrawSolidPolygon(ref transform, m_box.vertices, 4, radius, color);
                break;

            default:
                Debug.Assert(false);
                break;
        }
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 310.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, B2.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Shape Distance", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        string[] shapeTypes = ["point", "segment", "triangle", "box"];
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

        ImGui.SliderFloat("x offset", ref m_transform.p.x, -2.0f, 2.0f, "%.2f");
        ImGui.SliderFloat("y offset", ref m_transform.p.y, -2.0f, 2.0f, "%.2f");

        if (ImGui.SliderFloat("angle", ref m_angle, -B2_PI, B2_PI, "%.2f"))
        {
            m_transform.q = b2MakeRot(m_angle);
        }

        ImGui.Separator();

        ImGui.Checkbox("show indices", ref m_showIndices);
        ImGui.Checkbox("use cache", ref m_useCache);

        ImGui.Separator();

        if (ImGui.Checkbox("draw simplex", ref m_drawSimplex))
        {
            m_simplexIndex = 0;
        }

        if (m_drawSimplex)
        {
            ImGui.SliderInt("index", ref m_simplexIndex, 0, m_simplexCount - 1);
            m_simplexIndex = b2ClampInt(m_simplexIndex, 0, m_simplexCount - 1);
        }

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

    static B2Vec2 Weight2(float a1, B2Vec2 w1, float a2, B2Vec2 w2)
    {
        return new B2Vec2(a1 * w1.x + a2 * w2.x, a1 * w1.y + a2 * w2.y);
    }

    static B2Vec2 Weight3(float a1, B2Vec2 w1, float a2, B2Vec2 w2, float a3, B2Vec2 w3)
    {
        return new B2Vec2(a1 * w1.x + a2 * w2.x + a3 * w3.x, a1 * w1.y + a2 * w2.y + a3 * w3.y);
    }

    void ComputeSimplexWitnessPoints(ref B2Vec2 a, ref B2Vec2 b, B2Simplex s)
    {
        switch (s.count)
        {
            case 0:
                Debug.Assert(false);
                break;

            case 1:
                a = s.v1.wA;
                b = s.v1.wB;
                break;

            case 2:
                a = Weight2(s.v1.a, s.v1.wA, s.v2.a, s.v2.wA);
                b = Weight2(s.v1.a, s.v1.wB, s.v2.a, s.v2.wB);
                break;

            case 3:
                a = Weight3(s.v1.a, s.v1.wA, s.v2.a, s.v2.wA, s.v3.a, s.v3.wA);
                b = a;
                break;

            default:
                Debug.Assert(false);
                break;
        }
    }

    public override void Step(Settings _)
    {
        B2DistanceInput input = new B2DistanceInput();
        input.proxyA = m_proxyA;
        input.proxyB = m_proxyB;
        input.transformA = b2Transform_identity;
        input.transformB = m_transform;
        input.useRadii = m_radiusA > 0.0f || m_radiusB > 0.0f;

        if (m_useCache == false)
        {
            m_cache.count = 0;
        }

        B2DistanceOutput output = b2ShapeDistance(ref m_cache, ref input, m_simplexes, SIMPLEX_CAPACITY);

        m_simplexCount = output.simplexCount;

        var empty = b2Transform_identity;
        DrawShape(m_typeA, ref empty, m_radiusA, B2HexColor.b2_colorCyan);
        DrawShape(m_typeB, ref m_transform, m_radiusB, B2HexColor.b2_colorBisque);

        if (m_drawSimplex)
        {
            B2Simplex simplex = m_simplexes[m_simplexIndex];
            B2SimplexVertex[] vertices = new B2SimplexVertex[3] { simplex.v1, simplex.v2, simplex.v3 };

            if (m_simplexIndex > 0)
            {
                // The first recorded simplex does not have valid barycentric coordinates
                B2Vec2 pointA = new B2Vec2();
                B2Vec2 pointB = new B2Vec2();
                ComputeSimplexWitnessPoints(ref pointA, ref pointB, simplex);

                B2.g_draw.DrawSegment(pointA, pointB, B2HexColor.b2_colorWhite);
                B2.g_draw.DrawPoint(pointA, 5.0f, B2HexColor.b2_colorWhite);
                B2.g_draw.DrawPoint(pointB, 5.0f, B2HexColor.b2_colorWhite);
            }

            B2HexColor[] colors = new B2HexColor[3] { B2HexColor.b2_colorRed, B2HexColor.b2_colorGreen, B2HexColor.b2_colorBlue };

            for (int i = 0; i < simplex.count; ++i)
            {
                B2SimplexVertex vertex = vertices[i];
                B2.g_draw.DrawPoint(vertex.wA, 5.0f, colors[i]);
                B2.g_draw.DrawPoint(vertex.wB, 5.0f, colors[i]);
            }
        }
        else
        {
            B2.g_draw.DrawSegment(output.pointA, output.pointB, B2HexColor.b2_colorWhite);
            B2.g_draw.DrawPoint(output.pointA, 5.0f, B2HexColor.b2_colorWhite);
            B2.g_draw.DrawPoint(output.pointB, 5.0f, B2HexColor.b2_colorWhite);
        }

        if (m_showIndices)
        {
            for (int i = 0; i < m_proxyA.count; ++i)
            {
                B2Vec2 p = m_proxyA.points[i];
                B2.g_draw.DrawString(p, " %d", i);
            }

            for (int i = 0; i < m_proxyB.count; ++i)
            {
                B2Vec2 p = b2TransformPoint(ref m_transform, m_proxyB.points[i]);
                B2.g_draw.DrawString(p, " %d", i);
            }
        }

        B2.g_draw.DrawString(5, m_textLine, "mouse button 1: drag");
        m_textLine += m_textIncrement;
        B2.g_draw.DrawString(5, m_textLine, "mouse button 1 + shift: rotate");
        m_textLine += m_textIncrement;
        B2.g_draw.DrawString(5, m_textLine, "distance = %.2f, iterations = %d", output.distance, output.iterations);
        m_textLine += m_textIncrement;

        if (m_cache.count == 1)
        {
            B2.g_draw.DrawString(5, m_textLine, "cache = {%d}, {%d}", m_cache.indexA[0], m_cache.indexB[0]);
        }
        else if (m_cache.count == 2)
        {
            B2.g_draw.DrawString(5, m_textLine, "cache = {%d, %d}, {%d, %d}", m_cache.indexA[0], m_cache.indexA[1],
                m_cache.indexB[0], m_cache.indexB[1]);
        }
        else if (m_cache.count == 3)
        {
            B2.g_draw.DrawString(5, m_textLine, "cache = {%d, %d, %d}, {%d, %d, %d}", m_cache.indexA[0], m_cache.indexA[1],
                m_cache.indexA[2], m_cache.indexB[0], m_cache.indexB[1], m_cache.indexB[2]);
        }

        m_textLine += m_textIncrement;
    }
}
