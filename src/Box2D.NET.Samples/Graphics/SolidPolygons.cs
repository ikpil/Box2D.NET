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

// Rounded and non-rounded convex polygons using an SDF-based shader.
public static class SolidPolygons
{
    public const int e_batchSize = 512;

    public static SolidPolygonRender CreateSolidPolygons(GL gl)
    {
        var render = new SolidPolygonRender();
        render.programId = gl.CreateProgramFromFiles("data/solid_polygon.vs", "data/solid_polygon.fs");

        render.projectionUniform = gl.GetUniformLocation(render.programId, "projectionMatrix");
        render.pixelScaleUniform = gl.GetUniformLocation(render.programId, "pixelScale");
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
        gl.GenVertexArrays(render.vaoId);
        gl.GenBuffers(render.vboIds);

        gl.BindVertexArray(render.vaoId[0]);
        gl.EnableVertexAttribArray(vertexAttribute);
        gl.EnableVertexAttribArray(instanceTransform);
        gl.EnableVertexAttribArray(instancePoint12);
        gl.EnableVertexAttribArray(instancePoint34);
        gl.EnableVertexAttribArray(instancePoint56);
        gl.EnableVertexAttribArray(instancePoint78);
        gl.EnableVertexAttribArray(instancePointCount);
        gl.EnableVertexAttribArray(instanceRadius);
        gl.EnableVertexAttribArray(instanceColor);

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

        // Polygon buffer
        gl.BindBuffer(GLEnum.ArrayBuffer, render.vboIds[1]);
        gl.BufferData<PolygonData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<PolygonData>.Size, null, GLEnum.DynamicDraw);
        gl.VertexAttribPointer(instanceTransform, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero);
        gl.VertexAttribPointer(instancePoint12, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 16);
        gl.VertexAttribPointer(instancePoint34, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 32);
        gl.VertexAttribPointer(instancePoint56, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 48);
        gl.VertexAttribPointer(instancePoint78, 4, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 64);
        gl.VertexAttribIPointer(instancePointCount, 1, VertexAttribIType.Int, SizeOf<PolygonData>.Size, IntPtr.Zero + 80);
        gl.VertexAttribPointer(instanceRadius, 1, VertexAttribPointerType.Float, false, SizeOf<PolygonData>.Size, IntPtr.Zero + 84);
        // color will get automatically expanded to floats in the shader
        gl.VertexAttribPointer(instanceColor, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<PolygonData>.Size, IntPtr.Zero + 88);

        // These divisors tell glsl how to distribute per instance data
        gl.VertexAttribDivisor(instanceTransform, 1);
        gl.VertexAttribDivisor(instancePoint12, 1);
        gl.VertexAttribDivisor(instancePoint34, 1);
        gl.VertexAttribDivisor(instancePoint56, 1);
        gl.VertexAttribDivisor(instancePoint78, 1);
        gl.VertexAttribDivisor(instancePointCount, 1);
        gl.VertexAttribDivisor(instanceRadius, 1);
        gl.VertexAttribDivisor(instanceColor, 1);

        gl.CheckOpenGL();

        // Cleanup
        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);

        return render;
    }

    public static void DestroyPolygons(GL gl, ref SolidPolygonRender render)
    {
        if (0 != render.vaoId[0])
        {
            gl.DeleteVertexArrays(render.vaoId);
            gl.DeleteBuffers(render.vboIds);
            render.vaoId[0] = 0;
        }

        if (0 != render.programId)
        {
            gl.DeleteProgram(render.programId);
            render.programId = 0;
        }
    }

    public static void AddPolygon(ref SolidPolygonRender render, in B2Transform transform, ReadOnlySpan<B2Vec2> points, int count, float radius, B2HexColor color)
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

        render.polygons.Add(data);
    }

    public static void FlushPolygons(GL gl, ref SolidPolygonRender render, Camera camera)
    {
        int count = (int)render.polygons.Count;
        if (count == 0)
        {
            return;
        }

        gl.UseProgram(render.programId);
        gl.CheckOpenGL();

        B2FixedArray16<float> array16 = new B2FixedArray16<float>();
        Span<float> proj = array16.AsSpan();

        BuildProjectionMatrix(camera, proj, 0.2f);

        gl.UniformMatrix4(render.projectionUniform, 1, false, proj);
        gl.Uniform1(render.pixelScaleUniform, camera.height / camera.zoom);

        gl.BindVertexArray(render.vaoId[0]);
        gl.BindBuffer(GLEnum.ArrayBuffer, render.vboIds[1]);

        gl.Enable(GLEnum.Blend);
        gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        var polygons = CollectionsMarshal.AsSpan(render.polygons);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            gl.BufferSubData<PolygonData>(GLEnum.ArrayBuffer, 0, polygons.Slice(@base, batchCount));
            gl.DrawArraysInstanced(GLEnum.Triangles, 0, 6, (uint)batchCount);
            gl.CheckOpenGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        gl.Disable(GLEnum.Blend);

        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);
        gl.UseProgram(0);

        render.polygons.Clear();
    }
}