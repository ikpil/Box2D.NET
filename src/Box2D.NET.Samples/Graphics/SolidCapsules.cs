// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Box2D.NET.Samples.Helpers;
using Box2D.NET.Samples.Primitives;
using Serilog;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.Samples.Graphics.Cameras;

namespace Box2D.NET.Samples.Graphics;

// Draw capsules using SDF-based shader
public static class SolidCapsules
{
    private static readonly ILogger Logger = Log.ForContext(typeof(SolidCapsules));

    public const int e_batchSize = 2048;

    public static SolidCapsuleRender CreateSolidCapsule(GL gl)
    {
        var render = new SolidCapsuleRender();
        render.programId = gl.CreateProgramFromFiles("data/solid_capsule.vs", "data/solid_capsule.fs");

        render.projectionUniform = gl.GetUniformLocation(render.programId, "projectionMatrix");
        render.pixelScaleUniform = gl.GetUniformLocation(render.programId, "pixelScale");

        uint vertexAttribute = 0;
        uint transformInstance = 1;
        uint radiusInstance = 2;
        uint lengthInstance = 3;
        uint colorInstance = 4;

        // Generate
        gl.GenVertexArrays(render.vaoId);
        gl.GenBuffers(render.vboIds);

        gl.BindVertexArray(render.vaoId[0]);
        gl.EnableVertexAttribArray(vertexAttribute);
        gl.EnableVertexAttribArray(transformInstance);
        gl.EnableVertexAttribArray(radiusInstance);
        gl.EnableVertexAttribArray(lengthInstance);
        gl.EnableVertexAttribArray(colorInstance);

        // Vertex buffer for single quad
        float a = 1.1f;
        B2Vec2[] vertices = new B2Vec2[]
        {
            new B2Vec2(-a, -a),
            new B2Vec2(a, -a),
            new B2Vec2(-a, a),
            new B2Vec2(a, -a),
            new B2Vec2(a, a),
            new B2Vec2(-a, a),
        };
        gl.BindBuffer(GLEnum.ArrayBuffer, render.vboIds[0]);
        gl.BufferData<B2Vec2>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);
        gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

        // Capsule buffer
        gl.BindBuffer(GLEnum.ArrayBuffer, render.vboIds[1]);
        gl.BufferData<CapsuleData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<CapsuleData>.Size, null, GLEnum.DynamicDraw);

        gl.VertexAttribPointer(transformInstance, 4, VertexAttribPointerType.Float, false, SizeOf<CapsuleData>.Size, IntPtr.Zero);
        gl.VertexAttribPointer(radiusInstance, 1, VertexAttribPointerType.Float, false, SizeOf<CapsuleData>.Size, IntPtr.Zero + 16);
        gl.VertexAttribPointer(lengthInstance, 1, VertexAttribPointerType.Float, false, SizeOf<CapsuleData>.Size, IntPtr.Zero + 20);
        gl.VertexAttribPointer(colorInstance, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<CapsuleData>.Size, IntPtr.Zero + 24);

        gl.VertexAttribDivisor(transformInstance, 1);
        gl.VertexAttribDivisor(radiusInstance, 1);
        gl.VertexAttribDivisor(lengthInstance, 1);
        gl.VertexAttribDivisor(colorInstance, 1);

        gl.CheckOpenGL();

        // Cleanup
        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);

        return render;
    }

    public static void DestroyCapsules(GL gl, ref SolidCapsuleRender render)
    {
        if (0 != render.vaoId[0])
        {
            gl.DeleteVertexArrays(render.vaoId);
            gl.DeleteBuffers(render.vboIds);
            render.vaoId[0] = 0;
            render.vboIds[0] = 0;
            render.vboIds[1] = 0;
        }

        if (0 != render.programId)
        {
            gl.DeleteProgram(render.programId);
            render.programId = 0;
        }
    }

    public static void AddCapsule(ref SolidCapsuleRender render, B2Vec2 p1, B2Vec2 p2, float radius, B2HexColor c)
    {
        B2Vec2 d = p2 - p1;
        float length = b2Length(d);
        if (length < 0.001f)
        {
            Logger.Information("WARNING: sample app: capsule too short!\n");
            return;
        }

        B2Vec2 axis = new B2Vec2(d.X / length, d.Y / length);
        B2Transform transform;
        transform.p = 0.5f * (p1 + p2);
        transform.q.c = axis.X;
        transform.q.s = axis.Y;

        RGBA8 rgba = RGBA8.MakeRGBA8(c, 1.0f);

        render.capsules.Add(new CapsuleData(transform, radius, length, rgba));
    }

    public static void FlushCapsules(GL gl, ref SolidCapsuleRender render, Camera camera)
    {
        int count = render.capsules.Count;
        if (count == 0)
        {
            return;
        }

        gl.UseProgram(render.programId);

        B2FixedArray16<float> array16 = new B2FixedArray16<float>();
        Span<float> proj = array16.AsSpan();

        BuildProjectionMatrix(camera, proj, 0.2f);

        gl.UniformMatrix4(render.projectionUniform, 1, false, proj);
        gl.Uniform1(render.pixelScaleUniform, camera.height / camera.zoom);

        gl.BindVertexArray(render.vaoId[0]);

        gl.BindBuffer(GLEnum.ArrayBuffer, render.vboIds[1]);
        gl.Enable(GLEnum.Blend);
        gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        var capsules = CollectionsMarshal.AsSpan(render.capsules);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            gl.BufferSubData<CapsuleData>(GLEnum.ArrayBuffer, 0, capsules.Slice(@base, batchCount));
            gl.DrawArraysInstanced(GLEnum.Triangles, 0, 6, (uint)batchCount);

            gl.CheckOpenGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        gl.Disable(GLEnum.Blend);

        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);
        gl.UseProgram(0);

        render.capsules.Clear();
    }
}