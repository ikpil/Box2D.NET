// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Box2D.NET.Samples.Graphics;
using ImGuiNET;
using Silk.NET.OpenGL;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples;

// This class implements Box2D debug drawing callbacks
public class Draw
{
    private GL _gl;
    private Camera _camera;
    public bool m_showUI;

    private GLBackground m_background;
    private GLPoints m_points;
    private GLLines m_lines;
    private GLCircles m_circles;
    private GLSolidCircles m_solidCircles;
    private GLSolidCapsules m_solidCapsules;
    private GLSolidPolygons m_solidPolygons;
    public B2DebugDraw m_debugDraw;

    public ImFontPtr m_regularFont;
    public ImFontPtr m_mediumFont;
    public ImFontPtr m_largeFont;


    public Draw()
    {
        m_showUI = true;

        m_background = new GLBackground();
        m_points = new GLPoints();
        m_lines = new GLLines();
        m_circles = new GLCircles();
        m_solidCircles = new GLSolidCircles();
        m_solidCapsules = new GLSolidCapsules();
        m_solidPolygons = new GLSolidPolygons();


        B2AABB bounds = new B2AABB(new B2Vec2(-float.MaxValue, -float.MaxValue), new B2Vec2(float.MaxValue, float.MaxValue));

        m_debugDraw = new B2DebugDraw();
        m_debugDraw.DrawPolygonFcn = DrawPolygonFcn;
        m_debugDraw.DrawSolidPolygonFcn = DrawSolidPolygonFcn;
        m_debugDraw.DrawCircleFcn = DrawCircleFcn;
        m_debugDraw.DrawSolidCircleFcn = DrawSolidCircleFcn;
        m_debugDraw.DrawSolidCapsuleFcn = DrawSolidCapsuleFcn;
        m_debugDraw.DrawSegmentFcn = DrawSegmentFcn;
        m_debugDraw.DrawTransformFcn = DrawTransformFcn;
        m_debugDraw.DrawPointFcn = DrawPointFcn;
        m_debugDraw.DrawStringFcn = DrawStringFcn;
        m_debugDraw.drawingBounds = bounds;

        m_debugDraw.drawShapes = true;
        m_debugDraw.drawJoints = true;
        m_debugDraw.drawJointExtras = false;
        m_debugDraw.drawBounds = false;
        m_debugDraw.drawMass = false;
        m_debugDraw.drawContacts = false;
        m_debugDraw.drawGraphColors = false;
        m_debugDraw.drawContactNormals = false;
        m_debugDraw.drawContactImpulses = false;
        m_debugDraw.drawContactFeatures = false;
        m_debugDraw.drawFrictionImpulses = false;
        m_debugDraw.drawIslands = false;

        m_debugDraw.context = this;

        m_mediumFont = default;
        m_largeFont = default;
        m_regularFont = default;
    }

    public void Create(SampleContext context)
    {
        _camera = context.camera;
        _gl = context.gl;

        m_background.Create(context);
        m_points.Create(context);
        m_lines.Create(context);
        m_circles.Create(context);
        m_solidCircles.Create(context);
        m_solidCapsules.Create(context);
        m_solidPolygons.Create(context);
    }

    public void Destroy()
    {
        m_background.Destroy();
        m_background = null;

        m_points.Destroy();
        m_points = null;

        m_lines.Destroy();
        m_lines = null;

        m_circles.Destroy();
        m_circles = null;

        m_solidCircles.Destroy();
        m_solidCircles = null;

        m_solidCapsules.Destroy();
        m_solidCapsules = null;

        m_solidPolygons.Destroy();
        m_solidPolygons = null;
    }

    public void DrawPolygon(ReadOnlySpan<B2Vec2> vertices, int vertexCount, B2HexColor color)
    {
        B2Vec2 p1 = vertices[vertexCount - 1];
        for (int i = 0; i < vertexCount; ++i)
        {
            B2Vec2 p2 = vertices[i];
            m_lines.AddLine(p1, p2, color);
            p1 = p2;
        }
    }

    public void DrawSolidPolygon(ref B2Transform transform, ReadOnlySpan<B2Vec2> vertices, int vertexCount, float radius, B2HexColor color)
    {
        m_solidPolygons.AddPolygon(ref transform, vertices, vertexCount, radius, color);
    }

    public void DrawCircle(B2Vec2 center, float radius, B2HexColor color)
    {
        m_circles.AddCircle(center, radius, color);
    }

    public void DrawSolidCircle(ref B2Transform transform, B2Vec2 center, float radius, B2HexColor color)
    {
        transform.p = b2TransformPoint(ref transform, center);
        m_solidCircles.AddCircle(ref transform, radius, color);
    }

    public void DrawSolidCapsule(B2Vec2 p1, B2Vec2 p2, float radius, B2HexColor color)
    {
        m_solidCapsules.AddCapsule(p1, p2, radius, color);
    }

    public void DrawLine(B2Vec2 p1, B2Vec2 p2, B2HexColor color)
    {
        m_lines.AddLine(p1, p2, color);
    }

    public void DrawTransform(B2Transform transform)
    {
        const float k_axisScale = 0.2f;
        B2Vec2 p1 = transform.p;

        B2Vec2 p2 = b2MulAdd(p1, k_axisScale, b2Rot_GetXAxis(transform.q));
        m_lines.AddLine(p1, p2, B2HexColor.b2_colorRed);

        p2 = b2MulAdd(p1, k_axisScale, b2Rot_GetYAxis(transform.q));
        m_lines.AddLine(p1, p2, B2HexColor.b2_colorGreen);
    }

    public void DrawPoint(B2Vec2 p, float size, B2HexColor color)
    {
        m_points.AddPoint(p, size, color);
    }

    public void DrawString(int x, int y, string message)
    {
        // if (m_showUI == false)
        //{
        //	return;
        // }


        ImGui.Begin("Overlay",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoScrollbar);
        ImGui.PushFont(m_regularFont);
        ImGui.SetCursorPos(new Vector2(x, y));
        ImGui.TextColored(new Vector4(230, 153, 153, 255), message);
        ImGui.PopFont();
        ImGui.End();
    }

    public void DrawString(B2Vec2 p, string message)
    {
        B2Vec2 ps = _camera.ConvertWorldToScreen(p);


        ImGui.Begin("Overlay",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoScrollbar);
        ImGui.SetCursorPos(new Vector2(ps.X, ps.Y));
        ImGui.TextColored(new Vector4(230, 230, 230, 255), message);
        ImGui.End();
    }

    public void DrawBounds(B2AABB aabb, B2HexColor c)
    {
        B2Vec2 p1 = aabb.lowerBound;
        B2Vec2 p2 = new B2Vec2(aabb.upperBound.X, aabb.lowerBound.Y);
        B2Vec2 p3 = aabb.upperBound;
        B2Vec2 p4 = new B2Vec2(aabb.lowerBound.X, aabb.upperBound.Y);

        m_lines.AddLine(p1, p2, c);
        m_lines.AddLine(p2, p3, c);
        m_lines.AddLine(p3, p4, c);
        m_lines.AddLine(p4, p1, c);
    }

    public void Flush()
    {
        m_solidCircles.Flush(_camera);
        m_solidCapsules.Flush(_camera);
        m_solidPolygons.Flush(_camera);
        m_circles.Flush(_camera);
        m_lines.Flush(_camera);
        m_points.Flush(_camera);
        _gl.CheckOpenGL();
    }

    public void DrawBackground()
    {
        m_background.Draw(_camera);
    }

    public static void DrawPolygonFcn(ReadOnlySpan<B2Vec2> vertices, int vertexCount, B2HexColor color, object context)
    {
        (context as Draw).DrawPolygon(vertices, vertexCount, color);
    }

    public static void DrawSolidPolygonFcn(ref B2Transform transform, ReadOnlySpan<B2Vec2> vertices, int vertexCount, float radius, B2HexColor color, object context)
    {
        (context as Draw).DrawSolidPolygon(ref transform, vertices, vertexCount, radius, color);
    }

    public static void DrawCircleFcn(B2Vec2 center, float radius, B2HexColor color, object context)
    {
        (context as Draw).DrawCircle(center, radius, color);
    }

    public static void DrawSolidCircleFcn(ref B2Transform transform, float radius, B2HexColor color, object context)
    {
        (context as Draw).DrawSolidCircle(ref transform, b2Vec2_zero, radius, color);
    }

    public static void DrawSolidCapsuleFcn(B2Vec2 p1, B2Vec2 p2, float radius, B2HexColor color, object context)
    {
        (context as Draw).DrawSolidCapsule(p1, p2, radius, color);
    }

    public static void DrawSegmentFcn(B2Vec2 p1, B2Vec2 p2, B2HexColor color, object context)
    {
        (context as Draw).DrawLine(p1, p2, color);
    }

    public static void DrawTransformFcn(B2Transform transform, object context)
    {
        (context as Draw).DrawTransform(transform);
    }

    public static void DrawPointFcn(B2Vec2 p, float size, B2HexColor color, object context)
    {
        (context as Draw).DrawPoint(p, size, color);
    }

    public static void DrawStringFcn(B2Vec2 p, string s, B2HexColor color, object context)
    {
        (context as Draw).DrawString(p, s);
    }
}