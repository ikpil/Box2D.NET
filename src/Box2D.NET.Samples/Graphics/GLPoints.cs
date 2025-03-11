// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Box2D.NET.Samples.Helpers;
using Box2D.NET.Samples.Primitives;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Graphics;

public class GLPoints
{
    public const int e_batchSize = 2048;

    private Camera _camera;
    private List<PointData> m_points = new List<PointData>();

    private uint[] m_vaoId = new uint[1];
    private uint[] m_vboId = new uint[1];
    private uint m_programId;
    private int m_projectionUniform;

    public GLPoints(SampleAppContext context)
    {
        _camera = context.camera;
    }


    public void Create()
    {
        string vs = "#version 330\n" +
                    "uniform mat4 projectionMatrix;\n" +
                    "layout(location = 0) in vec2 v_position;\n" +
                    "layout(location = 1) in float v_size;\n" +
                    "layout(location = 2) in vec4 v_color;\n" +
                    "out vec4 f_color;\n" +
                    "void main(void)\n" +
                    "{\n" +
                    "	f_color = v_color;\n" +
                    "	gl_Position = projectionMatrix * vec4(v_position, 0.0f, 1.0f);\n" +
                    "	gl_PointSize = v_size;\n" +
                    "}\n";

        string fs = "#version 330\n" +
                    "in vec4 f_color;\n" +
                    "out vec4 color;\n" +
                    "void main(void)\n" +
                    "{\n" +
                    "	color = f_color;\n" +
                    "}\n";

        m_programId = B2.g_shader.CreateProgramFromStrings(vs, fs);
        m_projectionUniform = B2.g_shader.gl.GetUniformLocation(m_programId, "projectionMatrix");
        uint vertexAttribute = 0;
        uint sizeAttribute = 1;
        uint colorAttribute = 2;

        // Generate
        B2.g_shader.gl.GenVertexArrays(m_vaoId);
        B2.g_shader.gl.GenBuffers(m_vboId);

        B2.g_shader.gl.BindVertexArray(m_vaoId[0]);
        B2.g_shader.gl.EnableVertexAttribArray(vertexAttribute);
        B2.g_shader.gl.EnableVertexAttribArray(sizeAttribute);
        B2.g_shader.gl.EnableVertexAttribArray(colorAttribute);

        // Vertex buffer
        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId[0]);
        B2.g_shader.gl.BufferData<PointData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<PointData>.Size, null, GLEnum.DynamicDraw);

        B2.g_shader.gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, SizeOf<PointData>.Size, IntPtr.Zero);
        B2.g_shader.gl.VertexAttribPointer(sizeAttribute, 1, VertexAttribPointerType.Float, false, SizeOf<PointData>.Size, IntPtr.Zero + 8);
        // save bandwidth by expanding color to floats in the shader
        B2.g_shader.gl.VertexAttribPointer(colorAttribute, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<PointData>.Size, IntPtr.Zero + 12);

        B2.g_shader.CheckErrorGL();

        // Cleanup
        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.gl.BindVertexArray(0);
    }

    public void Destroy()
    {
        if (0 != m_vaoId[0])
        {
            B2.g_shader.gl.DeleteVertexArrays(m_vaoId);
            B2.g_shader.gl.DeleteBuffers(m_vboId);
            m_vaoId[0] = 0;
            m_vboId[0] = 0;
        }

        if (0 != m_programId)
        {
            B2.g_shader.gl.DeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    // todo instead of flushing, keep a growable array of data
    // this will prevent sorting problems.

    public void AddPoint(B2Vec2 v, float size, B2HexColor c)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(c, 1.0f);
        m_points.Add(new PointData(v, size, rgba));
    }

    public void Flush()
    {
        int count = m_points.Count;
        if (count == 0)
        {
            return;
        }

        B2.g_shader.gl.UseProgram(m_programId);

        float[] proj = new float[16];
        _camera.BuildProjectionMatrix(proj, 0.0f);

        B2.g_shader.gl.UniformMatrix4(m_projectionUniform, 1, false, proj);
        B2.g_shader.gl.BindVertexArray(m_vaoId[0]);

        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId[0]);
        B2.g_shader.gl.Enable(GLEnum.ProgramPointSize);

        var points = CollectionsMarshal.AsSpan(m_points);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);
            B2.g_shader.gl.BufferSubData<PointData>(GLEnum.ArrayBuffer, 0, points.Slice(@base, batchCount));
            B2.g_shader.gl.DrawArrays(GLEnum.Points, 0, (uint)batchCount);

            B2.g_shader.CheckErrorGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        B2.g_shader.gl.Disable(GLEnum.ProgramPointSize);
        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.gl.BindVertexArray(0);
        B2.g_shader.gl.UseProgram(0);

        m_points.Clear();
    }
}