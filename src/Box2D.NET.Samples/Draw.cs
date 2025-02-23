
// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using static Box2D.NET.math_function;

namespace Box2D.NET.Samples;

#define BUFFER_OFFSET( x ) ( (const void*)( x ) )
#define SHADER_TEXT( x ) "#version 330\n" #x


// This class implements Box2D debug drawing callbacks
public class Draw
{
    public static Draw g_draw;
    public static Camera g_camera;

    public bool m_showUI;

    public GLBackground m_background;
    public GLPoints m_points;
    public GLLines m_lines;
    public GLTriangles m_triangles;
    public GLCircles m_circles;
    public GLSolidCircles m_solidCircles;
    public GLSolidCapsules m_solidCapsules;
    public GLSolidPolygons m_solidPolygons;
    public b2DebugDraw m_debugDraw;

    ImFont m_smallFont;
    ImFont m_regularFont;
    ImFont m_mediumFont;
    ImFont m_largeFont;

    extern struct GLFWwindow* g_mainWindow;

    public Draw()
    {
        m_showUI = true;
        m_points = nullptr;
        m_lines = nullptr;
        m_triangles = nullptr;
        m_circles = nullptr;
        m_solidCircles = nullptr;
        m_solidCapsules = nullptr;
        m_solidPolygons = nullptr;
        m_debugDraw =  {
        }
        ;
        m_smallFont = nullptr;
        m_mediumFont = nullptr;
        m_largeFont = nullptr;
        m_regularFont = nullptr;
        m_background = nullptr;
    }

    ~Draw()
    {
        Debug.Assert(m_points == null);
        Debug.Assert(m_lines == null);
        Debug.Assert(m_triangles == null);
        Debug.Assert(m_circles == null);
        Debug.Assert(m_solidCircles == null);
        Debug.Assert(m_solidCapsules == null);
        Debug.Assert(m_solidPolygons == null);
        Debug.Assert(m_background == null);
    }

    public void Create()
    {
        m_background = new GLBackground();
        m_background.Create();
        m_points = new GLPoints();
        m_points.Create();
        m_lines = new GLLines();
        m_lines.Create();
        m_triangles = new GLTriangles();
        m_triangles.Create();
        m_circles = new GLCircles();
        m_circles.Create();
        m_solidCircles = new GLSolidCircles();
        m_solidCircles.Create();
        m_solidCapsules = new GLSolidCapsules();
        m_solidCapsules.Create();
        m_solidPolygons = new GLSolidPolygons();
        m_solidPolygons.Create();

        b2AABB bounds = { { -FLT_MAX, -FLT_MAX }, { FLT_MAX, FLT_MAX } };

        m_debugDraw =  {
        }
        ;

        m_debugDraw.DrawPolygon = DrawPolygonFcn;
        m_debugDraw.DrawSolidPolygon = DrawSolidPolygonFcn;
        m_debugDraw.DrawCircle = DrawCircleFcn;
        m_debugDraw.DrawSolidCircle = DrawSolidCircleFcn;
        m_debugDraw.DrawSolidCapsule = DrawSolidCapsuleFcn;
        m_debugDraw.DrawSegment = DrawSegmentFcn;
        m_debugDraw.DrawTransform = DrawTransformFcn;
        m_debugDraw.DrawPoint = DrawPointFcn;
        m_debugDraw.DrawString = DrawStringFcn;
        m_debugDraw.drawingBounds = bounds;

        m_debugDraw.useDrawingBounds = false;
        m_debugDraw.drawShapes = true;
        m_debugDraw.drawJoints = true;
        m_debugDraw.drawJointExtras = false;
        m_debugDraw.drawAABBs = false;
        m_debugDraw.drawMass = false;
        m_debugDraw.drawContacts = false;
        m_debugDraw.drawGraphColors = false;
        m_debugDraw.drawContactNormals = false;
        m_debugDraw.drawContactImpulses = false;
        m_debugDraw.drawFrictionImpulses = false;

        m_debugDraw.context = this;
    }

    public void Destroy()
    {
        m_background.Destroy();
        delete m_background;
        m_background = nullptr;

        m_points.Destroy();
        delete m_points;
        m_points = nullptr;

        m_lines.Destroy();
        delete m_lines;
        m_lines = nullptr;

        m_triangles.Destroy();
        delete m_triangles;
        m_triangles = nullptr;

        m_circles.Destroy();
        delete m_circles;
        m_circles = nullptr;

        m_solidCircles.Destroy();
        delete m_solidCircles;
        m_solidCircles = nullptr;

        m_solidCapsules.Destroy();
        delete m_solidCapsules;
        m_solidCapsules = nullptr;

        m_solidPolygons.Destroy();
        delete m_solidPolygons;
        m_solidPolygons = nullptr;
    }

    public void DrawPolygon( const b2Vec2* vertices,  int vertexCount, b2HexColor color )
    {
        b2Vec2 p1 = vertices[vertexCount - 1];
        for (int i = 0; i < vertexCount; ++i)
        {
            b2Vec2 p2 = vertices[i];
            m_lines.AddLine(p1, p2, color);
            p1 = p2;
        }
    }

    public void DrawSolidPolygon(b2Transform transform,  const b2Vec2* vertices,  int vertexCount, float radius, b2HexColor color )
    {
        m_solidPolygons.AddPolygon(transform, vertices, vertexCount, radius, color);
    }

    public void DrawCircle(b2Vec2 center, float radius, b2HexColor color)
    {
        m_circles.AddCircle(center, radius, color);
    }

    public void DrawSolidCircle(b2Transform transform, b2Vec2 center, float radius, b2HexColor color)
    {
        transform.p = b2TransformPoint(transform, center);
        m_solidCircles.AddCircle(transform, radius, color);
    }

    public void DrawSolidCapsule(b2Vec2 p1, b2Vec2 p2, float radius, b2HexColor color)
    {
        m_solidCapsules.AddCapsule(p1, p2, radius, color);
    }

    public void DrawSegment(b2Vec2 p1, b2Vec2 p2, b2HexColor color)
    {
        m_lines.AddLine(p1, p2, color);
    }

    public void DrawTransform(b2Transform transform)
    {
        const float k_axisScale = 0.2f;
        b2Vec2 p1 = transform.p;

        b2Vec2 p2 = b2MulAdd(p1, k_axisScale, b2Rot_GetXAxis(transform.q));
        m_lines.AddLine(p1, p2, b2HexColor.b2_colorRed);

        p2 = b2MulAdd(p1, k_axisScale, b2Rot_GetYAxis(transform.q));
        m_lines.AddLine(p1, p2, b2HexColor.b2_colorGreen);
    }

    public void DrawPoint(b2Vec2 p, float size, b2HexColor color)
    {
        m_points.AddPoint(p, size, color);
    }

    public void DrawString(int x, int y,  const char*  string, ...)
    {
        // if (m_showUI == false)
        //{
        //	return;
        // }

        va_list arg;
        va_start(arg, string);
        ImGui.Begin("Overlay", nullptr,
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoScrollbar);
        ImGui.PushFont(Draw.g_draw.m_regularFont);
        ImGui.SetCursorPos(new Vector2(float(x), float(y)));
        ImGui.TextColoredV(ImColor(230, 153, 153, 255), string, arg);
        ImGui.PopFont();
        ImGui.End();
        va_end(arg);
    }

    public void DrawString(b2Vec2 p,  const char*  string, ...)
    {
        b2Vec2 ps = Draw.g_camera.ConvertWorldToScreen(p);

        va_list arg;
        va_start(arg, string);
        ImGui.Begin("Overlay", nullptr,
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoScrollbar);
        ImGui.SetCursorPos(new Vector2(ps.x, ps.y));
        ImGui.TextColoredV(ImColor(230, 230, 230, 255), string, arg);
        ImGui.End();
        va_end(arg);
    }

    public void DrawAABB(b2AABB aabb, b2HexColor c)
    {
        b2Vec2 p1 = aabb.lowerBound;
        b2Vec2 p2 = { aabb.upperBound.x, aabb.lowerBound.y };
        b2Vec2 p3 = aabb.upperBound;
        b2Vec2 p4 = { aabb.lowerBound.x, aabb.upperBound.y };

        m_lines.AddLine(p1, p2, c);
        m_lines.AddLine(p2, p3, c);
        m_lines.AddLine(p3, p4, c);
        m_lines.AddLine(p4, p1, c);
    }

    public void Flush()
    {
        m_solidCircles.Flush();
        m_solidCapsules.Flush();
        m_solidPolygons.Flush();
        m_triangles.Flush();
        m_circles.Flush();
        m_lines.Flush();
        m_points.Flush();
        CheckErrorGL();
    }

    public void DrawBackground()
    {
        m_background.Draw();
    }
    
    public static void DrawPolygonFcn( const b2Vec2* vertices, int vertexCount, b2HexColor color, void* context )
    {
        static_cast<Draw*>( context ).DrawPolygon( vertices, vertexCount, color );
    }

    public static void DrawSolidPolygonFcn( b2Transform transform, const b2Vec2* vertices, int vertexCount, float radius, b2HexColor color,void* context )
    {
        static_cast<Draw*>( context ).DrawSolidPolygon( transform, vertices, vertexCount, radius, color );
    }

    public static void DrawCircleFcn( b2Vec2 center, float radius, b2HexColor color, void* context )
    {
        static_cast<Draw*>( context ).DrawCircle( center, radius, color );
    }

    public static void DrawSolidCircleFcn( b2Transform transform, float radius, b2HexColor color, void* context )
    {
        static_cast<Draw*>( context ).DrawSolidCircle( transform, b2Vec2_zero, radius, color );
    }

    public static void DrawSolidCapsuleFcn( b2Vec2 p1, b2Vec2 p2, float radius, b2HexColor color, void* context )
    {
        static_cast<Draw*>( context ).DrawSolidCapsule( p1, p2, radius, color );
    }

    public static void DrawSegmentFcn( b2Vec2 p1, b2Vec2 p2, b2HexColor color, void* context )
    {
        static_cast<Draw*>( context ).DrawSegment( p1, p2, color );
    }

    public static void DrawTransformFcn( b2Transform transform, void* context )
    {
        static_cast<Draw*>( context ).DrawTransform( transform );
    }

    public static void DrawPointFcn( b2Vec2 p, float size, b2HexColor color, void* context )
    {
        static_cast<Draw*>( context ).DrawPoint( p, size, color );
    }

    public static void DrawStringFcn( b2Vec2 p, const char* s, b2HexColor color, void* context )
    {
        static_cast<Draw*>( context ).DrawString( p, s );
    }

}