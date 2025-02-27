// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Graphics;

// Draw capsules using SDF-based shader
public class GLSolidCapsules
{
    public const int e_batchSize = 2048;

    List<CapsuleData> m_capsules;

    uint m_vaoId;
    uint m_vboIds[2];
    uint m_programId;
    int m_projectionUniform;
    int m_pixelScaleUniform;

    public void Create()
    {
        m_programId = B2GL.Shared.CreateProgramFromFiles("samples/data/solid_capsule.vs", "samples/data/solid_capsule.fs");

        m_projectionUniform = B2GL.Shared.Gl.GetUniformLocation(m_programId, "projectionMatrix");
        m_pixelScaleUniform = B2GL.Shared.Gl.GetUniformLocation(m_programId, "pixelScale");

        uint vertexAttribute = 0;
        uint transformInstance = 1;
        uint radiusInstance = 2;
        uint lengthInstance = 3;
        uint colorInstance = 4;

        // Generate
        B2GL.Shared.Gl.GenVertexArrays(1, &m_vaoId);
        B2GL.Shared.Gl.GenBuffers(2, m_vboIds);

        B2GL.Shared.Gl.BindVertexArray(m_vaoId);
        B2GL.Shared.Gl.EnableVertexAttribArray(vertexAttribute);
        B2GL.Shared.Gl.EnableVertexAttribArray(transformInstance);
        B2GL.Shared.Gl.EnableVertexAttribArray(radiusInstance);
        B2GL.Shared.Gl.EnableVertexAttribArray(lengthInstance);
        B2GL.Shared.Gl.EnableVertexAttribArray(colorInstance);

        // Vertex buffer for single quad
        float a = 1.1f;
        B2Vec2 vertices[] =  {
            {
                -a, -a
            }, {
                a, -a
            }, {
                -a, a
            }, {
                a, -a
            }, {
                a, a
            }, {
                -a, a
            }
        }
        ;
        B2GL.Shared.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[0]);
        B2GL.Shared.Gl.BufferData(GLEnum.ArrayBuffer, sizeof(vertices), vertices, GLEnum.StaticDraw);
        B2GL.Shared.Gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, GL_FALSE, 0, BUFFER_OFFSET(0));

        // Capsule buffer
        B2GL.Shared.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        B2GL.Shared.Gl.BufferData(GLEnum.ArrayBuffer, e_batchSize * sizeof(CapsuleData), nullptr, GLEnum.DynamicDraw);

        B2GL.Shared.Gl.VertexAttribPointer(transformInstance, 4, VertexAttribPointerType.Float, GL_FALSE, sizeof(CapsuleData), (void*)offsetof(CapsuleData, transform));
        B2GL.Shared.Gl.VertexAttribPointer(radiusInstance, 1, VertexAttribPointerType.Float, GL_FALSE, sizeof(CapsuleData), (void*)offsetof(CapsuleData, radius));
        B2GL.Shared.Gl.VertexAttribPointer(lengthInstance, 1, VertexAttribPointerType.Float, GL_FALSE, sizeof(CapsuleData), (void*)offsetof(CapsuleData, length));
        B2GL.Shared.Gl.VertexAttribPointer(colorInstance, 4, VertexAttribPointerType.UnsignedByte, GL_TRUE, sizeof(CapsuleData), (void*)offsetof(CapsuleData, rgba));

        B2GL.Shared.Gl.VertexAttribDivisor(transformInstance, 1);
        B2GL.Shared.Gl.VertexAttribDivisor(radiusInstance, 1);
        B2GL.Shared.Gl.VertexAttribDivisor(lengthInstance, 1);
        B2GL.Shared.Gl.VertexAttribDivisor(colorInstance, 1);

        B2GL.Shared.CheckErrorGL();

        // Cleanup
        B2GL.Shared.Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2GL.Shared.Gl.BindVertexArray(0);
    }

    public void Destroy()
    {
        if (m_vaoId)
        {
            B2GL.Shared.Gl.DeleteVertexArrays(1, &m_vaoId);
            B2GL.Shared.Gl.DeleteBuffers(2, m_vboIds);
            m_vaoId = 0;
            m_vboIds[0] = 0;
            m_vboIds[1] = 0;
        }

        if (m_programId)
        {
            B2GL.Shared.Gl.DeleteProgram(m_programId);
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

        B2Vec2 axis = { d.x / length, d.y / length };
        B2Transform transform;
        transform.p = 0.5f * (p1 + p2);
        transform.q.c = axis.x;
        transform.q.s = axis.y;

        RGBA8 rgba = RGBA8.MakeRGBA8(c, 1.0f);

        m_capsules.Add( {
            transform, radius, length, rgba
        } );
    }

    public void Flush()
    {
        int count = (int)m_capsules.size();
        if (count == 0)
        {
            return;
        }

        B2GL.Shared.Gl.UseProgram(m_programId);

        float proj[16] =  {
            0.0f
        }
        ;
        Draw.g_camera.BuildProjectionMatrix(proj, 0.2f);

        glUniformMatrix4fv(m_projectionUniform, 1, GL_FALSE, proj);
        B2GL.Shared.Gl.Uniform1(m_pixelScaleUniform, Draw.g_camera.m_height / Draw.g_camera.m_zoom);

        B2GL.Shared.Gl.BindVertexArray(m_vaoId);

        B2GL.Shared.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboIds[1]);
        B2GL.Shared.Gl.Enable(GLEnum.Blend);
        B2GL.Shared.Gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        int base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            B2GL.Shared.Gl.BufferSubData(GLEnum.ArrayBuffer, 0, batchCount * sizeof(CapsuleData), &m_capsules[base]);
            B2GL.Shared.Gl.DrawArraysInstanced(GLEnum.Triangles, 0, 6, batchCount);

            B2GL.Shared.CheckErrorGL();

            count -= e_batchSize;
            base += e_batchSize;
        }

        B2GL.Shared.Gl.Disable(GLEnum.Blend);

        B2GL.Shared.Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2GL.Shared.Gl.BindVertexArray(0);
        B2GL.Shared.Gl.UseProgram(0);

        m_capsules.clear();
    }
}
