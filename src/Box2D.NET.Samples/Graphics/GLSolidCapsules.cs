// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Box2D.NET.Samples.Helpers;
using Box2D.NET.Samples.Primitives;
using Serilog;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Graphics;

// Draw capsules using SDF-based shader
public class GLSolidCapsules
{
    private static readonly ILogger Logger = Log.ForContext<GLSolidCapsules>();
    
    public const int e_batchSize = 2048;

    private GL _gl;
    private Camera _camera;
    private List<CapsuleData> m_capsules = new List<CapsuleData>();

    private uint[] m_vaoId = new uint[1];
    private uint[] m_vboIds = new uint[2];
    private uint m_programId;
    private int m_projectionUniform;
    private int m_pixelScaleUniform;

    public void Create(SampleAppContext context)
    {
        _camera = context.camera;
        _gl = context.gl;

        m_programId = _gl.CreateProgramFromFiles("data/solid_capsule.vs", "data/solid_capsule.fs");

        m_projectionUniform = _gl.GetUniformLocation(m_programId, "projectionMatrix");
        m_pixelScaleUniform = _gl.GetUniformLocation(m_programId, "pixelScale");

        uint vertexAttribute = 0;
        uint transformInstance = 1;
        uint radiusInstance = 2;
        uint lengthInstance = 3;
        uint colorInstance = 4;

        // Generate
        _gl.GenVertexArrays(m_vaoId);
        _gl.GenBuffers(m_vboIds);

        _gl.BindVertexArray(m_vaoId[0]);
        _gl.EnableVertexAttribArray(vertexAttribute);
        _gl.EnableVertexAttribArray(transformInstance);
        _gl.EnableVertexAttribArray(radiusInstance);
        _gl.EnableVertexAttribArray(lengthInstance);
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

        // Capsule buffer
        _gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        _gl.BufferData<CapsuleData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<CapsuleData>.Size, null, GLEnum.DynamicDraw);

        _gl.VertexAttribPointer(transformInstance, 4, VertexAttribPointerType.Float, false, SizeOf<CapsuleData>.Size, IntPtr.Zero);
        _gl.VertexAttribPointer(radiusInstance, 1, VertexAttribPointerType.Float, false, SizeOf<CapsuleData>.Size, IntPtr.Zero + 16);
        _gl.VertexAttribPointer(lengthInstance, 1, VertexAttribPointerType.Float, false, SizeOf<CapsuleData>.Size, IntPtr.Zero + 20);
        _gl.VertexAttribPointer(colorInstance, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<CapsuleData>.Size, IntPtr.Zero + 24);

        _gl.VertexAttribDivisor(transformInstance, 1);
        _gl.VertexAttribDivisor(radiusInstance, 1);
        _gl.VertexAttribDivisor(lengthInstance, 1);
        _gl.VertexAttribDivisor(colorInstance, 1);

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

    public void AddCapsule(B2Vec2 p1, B2Vec2 p2, float radius, B2HexColor c)
    {
        B2Vec2 d = p2 - p1;
        float length = b2Length(d);
        if (length < 0.001f)
        {
            Logger.Information("WARNING: sample app: capsule too short!\n");
            return;
        }

        B2Vec2 axis = new B2Vec2(d.X / length, d.Y / length);
        B2Transform transform;
        transform.p = 0.5f * (p1 + p2);
        transform.q.c = axis.X;
        transform.q.s = axis.Y;

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

        _gl.UseProgram(m_programId);

        B2FixedArray16<float> array16 = new B2FixedArray16<float>();
        Span<float> proj = array16.AsSpan();
        
        _camera.BuildProjectionMatrix(proj, 0.2f);
        
        _gl.UniformMatrix4(m_projectionUniform, 1, false, proj);
        _gl.Uniform1(m_pixelScaleUniform, _camera.m_height / _camera.m_zoom);

        _gl.BindVertexArray(m_vaoId[0]);

        _gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        _gl.Enable(GLEnum.Blend);
        _gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        var capsules = CollectionsMarshal.AsSpan(m_capsules);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            _gl.BufferSubData<CapsuleData>(GLEnum.ArrayBuffer, 0, capsules.Slice(@base, batchCount));
            _gl.DrawArraysInstanced(GLEnum.Triangles, 0, 6, (uint)batchCount);

            _gl.CheckOpenGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        _gl.Disable(GLEnum.Blend);

        _gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
        _gl.UseProgram(0);

        m_capsules.Clear();
    }
}