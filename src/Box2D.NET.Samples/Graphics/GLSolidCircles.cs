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

// Draws SDF circles using quad instancing. Apparently instancing of quads can be slow on older GPUs.
// https://www.reddit.com/r/opengl/comments/q7yikr/how_to_draw_several_quads_through_instancing/
// https://www.g-truc.net/post-0666.html
public class GLSolidCircles
{
    public const int e_batchSize = 2048;

    private GL _gl;
    private Camera _camera;
    private List<SolidCircleData> m_circles = new List<SolidCircleData>();

    private uint[] m_vaoId = new uint[1];
    private uint[] m_vboIds = new uint[2];
    private uint m_programId;
    private int m_projectionUniform;
    private int m_pixelScaleUniform;


    public void Create(SampleAppContext context)
    {
        _camera = context.camera;
        _gl = context.gl;

        m_programId = _gl.CreateProgramFromFiles("data/solid_circle.vs", "data/solid_circle.fs");
        m_projectionUniform = _gl.GetUniformLocation(m_programId, "projectionMatrix");
        m_pixelScaleUniform = _gl.GetUniformLocation(m_programId, "pixelScale");

        // Generate
        _gl.GenVertexArrays(m_vaoId);
        _gl.GenBuffers(m_vboIds);

        _gl.BindVertexArray(m_vaoId[0]);

        uint vertexAttribute = 0;
        uint transformInstance = 1;
        uint radiusInstance = 2;
        uint colorInstance = 3;
        _gl.EnableVertexAttribArray(vertexAttribute);
        _gl.EnableVertexAttribArray(transformInstance);
        _gl.EnableVertexAttribArray(radiusInstance);
        _gl.EnableVertexAttribArray(colorInstance);

        // Vertex buffer for single quad
        float a = 1.1f;
        B2Vec2[] vertices =
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
        _gl.BufferData<SolidCircleData>(GLEnum.ArrayBuffer, e_batchSize * SizeOf<SolidCircleData>.Size, null, GLEnum.DynamicDraw);

        _gl.VertexAttribPointer(transformInstance, 4, VertexAttribPointerType.Float, false, SizeOf<SolidCircleData>.Size, IntPtr.Zero);
        _gl.VertexAttribPointer(radiusInstance, 1, VertexAttribPointerType.Float, false, SizeOf<SolidCircleData>.Size, IntPtr.Zero + 16);
        _gl.VertexAttribPointer(colorInstance, 4, VertexAttribPointerType.UnsignedByte, true, SizeOf<SolidCircleData>.Size, IntPtr.Zero + 20);

        _gl.VertexAttribDivisor(transformInstance, 1);
        _gl.VertexAttribDivisor(radiusInstance, 1);
        _gl.VertexAttribDivisor(colorInstance, 1);

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
            m_vboIds[0] = 0;
            m_vboIds[1] = 0;
        }

        if (0 != m_programId)
        {
            _gl.DeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    public void AddCircle(ref B2Transform transform, float radius, B2HexColor color)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(color, 1.0f);
        m_circles.Add(new SolidCircleData(transform, radius, rgba));
    }

    public void Flush()
    {
        int count = (int)m_circles.Count;
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

        var circles = CollectionsMarshal.AsSpan(m_circles);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            _gl.BufferSubData<SolidCircleData>(GLEnum.ArrayBuffer, 0, circles.Slice(@base, batchCount));
            _gl.DrawArraysInstanced(GLEnum.Triangles, 0, 6, (uint)batchCount);

            _gl.CheckErrorGL();

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