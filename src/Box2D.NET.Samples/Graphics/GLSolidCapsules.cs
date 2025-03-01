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

// Draw capsules using SDF-based shader
public class GLSolidCapsules
{
    public const int e_batchSize = 2048;

    private List<CapsuleData> m_capsules = new List<CapsuleData>();

    private uint[] m_vaoId = new uint[1];
    private uint[] m_vboIds = new uint[2];
    private uint m_programId;
    private int m_projectionUniform;
    private int m_pixelScaleUniform;

    public void Create()
    {
        m_programId = B2.g_shader.CreateProgramFromFiles("data/solid_capsule.vs", "data/solid_capsule.fs");

        m_projectionUniform = B2.g_shader.gl.GetUniformLocation(m_programId, "projectionMatrix");
        m_pixelScaleUniform = B2.g_shader.gl.GetUniformLocation(m_programId, "pixelScale");

        uint vertexAttribute = 0;
        uint transformInstance = 1;
        uint radiusInstance = 2;
        uint lengthInstance = 3;
        uint colorInstance = 4;

        // Generate
        B2.g_shader.gl.GenVertexArrays(m_vaoId);
        B2.g_shader.gl.GenBuffers(m_vboIds);

        B2.g_shader.gl.BindVertexArray(m_vaoId[0]);
        B2.g_shader.gl.EnableVertexAttribArray(vertexAttribute);
        B2.g_shader.gl.EnableVertexAttribArray(transformInstance);
        B2.g_shader.gl.EnableVertexAttribArray(radiusInstance);
        B2.g_shader.gl.EnableVertexAttribArray(lengthInstance);
        B2.g_shader.gl.EnableVertexAttribArray(colorInstance);

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

        // Capsule buffer
        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        B2.g_shader.gl.BufferData<CapsuleData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<CapsuleData>.Size, null, GLEnum.DynamicDraw);

        B2.g_shader.gl.VertexAttribPointer(transformInstance, 4, VertexAttribPointerType.Float, false, SizeOf<CapsuleData>.Size, IntPtr.Zero);
        B2.g_shader.gl.VertexAttribPointer(radiusInstance, 1, VertexAttribPointerType.Float, false, SizeOf<CapsuleData>.Size, IntPtr.Zero + 16);
        B2.g_shader.gl.VertexAttribPointer(lengthInstance, 1, VertexAttribPointerType.Float, false, SizeOf<CapsuleData>.Size, IntPtr.Zero + 20);
        B2.g_shader.gl.VertexAttribPointer(colorInstance, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<CapsuleData>.Size, IntPtr.Zero + 24);

        B2.g_shader.gl.VertexAttribDivisor(transformInstance, 1);
        B2.g_shader.gl.VertexAttribDivisor(radiusInstance, 1);
        B2.g_shader.gl.VertexAttribDivisor(lengthInstance, 1);
        B2.g_shader.gl.VertexAttribDivisor(colorInstance, 1);

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
            B2.g_shader.gl.DeleteBuffers(m_vboIds);
            m_vaoId[0] = 0;
            m_vboIds[0] = 0;
            m_vboIds[1] = 0;
        }

        if (0 != m_programId)
        {
            B2.g_shader.gl.DeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    public void AddCapsule(B2Vec2 p1, B2Vec2 p2, float radius, B2HexColor c)
    {
        B2Vec2 d = p2 - p1;
        float length = b2Length(d);
        if (length < 0.001f)
        {
            Console.WriteLine("WARNING: sample app: capsule too short!\n");
            return;
        }

        B2Vec2 axis = new B2Vec2(d.x / length, d.y / length);
        B2Transform transform;
        transform.p = 0.5f * (p1 + p2);
        transform.q.c = axis.x;
        transform.q.s = axis.y;

        RGBA8 rgba = RGBA8.MakeRGBA8(c, 1.0f);

        m_capsules.Add(new CapsuleData(transform, radius, length, rgba));
    }

    public void Flush()
    {
        int count = m_capsules.Count;
        if (count == 0)
        {
            return;
        }

        B2.g_shader.gl.UseProgram(m_programId);

        float[] proj = new float[16];
        B2.g_camera.BuildProjectionMatrix(proj, 0.2f);

        B2.g_shader.gl.UniformMatrix4(m_projectionUniform, 1, false, proj);
        B2.g_shader.gl.Uniform1(m_pixelScaleUniform, B2.g_camera.m_height / B2.g_camera.m_zoom);

        B2.g_shader.gl.BindVertexArray(m_vaoId[0]);

        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        B2.g_shader.gl.Enable(GLEnum.Blend);
        B2.g_shader.gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        var capsules = CollectionsMarshal.AsSpan(m_capsules);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            B2.g_shader.gl.BufferSubData<CapsuleData>(GLEnum.ArrayBuffer, 0, capsules.Slice(@base, batchCount));
            B2.g_shader.gl.DrawArraysInstanced(GLEnum.Triangles, 0, 6, (uint)batchCount);

            B2.g_shader.CheckErrorGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        B2.g_shader.gl.Disable(GLEnum.Blend);

        B2.g_shader.gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.gl.BindVertexArray(0);
        B2.g_shader.gl.UseProgram(0);

        m_capsules.Clear();
    }
}