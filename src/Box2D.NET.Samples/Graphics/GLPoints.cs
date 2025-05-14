﻿// SPDX-FileCopyrightText: 2025 Erin Catto
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

    private GL _gl;
    private List<PointData> m_points = new List<PointData>();

    private uint[] m_vaoId = new uint[1];
    private uint[] m_vboId = new uint[1];
    private uint m_programId;
    private int m_projectionUniform;

    public void Create(SampleContext context)
    {
        _gl = context.gl;

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

        m_programId = _gl.CreateProgramFromStrings(vs, fs);
        m_projectionUniform = _gl.GetUniformLocation(m_programId, "projectionMatrix");
        uint vertexAttribute = 0;
        uint sizeAttribute = 1;
        uint colorAttribute = 2;

        // Generate
        _gl.GenVertexArrays(m_vaoId);
        _gl.GenBuffers(m_vboId);

        _gl.BindVertexArray(m_vaoId[0]);
        _gl.EnableVertexAttribArray(vertexAttribute);
        _gl.EnableVertexAttribArray(sizeAttribute);
        _gl.EnableVertexAttribArray(colorAttribute);

        // Vertex buffer
        _gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId[0]);
        _gl.BufferData<PointData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<PointData>.Size, null, GLEnum.DynamicDraw);

        _gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, SizeOf<PointData>.Size, IntPtr.Zero);
        _gl.VertexAttribPointer(sizeAttribute, 1, VertexAttribPointerType.Float, false, SizeOf<PointData>.Size, IntPtr.Zero + 8);
        // save bandwidth by expanding color to floats in the shader
        _gl.VertexAttribPointer(colorAttribute, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<PointData>.Size, IntPtr.Zero + 12);

        _gl.CheckOpenGL();

        // Cleanup
        _gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
    }

    public void Destroy()
    {
        if (0 != m_vaoId[0])
        {
            _gl.DeleteVertexArrays(m_vaoId);
            _gl.DeleteBuffers(m_vboId);
            m_vaoId[0] = 0;
            m_vboId[0] = 0;
        }

        if (0 != m_programId)
        {
            _gl.DeleteProgram(m_programId);
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

    public void Flush(Camera camera)
    {
        int count = m_points.Count;
        if (count == 0)
        {
            return;
        }

        _gl.UseProgram(m_programId);

        B2FixedArray16<float> array16 = new B2FixedArray16<float>();
        Span<float> proj = array16.AsSpan();

        camera.BuildProjectionMatrix(proj, 0.0f);

        _gl.UniformMatrix4(m_projectionUniform, 1, false, proj);
        _gl.BindVertexArray(m_vaoId[0]);

        _gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId[0]);
        _gl.Enable(GLEnum.ProgramPointSize);

        var points = CollectionsMarshal.AsSpan(m_points);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);
            _gl.BufferSubData<PointData>(GLEnum.ArrayBuffer, 0, points.Slice(@base, batchCount));
            _gl.DrawArrays(GLEnum.Points, 0, (uint)batchCount);

            _gl.CheckOpenGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        _gl.Disable(GLEnum.ProgramPointSize);
        _gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
        _gl.UseProgram(0);

        m_points.Clear();
    }
}