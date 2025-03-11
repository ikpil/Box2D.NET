// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace Box2D.NET.Samples.Graphics;

public class GLBackground
{
    private Camera _camera;
    private Glfw _glfw;
    private GL _gl;
    
    private uint[] m_vaoId = new uint[1];
    private uint[] m_vboId = new uint[1];
    private uint m_programId;
    private int m_timeUniform;
    private int m_resolutionUniform;
    private int m_baseColorUniform;

    public void Create(SampleAppContext context)
    {
        _glfw = context.glfw;
        _gl = context.gl;
        _camera = context.camera;
        
        m_programId = _gl.CreateProgramFromFiles("data/background.vs", "data/background.fs");
        m_timeUniform = _gl.GetUniformLocation(m_programId, "time");
        m_resolutionUniform = _gl.GetUniformLocation(m_programId, "resolution");
        m_baseColorUniform = _gl.GetUniformLocation(m_programId, "baseColor");
        uint vertexAttribute = 0;

        // Generate
        _gl.GenVertexArrays(m_vaoId);
        _gl.GenBuffers(m_vboId);

        _gl.BindVertexArray(m_vaoId[0]);
        _gl.EnableVertexAttribArray(vertexAttribute);

        // Single quad
        //B2ExplicitVec2[] vertices = [new(-1.0f, 1.0f), new(-1.0f, -1.0f), new(1.0f, 1.0f), new(1.0f, -1.0f)];
        B2Vec2[] vertices = [new(-1.0f, 1.0f), new(-1.0f, -1.0f), new(1.0f, 1.0f), new(1.0f, -1.0f)];
        _gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId[0]);
        _gl.BufferData<B2Vec2>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);
        _gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

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
            _gl.DeleteBuffers(m_vboId);
            m_vaoId[0] = 0;
            m_vboId[0] = 0;
        }

        if (0 != m_programId)
        {
            _gl.DeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    public void Draw()
    {
        _gl.UseProgram(m_programId);

        double time = _glfw.GetTime();
        time = time % 100.0; // fmodf 대신 % 연산 사용

        // float time = (float)B2.g_glfw.GetTime();
        // time = fmodf(time, 100.0f);

        _gl.Uniform1(m_timeUniform, time);
        _gl.Uniform2(m_resolutionUniform, _camera.m_width, _camera.m_height);

        // struct RGBA8 c8 = RGBA8.MakeRGBA8( b2_colorGray2, 1.0f );
        // B2GL.Shared.Gl.Uniform3(m_baseColorUniform, c8.r/255.0f, c8.g/255.0f, c8.b/255.0f);
        _gl.Uniform3(m_baseColorUniform, 0.2f, 0.2f, 0.2f);

        _gl.BindVertexArray(m_vaoId[0]);

        _gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId[0]);
        _gl.DrawArrays(GLEnum.TriangleStrip, 0, 4);
        _gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
        _gl.UseProgram(0);
    }
}