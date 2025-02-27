// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;
using static Box2D.NET.B2MathFunction;


namespace Box2D.NET.Samples.Graphics;

public class GLCircles
{
    public const int e_batchSize = 2048;

    List<CircleData> m_circles;

    uint[] m_vaoId = new uint[1];
    uint[] m_vboIds = new uint[2];
    uint m_programId;
    int m_projectionUniform;
    int m_pixelScaleUniform;


    public void Create()
    {
        m_programId = B2.g_shader.CreateProgramFromFiles("samples/data/circle.vs", "samples/data/circle.fs");
        m_projectionUniform = B2.g_shader.Gl.GetUniformLocation(m_programId, "projectionMatrix");
        m_pixelScaleUniform = B2.g_shader.Gl.GetUniformLocation(m_programId, "pixelScale");
        uint vertexAttribute = 0;
        uint positionInstance = 1;
        uint radiusInstance = 2;
        uint colorInstance = 3;

        // Generate
        B2.g_shader.Gl.GenVertexArrays(m_vaoId);
        B2.g_shader.Gl.GenBuffers(m_vboIds);

        B2.g_shader.Gl.BindVertexArray(m_vaoId[0]);
        B2.g_shader.Gl.EnableVertexAttribArray(vertexAttribute);
        B2.g_shader.Gl.EnableVertexAttribArray(positionInstance);
        B2.g_shader.Gl.EnableVertexAttribArray(radiusInstance);
        B2.g_shader.Gl.EnableVertexAttribArray(colorInstance);

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
        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[0]);
        B2.g_shader.Gl.BufferData<B2Vec2>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);
        B2.g_shader.Gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

        // Circle buffer
        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        B2.g_shader.Gl.BufferData(GLEnum.ArrayBuffer, e_batchSize * sizeof(CircleData), nullptr, GLEnum.DynamicDraw);

        B2.g_shader.Gl.VertexAttribPointer(positionInstance, 2, VertexAttribPointerType.Float, GL_FALSE, sizeof(CircleData), (void*)offsetof(CircleData, position));
        B2.g_shader.Gl.VertexAttribPointer(radiusInstance, 1, VertexAttribPointerType.Float, GL_FALSE, sizeof(CircleData), (void*)offsetof(CircleData, radius));
        B2.g_shader.Gl.VertexAttribPointer(colorInstance, 4, VertexAttribPointerType.UnsignedByte, GL_TRUE, sizeof(CircleData), (void*)offsetof(CircleData, rgba));

        B2.g_shader.Gl.VertexAttribDivisor(positionInstance, 1);
        B2.g_shader.Gl.VertexAttribDivisor(radiusInstance, 1);
        B2.g_shader.Gl.VertexAttribDivisor(colorInstance, 1);

        B2.g_shader.CheckErrorGL();

        // Cleanup
        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.Gl.BindVertexArray(0);
    }

    public void Destroy()
    {
        if (m_vaoId)
        {
            B2.g_shader.Gl.DeleteVertexArrays(1, &m_vaoId);
            B2.g_shader.Gl.DeleteBuffers(2, m_vboIds);
            m_vaoId = 0;
            m_vboIds[0] = 0;
            m_vboIds[1] = 0;
        }

        if (m_programId)
        {
            B2.g_shader.Gl.DeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    public void AddCircle(B2Vec2 center, float radius, B2HexColor color)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(color, 1.0f);
        m_circles.Add( {
            center, radius, rgba
        } );
    }

    public void Flush()
    {
        int count = m_circles.Count;
        if (count == 0)
        {
            return;
        }

        B2.g_shader.Gl.UseProgram(m_programId);

        float proj[16] =  {
            0.0f
        }
        ;
        Draw.g_camera.BuildProjectionMatrix(proj, 0.2f);

        glUniformMatrix4fv(m_projectionUniform, 1, GL_FALSE, proj);
        B2.g_shader.Gl.Uniform1(m_pixelScaleUniform, Draw.g_camera.m_height / Draw.g_camera.m_zoom);

        B2.g_shader.Gl.BindVertexArray(m_vaoId);

        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        B2.g_shader.Gl.Enable(GLEnum.Blend);
        B2.g_shader.Gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        var asdf = CollectionsMarshal.AsSpan(m_circles);
        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            B2.g_shader.Gl.BufferSubData<CircleData>(GLEnum.ArrayBuffer, 0, batchCount * sizeof(CircleData), &m_circles[base]);
            B2.g_shader.Gl.BufferSubData<CircleData>(GLEnum.ArrayBuffer, 0, asdf);
            B2.g_shader.Gl.DrawArraysInstanced(GLEnum.Triangles, 0, 6, batchCount);

            B2.g_shader.CheckErrorGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        B2.g_shader.Gl.Disable(GLEnum.Blend);

        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.Gl.BindVertexArray(0);
        B2.g_shader.Gl.UseProgram(0);

        m_circles.Clear();
    }
}
