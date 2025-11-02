// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Box2D.NET.Samples.Graphics;
using ImGuiNET;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using static Box2D.NET.B2Types;
using static Box2D.NET.Samples.Graphics.Backgrounds;
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples;

public class SampleContext
{
    //
    public readonly string Signature;
    public readonly Glfw glfw;
    public readonly Camera camera;
    public Draw draw;
    
    public float uiScale = 1.0f;
    public float hertz = 60.0f;
    public int subStepCount = 4;
    public int workerCount = 1;
    public bool restart = false;
    public bool pause = false;
    public bool singleStep = false;
    public bool drawCounters = false;
    public bool drawProfile = false;
    public bool enableWarmStarting = true;
    public bool enableContinuous = true;
    public bool enableSleep = true;
    public bool showUI = true;
    
    // These are persisted
    public int sampleIndex = 0;
 

    public B2DebugDraw debugDraw;

    public ImFontPtr m_regularFont;
    public ImFontPtr m_mediumFont;
    public ImFontPtr m_largeFont;

    //
    public GL gl;
    public unsafe WindowHandle* window;

    private static string CreateSignature(string member, string file, int line)
    {
        return $"{member}() {Path.GetFileName(file)}:{line}";
    }

    private SampleContext(string signature, Glfw glfw)
    {
        Signature = signature;
        this.glfw = glfw;
        camera = new Camera();
        draw = new Draw();

        showUI = true;

        B2AABB bounds = new B2AABB(new B2Vec2(-float.MaxValue, -float.MaxValue), new B2Vec2(float.MaxValue, float.MaxValue));

        debugDraw = b2DefaultDebugDraw();
        debugDraw.DrawPolygonFcn = DrawPolygonFcn;
        debugDraw.DrawSolidPolygonFcn = DrawSolidPolygonFcn;
        debugDraw.DrawCircleFcn = DrawCircleFcn;
        debugDraw.DrawSolidCircleFcn = DrawSolidCircleFcn;
        debugDraw.DrawSolidCapsuleFcn = DrawSolidCapsuleFcn;
        debugDraw.drawLineFcn = DrawLineFcn;
        debugDraw.DrawTransformFcn = DrawTransformFcn;
        debugDraw.DrawPointFcn = DrawPointFcn;
        debugDraw.DrawStringFcn = DrawStringFcn;
        debugDraw.drawingBounds = bounds;


        debugDraw.context = this;

        m_mediumFont = default;
        m_largeFont = default;
        m_regularFont = default;
    }

    public void Load()
    {
        var settings = Settings.Load();
        
        //
        camera.width = settings.windowWidth;
        camera.height = settings.windowHeight;

        //
        uiScale = settings.uiScale;
        hertz = settings.hertz;
        subStepCount = settings.subStepCount;
        workerCount = settings.workerCount;

        //
        sampleIndex = settings.sampleIndex;
        debugDraw.drawShapes = settings.drawShapes;
        debugDraw.drawJoints = settings.drawJoints;
        
        //
        debugDraw.drawShapes = settings.drawShapes;
        debugDraw.drawJoints = settings.drawJoints;
        debugDraw.drawJointExtras = settings.drawJointExtras;
        debugDraw.drawBounds = settings.drawBounds;
        debugDraw.drawMass = settings.drawMass;
        debugDraw.drawContactPoints = settings.drawContactPoints;
        debugDraw.drawGraphColors = settings.drawGraphColors;
        debugDraw.drawContactNormals = settings.drawContactNormals;
        debugDraw.drawContactForces = settings.drawContactForces;
        debugDraw.drawContactFeatures = settings.drawContactFeatures;
        debugDraw.drawFrictionForces = settings.drawFrictionForces;
        debugDraw.drawIslands = settings.drawIslands;
        
        //
        debugDraw.jointScale = settings.jointScale;
        debugDraw.forceScale = settings.forceScale;
    }


    public static SampleContext Create([CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        // for windows - https://learn.microsoft.com/ko-kr/cpp/windows/latest-supported-vc-redist
        var glfw = Glfw.GetApi();
        var sig = CreateSignature(member, file, line);
        return CreateFor(sig, glfw);
    }

    public static SampleContext CreateWithoutGLFW([CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        var sig = CreateSignature(member, file, line);
        return CreateFor(sig, null);
    }

    public static SampleContext CreateFor(string sig, Glfw glfw)
    {
        var context = new SampleContext(sig, glfw);
        return context;
    }

    public static void DrawBackground(Draw draw, Camera camera)
    {
        RenderBackground(draw.glfw, draw.gl, ref draw.background, camera);
    }

    public static void DrawPolygonFcn(ReadOnlySpan<B2Vec2> vertices, int vertexCount, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)context;
        DrawPolygon(sampleContext.draw, vertices, vertexCount, color);
    }

    public static void DrawSolidPolygonFcn(ref B2Transform transform, ReadOnlySpan<B2Vec2> vertices, int vertexCount, float radius, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)context;
        DrawSolidPolygon(sampleContext.draw, ref transform, vertices, vertexCount, radius, color);
    }

    public static void DrawCircleFcn(B2Vec2 center, float radius, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)context;
        DrawCircle(sampleContext.draw, center, radius, color);
    }

    public static void DrawSolidCircleFcn(ref B2Transform transform, float radius, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawSolidCircle(sampleContext.draw, transform, radius, color);
    }

    public static void DrawSolidCapsuleFcn(B2Vec2 p1, B2Vec2 p2, float radius, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawSolidCapsule(sampleContext.draw, p1, p2, radius, color);
    }

    public static void DrawLineFcn(B2Vec2 p1, B2Vec2 p2, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawLine(sampleContext.draw, p1, p2, color);
    }

    public static void DrawTransformFcn(B2Transform transform, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawTransform(sampleContext.draw, transform, 1.0f);
    }

    public static void DrawPointFcn(B2Vec2 p, float size, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawPoint(sampleContext.draw, p, size, color);
    }

    public static void DrawStringFcn(B2Vec2 p, string s, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawWorldString(sampleContext.draw, sampleContext.camera, p, color, s);
    }
}