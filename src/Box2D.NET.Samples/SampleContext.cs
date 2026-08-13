// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Box2D.NET.Samples.Graphics;
using Box2D.NET.Samples.Samples;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Constants;
using static Box2D.NET.Samples.Graphics.Backgrounds;
using static Box2D.NET.Samples.Graphics.Cameras;
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples;

public class SampleContext
{
    //
    public readonly string Signature;
    public readonly Glfw glfw;
    public GL gl;
    public unsafe WindowHandle* window;
    public readonly Camera camera;
    public Draw draw;
    public Sample sample = null;
    public B2Capacity capacity;
    public B2DebugDraw debugDraw;

    public float uiScale = 1.0f;
    public float hertz = 60.0f;
    public float recycleDistance = 0.05f;
    public int subStepCount = 4;
    public int workerCount = 1;
    public bool restart = false;
    public bool pause = false;
    public bool singleStep = false;
    public bool enableWarmStarting = true;
    public bool enableContinuous = true;
    public bool enableSleep = true;
    public bool showUI = true;

    // Diagnostics drawer visibility. D toggles.
    public bool showMetrics = false;

    // Set by Ctrl+O; consumed by UpdateSampleUI to open the fuzzy sample picker.
    public bool openSamplePicker = false;

    // These are persisted
    public int sampleIndex = 0;

    private static string CreateSignature(string member, string file, int line)
    {
        return $"{member}() {Path.GetFileName(file)}:{line}";
    }

    private SampleContext(string signature, Glfw glfw)
    {
        Signature = signature;
        this.glfw = glfw;
        camera = GetDefaultCamera();
        draw = new Draw();

        showUI = true;

        B2AABB bounds = new B2AABB(new B2Vec2(-float.MaxValue, -float.MaxValue), new B2Vec2(float.MaxValue, float.MaxValue));

        debugDraw = b2DefaultDebugDraw();
        debugDraw.DrawPolygonFcn = DrawPolygonFcn;
        debugDraw.DrawSolidPolygonFcn = DrawSolidPolygonFcn;
        debugDraw.DrawCircleFcn = DrawCircleFcn;
        debugDraw.DrawSolidCircleFcn = DrawSolidCircleFcn;
        debugDraw.DrawSolidCapsuleFcn = DrawSolidCapsuleFcn;
        debugDraw.DrawLineFcn = DrawLineFcn;
        debugDraw.DrawTransformFcn = DrawTransformFcn;
        debugDraw.DrawPointFcn = DrawPointFcn;
        debugDraw.DrawStringFcn = DrawStringFcn;
        debugDraw.drawingBounds = bounds;


        debugDraw.context = this;

    }

    public void Load()
    {
        recycleDistance = B2_CONTACT_RECYCLE_DISTANCE;

        var settings = Settings.Load();

        //
        sampleIndex = settings.sampleIndex;
        debugDraw.drawShapes = settings.drawShapes;
        debugDraw.drawJoints = settings.drawJoints;
        showMetrics = settings.showDiagnostics;
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

    public static void DrawSolidPolygonFcn(in B2Transform transform, ReadOnlySpan<B2Vec2> vertices, int vertexCount, float radius, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)context;
        DrawSolidPolygon(sampleContext.draw, transform, vertices, vertexCount, radius, color);
    }

    public static void DrawCircleFcn(in B2Vec2 center, float radius, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)context;
        DrawCircle(sampleContext.draw, center, radius, color);
    }

    public static void DrawSolidCircleFcn(in B2Transform transform, float radius, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawSolidCircle(sampleContext.draw, transform, radius, color);
    }

    public static void DrawSolidCapsuleFcn(in B2Vec2 p1, in B2Vec2 p2, float radius, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawSolidCapsule(sampleContext.draw, p1, p2, radius, color);
    }

    public static void DrawLineFcn(in B2Vec2 p1, in B2Vec2 p2, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawLine(sampleContext.draw, p1, p2, color);
    }

    public static void DrawTransformFcn(in B2Transform transform, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawTransform(sampleContext.draw, transform, 1.0f);
    }

    public static void DrawPointFcn(in B2Vec2 p, float size, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawPoint(sampleContext.draw, p, size, color);
    }

    public static void DrawStringFcn(in B2Vec2 p, string s, B2HexColor color, object context)
    {
        SampleContext sampleContext = (SampleContext)(context);
        DrawWorldString(sampleContext.draw, sampleContext.camera, p, color, s);
    }
}
