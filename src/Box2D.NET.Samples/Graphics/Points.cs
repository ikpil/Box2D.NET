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

public static class Points
{
    public const int e_batchSize = 2048;

    public static PointRender CreatePointDrawData(GL gl)
    {
        PointRender render = new PointRender();
        render.programId = gl.CreateProgramFromFiles("data/point.vs", "data/point.fs");
        render.projectionUniform = gl.GetUniformLocation(render.programId, "projectionMatrix");
        uint vertexAttribute = 0;
        uint sizeAttribute = 1;
        uint colorAttribute = 2;

        // Generate
        gl.GenVertexArrays(render.vaoId);
        gl.GenBuffers(render.vboId);

        gl.BindVertexArray(render.vaoId[0]);
        gl.EnableVertexAttribArray(vertexAttribute);
        gl.EnableVertexAttribArray(sizeAttribute);
        gl.EnableVertexAttribArray(colorAttribute);

        // Vertex buffer
        gl.BindBuffer(GLEnum.ArrayBuffer, render.vboId[0]);
        gl.BufferData<PointData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<PointData>.Size, null, GLEnum.DynamicDraw);

        gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, SizeOf<PointData>.Size, IntPtr.Zero);
        gl.VertexAttribPointer(sizeAttribute, 1, VertexAttribPointerType.Float, false, SizeOf<PointData>.Size, IntPtr.Zero + 8);
        // save bandwidth by expanding color to floats in the shader
        gl.VertexAttribPointer(colorAttribute, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<PointData>.Size, IntPtr.Zero + 12);

        gl.CheckOpenGL();

        // Cleanup
        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);

        return render;
    }

    public static void DestroyPointDrawData(GL gl, ref PointRender render)
    {
        if (0 != render.vaoId[0])
        {
            gl.DeleteVertexArrays(render.vaoId);
            gl.DeleteBuffers(render.vboId);
            render.vaoId[0] = 0;
            render.vboId[0] = 0;
        }

        if (0 != render.programId)
        {
            gl.DeleteProgram(render.programId);
            render.programId = 0;
        }
    }

    // todo instead of flushing, keep a growable array of data
    // this will prevent sorting problems.

    public static void AddPoint(ref PointRender render, B2Vec2 v, float size, B2HexColor c)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(c, 1.0f);
        render.points.Add(new PointData(v, size, rgba));
    }

    public static void FlushPoints(GL gl, ref PointRender render, Camera camera)
    {
        int count = render.points.Count;
        if (count == 0)
        {
            return;
        }

        gl.UseProgram(render.programId);

        B2FixedArray16<float> array16 = new B2FixedArray16<float>();
        Span<float> proj = array16.AsSpan();

        BuildProjectionMatrix(camera, proj, 0.0f);

        gl.UniformMatrix4(render.projectionUniform, 1, false, proj);
        gl.BindVertexArray(render.vaoId[0]);

        gl.BindBuffer(GLEnum.ArrayBuffer, render.vboId[0]);
        gl.Enable(GLEnum.ProgramPointSize);

        var points = CollectionsMarshal.AsSpan(render.points);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);
            gl.BufferSubData<PointData>(GLEnum.ArrayBuffer, 0, points.Slice(@base, batchCount));
            gl.DrawArrays(GLEnum.Points, 0, (uint)batchCount);

            gl.CheckOpenGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        gl.Disable(GLEnum.ProgramPointSize);
        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);
        gl.UseProgram(0);

        render.points.Clear();
    }
}