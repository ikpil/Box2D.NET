// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Box2D.NET.Samples.Helpers;
using Box2D.NET.Samples.Primitives;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.Samples.Graphics.Cameras;

namespace Box2D.NET.Samples.Graphics;

// Draws SDF circles using quad instancing. Apparently instancing of quads can be slow on older GPUs.
// https://www.reddit.com/r/opengl/comments/q7yikr/how_to_draw_several_quads_through_instancing/
// https://www.g-truc.net/post-0666.html
public static class SolidCircles
{
    public const int e_batchSize = 2048;

    public static SolidCircleRender CreateSolidCircles(GL gl)
    {
        var render = new SolidCircleRender();
        render.programId = gl.CreateProgramFromFiles("data/solid_circle.vs", "data/solid_circle.fs");
        render.projectionUniform = gl.GetUniformLocation(render.programId, "projectionMatrix");
        render.pixelScaleUniform = gl.GetUniformLocation(render.programId, "pixelScale");

        // Generate
        gl.GenVertexArrays(render.vaoId);
        gl.GenBuffers(render.vboIds);

        gl.BindVertexArray(render.vaoId[0]);

        uint vertexAttribute = 0;
        uint transformInstance = 1;
        uint radiusInstance = 2;
        uint colorInstance = 3;
        gl.EnableVertexAttribArray(vertexAttribute);
        gl.EnableVertexAttribArray(transformInstance);
        gl.EnableVertexAttribArray(radiusInstance);
        gl.EnableVertexAttribArray(colorInstance);

        // Vertex buffer for single quad
        float a = 1.1f;
        B2Vec2[] vertices =
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

        // Circle buffer
        gl.BindBuffer(GLEnum.ArrayBuffer, render.vboIds[1]);
        gl.BufferData<SolidCircleData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<SolidCircleData>.Size, null, GLEnum.DynamicDraw);

        gl.VertexAttribPointer(transformInstance, 4, VertexAttribPointerType.Float, false, SizeOf<SolidCircleData>.Size, IntPtr.Zero);
        gl.VertexAttribPointer(radiusInstance, 1, VertexAttribPointerType.Float, false, SizeOf<SolidCircleData>.Size, IntPtr.Zero + 16);
        gl.VertexAttribPointer(colorInstance, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<SolidCircleData>.Size, IntPtr.Zero + 20);

        gl.VertexAttribDivisor(transformInstance, 1);
        gl.VertexAttribDivisor(radiusInstance, 1);
        gl.VertexAttribDivisor(colorInstance, 1);

        gl.CheckOpenGL();

        // Cleanup
        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);

        return render;
    }

    public static void DestroySolidCircles(GL gl, ref SolidCircleRender render)
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

    public static void AddSolidCircle(ref SolidCircleRender render, in B2Transform transform, float radius, B2HexColor color)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(color, 1.0f);
        render.circles.Add(new SolidCircleData(transform, radius, rgba));
    }

    public static void FlushSolidCircles(GL gl, ref SolidCircleRender render, Camera camera)
    {
        int count = (int)render.circles.Count;
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

        var circles = CollectionsMarshal.AsSpan(render.circles);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            gl.BufferSubData<SolidCircleData>(GLEnum.ArrayBuffer, 0, circles.Slice(@base, batchCount));
            gl.DrawArraysInstanced(GLEnum.Triangles, 0, 6, (uint)batchCount);

            gl.CheckOpenGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        gl.Disable(GLEnum.Blend);

        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);
        gl.UseProgram(0);

        render.circles.Clear();
    }
}