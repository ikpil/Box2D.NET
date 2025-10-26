// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace Box2D.NET.Samples.Graphics;

public static class Backgrounds
{
    public static Background CreateBackground(GL gl)
    {
        Background background = new Background();

        background.programId = gl.CreateProgramFromFiles("data/background.vs", "data/background.fs");
        background.timeUniform = gl.GetUniformLocation(background.programId, "time");
        background.resolutionUniform = gl.GetUniformLocation(background.programId, "resolution");
        background.baseColorUniform = gl.GetUniformLocation(background.programId, "baseColor");
        uint vertexAttribute = 0;

        // Generate
        gl.GenVertexArrays(background.vaoId);
        gl.GenBuffers(background.vboId);

        gl.BindVertexArray(background.vaoId[0]);
        gl.EnableVertexAttribArray(vertexAttribute);

        // Single quad
        //B2ExplicitVec2[] vertices = [new(-1.0f, 1.0f), new(-1.0f, -1.0f), new(1.0f, 1.0f), new(1.0f, -1.0f)];
        B2Vec2[] vertices = [new(-1.0f, 1.0f), new(-1.0f, -1.0f), new(1.0f, 1.0f), new(1.0f, -1.0f)];
        gl.BindBuffer(GLEnum.ArrayBuffer, background.vboId[0]);
        gl.BufferData<B2Vec2>(GLEnum.ArrayBuffer, vertices, GLEnum.StaticDraw);
        gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

        gl.CheckOpenGL();

        // Cleanup
        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);

        return background;
    }

    public static void DestroyBackground(GL gl, ref Background background)
    {
        if (0 != background.vaoId[0])
        {
            gl.DeleteVertexArrays(background.vaoId);
            gl.DeleteBuffers(background.vboId);
            background.vaoId[0] = 0;
            background.vboId[0] = 0;
        }

        if (0 != background.programId)
        {
            gl.DeleteProgram(background.programId);
            background.programId = 0;
        }
    }

    public static void RenderBackground(Glfw glfw, GL gl, ref Background background, Camera camera)
    {
        gl.UseProgram(background.programId);

        double time = glfw.GetTime();
        time = time % 100.0; // fmodf 대신 % 연산 사용

        // float time = (float)B2.gglfw.GetTime();
        // time = fmodf(time, 100.0f);

        gl.Uniform1(background.timeUniform, time);
        gl.Uniform2(background.resolutionUniform, camera.width, camera.height);

        // struct RGBA8 c8 = RGBA8.MakeRGBA8( b2_colorGray2, 1.0f );
        // B2GL.Shared.Gl.Uniform3(background.m_baseColorUniform, c8.r/255.0f, c8.g/255.0f, c8.b/255.0f);
        gl.Uniform3(background.baseColorUniform, 0.2f, 0.2f, 0.2f);

        gl.BindVertexArray(background.vaoId[0]);

        gl.BindBuffer(GLEnum.ArrayBuffer, background.vboId[0]);
        gl.DrawArrays(GLEnum.TriangleStrip, 0, 4);
        gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        gl.BindVertexArray(0);
        gl.UseProgram(0);
    }
}
