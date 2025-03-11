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

public class GLCircles
{
    public const int e_batchSize = 2048;

    private Shader _shader;
    private GL _gl;
    private Camera _camera;
    private List<CircleData> m_circles = new List<CircleData>();

    private uint[] m_vaoId = new uint[1];
    private uint[] m_vboIds = new uint[2];
    private uint m_programId;
    private int m_projectionUniform;
    private int m_pixelScaleUniform;

    public GLCircles(SampleAppContext context)
    {
        _camera = context.camera;
        _gl = context.gl;
        _shader = context.shader;
    }

    public void Create()
    {
        m_programId = _shader.CreateProgramFromFiles("data/circle.vs", "data/circle.fs");
        m_projectionUniform = _gl.GetUniformLocation(m_programId, "projectionMatrix");
        m_pixelScaleUniform = _gl.GetUniformLocation(m_programId, "pixelScale");
        uint vertexAttribute = 0;
        uint positionInstance = 1;
        uint radiusInstance = 2;
        uint colorInstance = 3;

        // Generate
        _gl.GenVertexArrays(m_vaoId);
        _gl.GenBuffers(m_vboIds);

        _gl.BindVertexArray(m_vaoId[0]);
        _gl.EnableVertexAttribArray(vertexAttribute);
        _gl.EnableVertexAttribArray(positionInstance);
        _gl.EnableVertexAttribArray(radiusInstance);
        _gl.EnableVertexAttribArray(colorInstance);

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
        _gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[0]);
        _gl.BufferData<B2Vec2>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);
        _gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

        // Circle buffer
        _gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        _gl.BufferData<CircleData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<CircleData>.Size, null, GLEnum.DynamicDraw);

        _gl.VertexAttribPointer(positionInstance, 2, VertexAttribPointerType.Float, false, SizeOf<CircleData>.Size, IntPtr.Zero); // 8
        _gl.VertexAttribPointer(radiusInstance, 1, VertexAttribPointerType.Float, false, SizeOf<CircleData>.Size, IntPtr.Zero + 8); // 4
        _gl.VertexAttribPointer(colorInstance, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<CircleData>.Size, IntPtr.Zero + 12); // 4

        _gl.VertexAttribDivisor(positionInstance, 1);
        _gl.VertexAttribDivisor(radiusInstance, 1);
        _gl.VertexAttribDivisor(colorInstance, 1);

        _shader.CheckErrorGL();

        // Cleanup
        _gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
    }

    public void Destroy()
    {
        if (0 != m_vaoId[0])
        {
            _gl.DeleteVertexArrays(m_vaoId);
            _gl.DeleteBuffers(m_vboIds);
            m_vaoId[0] = 0;
            m_vboIds[0] = 0;
            m_vboIds[1] = 0;
        }

        if (0 != m_programId)
        {
            _gl.DeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    public void AddCircle(B2Vec2 center, float radius, B2HexColor color)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(color, 1.0f);
        m_circles.Add(new CircleData(center, radius, rgba));
    }

    public void Flush()
    {
        int count = m_circles.Count;
        if (count == 0)
        {
            return;
        }

        _gl.UseProgram(m_programId);

        float[] proj = new float[16];
        _camera.BuildProjectionMatrix(proj, 0.2f);

        _gl.UniformMatrix4(m_projectionUniform, 1, false, proj);
        _gl.Uniform1(m_pixelScaleUniform, _camera.m_height / _camera.m_zoom);

        _gl.BindVertexArray(m_vaoId[0]);

        _gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        _gl.Enable(GLEnum.Blend);
        _gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        var circles = CollectionsMarshal.AsSpan(m_circles);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            _gl.BufferSubData<CircleData>(GLEnum.ArrayBuffer, 0, circles.Slice(@base, batchCount));
            _gl.DrawArraysInstanced(GLEnum.Triangles, 0, 6, (uint)batchCount);

            _shader.CheckErrorGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        _gl.Disable(GLEnum.Blend);

        _gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
        _gl.UseProgram(0);

        m_circles.Clear();
    }
}