// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.Samples.Graphics.Cameras;
using static Box2D.NET.Samples.Graphics.Backgrounds;
using static Box2D.NET.Samples.Graphics.Points;
using static Box2D.NET.Samples.Graphics.Circles;
using static Box2D.NET.Samples.Graphics.Lines;
using static Box2D.NET.Samples.Graphics.SolidCapsules;
using static Box2D.NET.Samples.Graphics.SolidCircles;
using static Box2D.NET.Samples.Graphics.SolidPolygons;
using static Box2D.NET.Samples.Graphics.Fonts;

namespace Box2D.NET.Samples.Graphics;

public static class Draws
{
    public static Draw CreateDraw(SampleContext context)
    {
        var draw = new Draw();
        draw.camera = context.camera;
        draw.gl = context.gl;

        draw.background = CreateBackground(context.gl);
        draw.points = CreatePointDrawData(context.gl);
        draw.lines = CreateLineRender(context.gl);
        draw.hollowCircles = CreateCircles(context.gl);
        draw.circles = CreateSolidCircles(context.gl);
        draw.capsules = CreateSolidCapsule(context.gl);
        draw.polygons = CreateSolidPolygons(context.gl);
        draw.font = CreateFont(context.gl, "data/droid_sans.ttf", 18.0f);

        return draw;
    }

    public static void DestroyDraw(Draw draw)
    {
        DestroyBackground(draw.gl, ref draw.background);
        DestroyPointDrawData(draw.gl, ref draw.points);
        DestroyLineRender(draw.gl, ref draw.lines);
        DestroyCircles(draw.gl, ref draw.hollowCircles);
        DestroySolidCircles(draw.gl, ref draw.circles);
        DestroyCapsules(draw.gl, ref draw.capsules);
        DestroyPolygons(draw.gl, ref draw.polygons);
        DestroyFont(draw.gl, ref draw.font);
    }

    public static void DrawPolygon(Draw draw, ReadOnlySpan<B2Vec2> vertices, int vertexCount, B2HexColor color)
    {
        B2Vec2 p1 = vertices[vertexCount - 1];
        for (int i = 0; i < vertexCount; ++i)
        {
            B2Vec2 p2 = vertices[i];
            AddLine(ref draw.lines, p1, p2, color);
            p1 = p2;
        }
    }

    public static void DrawSolidPolygon(Draw draw, ref B2Transform transform, ReadOnlySpan<B2Vec2> vertices, int vertexCount, float radius, B2HexColor color)
    {
        AddPolygon(ref draw.polygons, ref transform, vertices, vertexCount, radius, color);
    }

    public static void DrawTransform(Draw draw, B2Transform transform, float scale)
    {
        B2Vec2 p1 = transform.p;

        B2Vec2 p2 = b2MulAdd(p1, scale, b2Rot_GetXAxis(transform.q));
        AddLine(ref draw.lines, p1, p2, B2HexColor.b2_colorRed);

        p2 = b2MulAdd(p1, scale, b2Rot_GetYAxis(transform.q));
        AddLine(ref draw.lines, p1, p2, B2HexColor.b2_colorGreen);
    }

    public static void DrawBounds(Draw draw, B2AABB aabb, B2HexColor c)
    {
        B2Vec2 p1 = aabb.lowerBound;
        B2Vec2 p2 = new B2Vec2(aabb.upperBound.X, aabb.lowerBound.Y);
        B2Vec2 p3 = aabb.upperBound;
        B2Vec2 p4 = new B2Vec2(aabb.lowerBound.X, aabb.upperBound.Y);

        AddLine(ref draw.lines, p1, p2, c);
        AddLine(ref draw.lines, p2, p3, c);
        AddLine(ref draw.lines, p3, p4, c);
        AddLine(ref draw.lines, p4, p1, c);
    }
    
    public static void DrawScreenString(Draw draw, float x, float y, B2HexColor color, string message)
    {
        AddText(ref draw.font, x, y, color, message);
    }

    public static void DrawWorldString(Draw draw, Camera camera, B2Vec2 p, B2HexColor color, string message)
    {
        B2Vec2 ps = ConvertWorldToScreen(camera, p);
        AddText(ref draw.font, ps.X, ps.Y, color, message);
    }

    public static void DrawCircle(Draw draw, B2Vec2 center, float radius, B2HexColor color)
    {
        AddCircle(ref draw.hollowCircles, center, radius, color);
    }

    public static void DrawSolidCircle(Draw draw, B2Transform transform, float radius, B2HexColor color)
    {
        AddSolidCircle(ref draw.circles, ref transform, radius, color);
    }

    public static void DrawSolidCapsule(Draw draw, B2Vec2 p1, B2Vec2 p2, float radius, B2HexColor color)
    {
        AddCapsule(ref draw.capsules, p1, p2, radius, color);
    }

    public static void DrawLine(Draw draw, B2Vec2 p1, B2Vec2 p2, B2HexColor color)
    {
        AddLine(ref draw.lines, p1, p2, color);
    }


    public static void DrawPoint(Draw draw, B2Vec2 p, float size, B2HexColor color)
    {
        AddPoint(ref draw.points, p, size, color);
    }



    public static void FlushDraw(Draw draw)
    {
        // order matters
        FlushSolidCircles(draw.gl, ref draw.circles, draw.camera);
        FlushCapsules(draw.gl, ref draw.capsules, draw.camera);
        FlushPolygons(draw.gl, ref draw.polygons, draw.camera);
        FlushCircles(draw.gl, ref draw.hollowCircles, draw.camera);
        FlushLines(draw.gl, ref draw.lines, draw.camera);
        FlushPoints(draw.gl, ref draw.points, draw.camera);
        FlushText(draw.gl, ref draw.font, draw.camera);
        draw.gl.CheckOpenGL();
    }
}
