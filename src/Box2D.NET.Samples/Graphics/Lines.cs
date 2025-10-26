// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Box2D.NET.Samples.Helpers;
using Box2D.NET.Samples.Primitives;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.Samples.Graphics.Cameras;

namespace Box2D.NET.Samples.Graphics;

public static class Lines
{
    // need lots of space for lines so they draw last
    // could also consider disabling depth buffer
    // must be multiple of 2
    public const int e_batchSize = 2 * 2048;

    public static LineRender CreateLineRender(GL gl)
    {
        var render = new LineRender();
        render.m_programId = gl.CreateProgramFromFiles("data/line.vs", "data/line.fs");
        render.m_projectionUniform = gl.GetUniformLocation(render.m_programId, "projectionMatrix");
        uint vertexAttribute = 0;
        uint colorAttribute = 1;

        // Generate
        gl.GenVertexArrays(render.m_vaoId);
        gl.GenBuffers(render.m_vboId);

        gl.BindVertexArray(render.m_vaoId[0]);
        gl.EnableVertexAttribArray(vertexAttribute);
        gl.EnableVertexAttribArray(colorAttribute);

        // Vertex buffer
        gl.BindBuffer(GLEnum.ArrayBuffer, render.m_vboId[0]);
        gl.BufferData<VertexData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<VertexData>.Size, null, GLEnum.DynamicDraw);

        gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, SizeOf<VertexData>.Size, IntPtr.Zero);
        // save bandwidth by expanding color to floats in the shader
        gl.VertexAttribPointer(colorAttribute, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<VertexData>.Size, IntPtr.Zero + 8);

        gl.CheckOpenGL();

        // Cleanup
        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);

        return render;
    }

    public static void DestroyLineRender(GL gl, ref LineRender render)
    {
        if (0 != render.m_vaoId[0])
        {
            gl.DeleteVertexArrays(render.m_vaoId);
            gl.DeleteBuffers(render.m_vboId);
            render.m_vaoId[0] = 0;
            render.m_vboId[0] = 0;
        }

        if (0 != render.m_programId)
        {
            gl.DeleteProgram(render.m_programId);
            render.m_programId = 0;
        }
    }

    public static void AddLine(ref LineRender render, B2Vec2 p1, B2Vec2 p2, B2HexColor c)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(c, 1.0f);
        render.m_points.Add(new VertexData(p1, rgba));
        render.m_points.Add(new VertexData(p2, rgba));
    }

    public static void FlushLines(GL gl, ref LineRender render, Camera camera)
    {
        int count = render.m_points.Count;
        if (count == 0)
        {
            return;
        }

        B2_ASSERT(count % 2 == 0);

        gl.Enable(GLEnum.Blend);
        gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        gl.UseProgram(render.m_programId);

        B2FixedArray16<float> array16 = new B2FixedArray16<float>();
        Span<float> proj = array16.AsSpan();

        BuildProjectionMatrix(camera, proj, 0.1f);

        gl.UniformMatrix4(render.m_projectionUniform, 1, false, proj);

        gl.BindVertexArray(render.m_vaoId[0]);

        gl.BindBuffer(GLEnum.ArrayBuffer, render.m_vboId[0]);

        var points = CollectionsMarshal.AsSpan(render.m_points);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);
            gl.BufferSubData<VertexData>(GLEnum.ArrayBuffer, 0, points.Slice(@base, batchCount));

            gl.DrawArrays(GLEnum.Lines, 0, (uint)batchCount);

            gl.CheckOpenGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);
        gl.UseProgram(0);

        gl.Disable(GLEnum.Blend);

        render.m_points.Clear();
    }
}