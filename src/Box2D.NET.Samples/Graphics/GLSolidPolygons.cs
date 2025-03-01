// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Helpers;
using Box2D.NET.Samples.Primitives;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Graphics;

// Rounded and non-rounded convex polygons using an SDF-based shader.
public class GLSolidPolygons
{
    public const int e_batchSize = 512;

    private List<PolygonData> m_polygons = new List<PolygonData>();

    uint[] m_vaoId = new uint[1];
    uint[] m_vboIds = new uint[2];
    uint m_programId;
    int m_projectionUniform;
    int m_pixelScaleUniform;

    public void Create()
    {
        m_programId = B2.g_shader.CreateProgramFromFiles("data/solid_polygon.vs", "data/solid_polygon.fs");

        m_projectionUniform = B2.g_shader.gl.GetUniformLocation(m_programId, "projectionMatrix");
        m_pixelScaleUniform = B2.g_shader.gl.GetUniformLocation(m_programId, "pixelScale");
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
        B2.g_shader.gl.GenVertexArrays(m_vaoId);
        B2.g_shader.gl.GenBuffers(m_vboIds);

        B2.g_shader.gl.BindVertexArray(m_vaoId[0]);
        B2.g_shader.gl.EnableVertexAttribArray(vertexAttribute);
        B2.g_shader.gl.EnableVertexAttribArray(instanceTransform);
        B2.g_shader.gl.EnableVertexAttribArray(instancePoint12);
        B2.g_shader.gl.EnableVertexAttribArray(instancePoint34);
        B2.g_shader.gl.EnableVertexAttribArray(instancePoint56);
        B2.g_shader.gl.EnableVertexAttribArray(instancePoint78);
        B2.g_shader.gl.EnableVertexAttribArray(instancePointCount);
        B2.g_shader.gl.EnableVertexAttribArray(instanceRadius);
        B2.g_shader.gl.EnableVertexAttribArray(instanceColor);

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
        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[0]);
        B2.g_shader.gl.BufferData<B2Vec2>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);
        B2.g_shader.gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

        // Polygon buffer
        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        B2.g_shader.gl.BufferData<PolygonData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<PolygonData>.Size, null, GLEnum.DynamicDraw);
        B2.g_shader.gl.VertexAttribPointer(instanceTransform, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero);
        B2.g_shader.gl.VertexAttribPointer(instancePoint12, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 16);
        B2.g_shader.gl.VertexAttribPointer(instancePoint34, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 32);
        B2.g_shader.gl.VertexAttribPointer(instancePoint56, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 48);
        B2.g_shader.gl.VertexAttribPointer(instancePoint78, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 64);
        B2.g_shader.gl.VertexAttribIPointer(instancePointCount, 1, VertexAttribIType.Int, SizeOf<PolygonData>.Size, IntPtr.Zero + 80);
        B2.g_shader.gl.VertexAttribPointer(instanceRadius, 1, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 84);
        // color will get automatically expanded to floats in the shader
        B2.g_shader.gl.VertexAttribPointer(instanceColor, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<PolygonData>.Size, IntPtr.Zero + 88);

        // These divisors tell glsl how to distribute per instance data
        B2.g_shader.gl.VertexAttribDivisor(instanceTransform, 1);
        B2.g_shader.gl.VertexAttribDivisor(instancePoint12, 1);
        B2.g_shader.gl.VertexAttribDivisor(instancePoint34, 1);
        B2.g_shader.gl.VertexAttribDivisor(instancePoint56, 1);
        B2.g_shader.gl.VertexAttribDivisor(instancePoint78, 1);
        B2.g_shader.gl.VertexAttribDivisor(instancePointCount, 1);
        B2.g_shader.gl.VertexAttribDivisor(instanceRadius, 1);
        B2.g_shader.gl.VertexAttribDivisor(instanceColor, 1);

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
            B2.g_shader.gl.DeleteBuffers(2, m_vboIds);
            m_vaoId[0] = 0;
        }

        if (0 != m_programId)
        {
            B2.g_shader.gl.DeleteProgram(m_programId);
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

        B2.g_shader.gl.UseProgram(m_programId);
            B2.g_shader.CheckErrorGL();

        float[] proj = new float[16];
        B2.g_camera.BuildProjectionMatrix(proj, 0.2f);

        B2.g_shader.gl.UniformMatrix4(m_projectionUniform, 1, false, proj);
        B2.g_shader.gl.Uniform1(m_pixelScaleUniform, B2.g_camera.m_height / B2.g_camera.m_zoom);

        B2.g_shader.gl.BindVertexArray(m_vaoId[0]);
        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);

        B2.g_shader.gl.Enable(GLEnum.Blend);
        B2.g_shader.gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        var polygons = CollectionsMarshal.AsSpan(m_polygons);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            B2.g_shader.gl.BufferSubData<PolygonData>(GLEnum.ArrayBuffer, 0, polygons.Slice(@base, batchCount));
            B2.g_shader.gl.DrawArraysInstanced(GLEnum.Triangles, 0, 6, (uint)batchCount);
            B2.g_shader.CheckErrorGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        B2.g_shader.gl.Disable(GLEnum.Blend);

        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.gl.BindVertexArray(0);
        B2.g_shader.gl.UseProgram(0);

        m_polygons.Clear();
    }
}