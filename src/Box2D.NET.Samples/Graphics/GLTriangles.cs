// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Helpers;
using Box2D.NET.Samples.Primitives;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Graphics;

// todo this is not used anymore and has untested changes
public class GLTriangles
{
    // must be multiple of 3
    public const int e_batchSize = 3 * 512;

    private List<VertexData> m_points = new List<VertexData>();

    private uint[] m_vaoId = new uint[1];
    private uint[] m_vboId = new uint[1];
    private uint m_programId;
    private int m_projectionUniform;

    public void Create()
    {
        string vs = "#version 330\n" +
                    "uniform mat4 projectionMatrix;\n" +
                    "layout(location = 0) in vec2 v_position;\n" +
                    "layout(location = 1) in vec4 v_color;\n" +
                    "out vec4 f_color;\n" +
                    "void main(void)\n" +
                    "{\n" +
                    "	f_color = v_color;\n" +
                    "	gl_Position = projectionMatrix * vec4(v_position, 0.0f, 1.0f);\n" +
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
        uint colorAttribute = 1;

        // Generate
        B2.g_shader.gl.GenVertexArrays(m_vaoId);
        B2.g_shader.gl.GenBuffers(m_vboId);

        B2.g_shader.gl.BindVertexArray(m_vaoId[0]);
        B2.g_shader.gl.EnableVertexAttribArray(vertexAttribute);
        B2.g_shader.gl.EnableVertexAttribArray(colorAttribute);

        // Vertex buffer
        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId[0]);
        B2.g_shader.gl.BufferData<VertexData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<VertexData>.Size, null, GLEnum.DynamicDraw);

        B2.g_shader.gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, SizeOf<VertexData>.Size, IntPtr.Zero);
        // color will get automatically expanded to floats in the shader
        B2.g_shader.gl.VertexAttribPointer(colorAttribute, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<VertexData>.Size, IntPtr.Zero + 8);

        B2.g_shader.CheckErrorGL();

        // Cleanup
        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.gl.BindVertexArray(0);
    }

    public void Destroy()
    {
        if (0 != m_vaoId[0])
        {
            B2.g_shader.gl.DeleteVertexArrays(1, m_vaoId);
            B2.g_shader.gl.DeleteBuffers(1, m_vboId);
            m_vaoId[0] = 0;
            m_vboId[0] = 0;
        }

        if (0 != m_programId)
        {
            B2.g_shader.gl.DeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    public void AddTriangle(B2Vec2 p1, B2Vec2 p2, B2Vec2 p3, B2HexColor c)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(c, 1.0f);
        m_points.Add(new VertexData(p1, rgba));
        m_points.Add(new VertexData(p2, rgba));
        m_points.Add(new VertexData(p3, rgba));
    }

    public void Flush()
    {
        int count = (int)m_points.Count;
        if (count == 0)
        {
            return;
        }

        Debug.Assert(count % 3 == 0);

        B2.g_shader.gl.UseProgram(m_programId);

        float[] proj = new float[16];
        B2.g_camera.BuildProjectionMatrix(proj, 0.2f);

        B2.g_shader.gl.UniformMatrix4(m_projectionUniform, 1, false, proj);

        B2.g_shader.gl.BindVertexArray(m_vaoId[0]);

        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId[0]);
        B2.g_shader.gl.Enable(GLEnum.Blend);
        B2.g_shader.gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        var points = CollectionsMarshal.AsSpan(m_points);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            B2.g_shader.gl.BufferSubData<VertexData>(GLEnum.ArrayBuffer, 0, points.Slice(@base, batchCount));
            B2.g_shader.gl.DrawArrays(GLEnum.Triangles, 0, (uint)batchCount);

            B2.g_shader.CheckErrorGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        B2.g_shader.gl.Disable(GLEnum.Blend);

        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.gl.BindVertexArray(0);
        B2.g_shader.gl.UseProgram(0);

        m_points.Clear();
    }
}