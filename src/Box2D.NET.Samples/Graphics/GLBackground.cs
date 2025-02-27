// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using Box2D.NET.Primitives;
using Silk.NET.OpenGL;

namespace Box2D.NET.Samples.Graphics;

public class GLBackground
{
    uint[] m_vaoId = new uint[1];
    uint[] m_vboId = new uint[1];
    uint m_programId;
    int m_timeUniform;
    int m_resolutionUniform;
    int m_baseColorUniform;

    public GLBackground()
    {
    }

    public void Create()
    {
        m_programId = B2.g_shader.CreateProgramFromFiles("samples/data/background.vs", "samples/data/background.fs");
        m_timeUniform = B2.g_shader.Gl.GetUniformLocation(m_programId, "time");
        m_resolutionUniform = B2.g_shader.Gl.GetUniformLocation(m_programId, "resolution");
        m_baseColorUniform = B2.g_shader.Gl.GetUniformLocation(m_programId, "baseColor");
        uint vertexAttribute = 0;

        // Generate
        B2.g_shader.Gl.GenVertexArrays(m_vaoId);
        B2.g_shader.Gl.GenBuffers(m_vboId);

        B2.g_shader.Gl.BindVertexArray(m_vaoId[0]);
        B2.g_shader.Gl.EnableVertexAttribArray(vertexAttribute);

        // Single quad
        B2Vec2[] vertices = new B2Vec2[] { new B2Vec2(-1.0f, 1.0f), new B2Vec2(-1.0f, -1.0f), new B2Vec2(1.0f, 1.0f), new B2Vec2(1.0f, -1.0f) };
        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId[0]);
        B2.g_shader.Gl.BufferData<B2Vec2>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);
        B2.g_shader.Gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

        B2.g_shader.CheckErrorGL();

        // Cleanup
        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.Gl.BindVertexArray(0);
    }

    public void Destroy()
    {
        if (0 != m_vaoId[0])
        {
            B2.g_shader.Gl.DeleteVertexArrays(m_vaoId);
            B2.g_shader.Gl.DeleteBuffers(m_vboId);
            m_vaoId[0] = 0;
            m_vboId[0] = 0;
        }

        if (0 != m_programId)
        {
            B2.g_shader.Gl.DeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    public void Draw()
    {
        B2.g_shader.Gl.UseProgram(m_programId);

        double time = B2.g_glfw.GetTime();
        time = time % 100.0; // fmodf 대신 % 연산 사용

        // float time = (float)glfwGetTime();
        // time = fmodf(time, 100.0f);

        B2.g_shader.Gl.Uniform1(m_timeUniform, time);
        B2.g_shader.Gl.Uniform2(m_resolutionUniform, B2.g_camera.m_width, B2.g_camera.m_height);

        // struct RGBA8 c8 = RGBA8.MakeRGBA8( b2_colorGray2, 1.0f );
        // B2GL.Shared.Gl.Uniform3(m_baseColorUniform, c8.r/255.0f, c8.g/255.0f, c8.b/255.0f);
        B2.g_shader.Gl.Uniform3(m_baseColorUniform, 0.2f, 0.2f, 0.2f);

        B2.g_shader.Gl.BindVertexArray(m_vaoId[0]);

        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId[0]);
        B2.g_shader.Gl.DrawArrays(GLEnum.TriangleStrip, 0, 4);
        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.Gl.BindVertexArray(0);
        B2.g_shader.Gl.UseProgram(0);
    }
}