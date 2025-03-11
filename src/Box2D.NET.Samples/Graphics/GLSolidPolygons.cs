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

// Rounded and non-rounded convex polygons using an SDF-based shader.
public class GLSolidPolygons
{
    public const int e_batchSize = 512;

    private GL _gl;
    private Camera _camera;
    private List<PolygonData> m_polygons = new List<PolygonData>();

    uint[] m_vaoId = new uint[1];
    uint[] m_vboIds = new uint[2];
    uint m_programId;
    int m_projectionUniform;
    int m_pixelScaleUniform;

    public void Create(SampleAppContext context)
    {
        _camera = context.camera;
        _gl = context.gl;

        m_programId = _gl.CreateProgramFromFiles("data/solid_polygon.vs", "data/solid_polygon.fs");

        m_projectionUniform = _gl.GetUniformLocation(m_programId, "projectionMatrix");
        m_pixelScaleUniform = _gl.GetUniformLocation(m_programId, "pixelScale");
        uint vertexAttribute = 0;
        uint instanceTransform = 1;
        uint instancePoint12 = 2;
        uint instancePoint34 = 3;
        uint instancePoint56 = 4;
        uint instancePoint78 = 5;
        uint instancePointCount = 6;
        uint instanceRadius = 7;
        uint instanceColor = 8;

        // Generate
        _gl.GenVertexArrays(m_vaoId);
        _gl.GenBuffers(m_vboIds);

        _gl.BindVertexArray(m_vaoId[0]);
        _gl.EnableVertexAttribArray(vertexAttribute);
        _gl.EnableVertexAttribArray(instanceTransform);
        _gl.EnableVertexAttribArray(instancePoint12);
        _gl.EnableVertexAttribArray(instancePoint34);
        _gl.EnableVertexAttribArray(instancePoint56);
        _gl.EnableVertexAttribArray(instancePoint78);
        _gl.EnableVertexAttribArray(instancePointCount);
        _gl.EnableVertexAttribArray(instanceRadius);
        _gl.EnableVertexAttribArray(instanceColor);

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

        // Polygon buffer
        _gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        _gl.BufferData<PolygonData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<PolygonData>.Size, null, GLEnum.DynamicDraw);
        _gl.VertexAttribPointer(instanceTransform, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero);
        _gl.VertexAttribPointer(instancePoint12, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 16);
        _gl.VertexAttribPointer(instancePoint34, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 32);
        _gl.VertexAttribPointer(instancePoint56, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 48);
        _gl.VertexAttribPointer(instancePoint78, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 64);
        _gl.VertexAttribIPointer(instancePointCount, 1, VertexAttribIType.Int, SizeOf<PolygonData>.Size, IntPtr.Zero + 80);
        _gl.VertexAttribPointer(instanceRadius, 1, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 84);
        // color will get automatically expanded to floats in the shader
        _gl.VertexAttribPointer(instanceColor, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<PolygonData>.Size, IntPtr.Zero + 88);

        // These divisors tell glsl how to distribute per instance data
        _gl.VertexAttribDivisor(instanceTransform, 1);
        _gl.VertexAttribDivisor(instancePoint12, 1);
        _gl.VertexAttribDivisor(instancePoint34, 1);
        _gl.VertexAttribDivisor(instancePoint56, 1);
        _gl.VertexAttribDivisor(instancePoint78, 1);
        _gl.VertexAttribDivisor(instancePointCount, 1);
        _gl.VertexAttribDivisor(instanceRadius, 1);
        _gl.VertexAttribDivisor(instanceColor, 1);

        _gl.CheckErrorGL();

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
        }

        if (0 != m_programId)
        {
            _gl.DeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    public void AddPolygon(ref B2Transform transform, ReadOnlySpan<B2Vec2> points, int count, float radius, B2HexColor color)
    {
        PolygonData data = new PolygonData();
        data.transform = transform;

        int n = count < 8 ? count : 8;
        for (int i = 0; i < n; ++i)
        {
            data.points[i] = points[i];
        }

        data.count = n;
        data.radius = radius;
        data.color = RGBA8.MakeRGBA8(color, 1.0f);

        m_polygons.Add(data);
    }

    public void Flush()
    {
        int count = (int)m_polygons.Count;
        if (count == 0)
        {
            return;
        }

        _gl.UseProgram(m_programId);
        _gl.CheckErrorGL();

        float[] proj = new float[16];
        _camera.BuildProjectionMatrix(proj, 0.2f);

        _gl.UniformMatrix4(m_projectionUniform, 1, false, proj);
        _gl.Uniform1(m_pixelScaleUniform, _camera.m_height / _camera.m_zoom);

        _gl.BindVertexArray(m_vaoId[0]);
        _gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);

        _gl.Enable(GLEnum.Blend);
        _gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        var polygons = CollectionsMarshal.AsSpan(m_polygons);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            _gl.BufferSubData<PolygonData>(GLEnum.ArrayBuffer, 0, polygons.Slice(@base, batchCount));
            _gl.DrawArraysInstanced(GLEnum.Triangles, 0, 6, (uint)batchCount);
            _gl.CheckErrorGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        _gl.Disable(GLEnum.Blend);

        _gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
        _gl.UseProgram(0);

        m_polygons.Clear();
    }
}