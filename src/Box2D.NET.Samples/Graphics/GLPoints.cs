// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Silk.NET.OpenGL;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Graphics;

public class GLPoints
{
    public const int e_batchSize = 2048;

    List<PointData> m_points;

    uint[] m_vaoId = new uint[1];
    uint[] m_vboId = new uint[1];
    uint m_programId;
    int m_projectionUniform;

    public void Create()
    {
        string vs = "#version 330\n" +
                    "uniform mat4 projectionMatrix;\n" +
                    "layout(location = 0) in vec2 v_position;\n" +
                    "layout(location = 1) in float v_size;\n" +
                    "layout(location = 2) in vec4 v_color;\n" +
                    "out vec4 f_color;\n" +
                    "void main(void)\n" +
                    "{\n" +
                    "	f_color = v_color;\n" +
                    "	gl_Position = projectionMatrix * vec4(v_position, 0.0f, 1.0f);\n" +
                    "	gl_PointSize = v_size;\n" +
                    "}\n";

        string fs = "#version 330\n" +
                    "in vec4 f_color;\n" +
                    "out vec4 color;\n" +
                    "void main(void)\n" +
                    "{\n" +
                    "	color = f_color;\n" +
                    "}\n";

        m_programId = B2.g_shader.CreateProgramFromStrings(vs, fs);
        m_projectionUniform = B2.g_shader.Gl.GetUniformLocation(m_programId, "projectionMatrix");
        uint vertexAttribute = 0;
        uint sizeAttribute = 1;
        uint colorAttribute = 2;

        // Generate
        B2.g_shader.Gl.GenVertexArrays(m_vaoId);
        B2.g_shader.Gl.GenBuffers(m_vboId);

        B2.g_shader.Gl.BindVertexArray(m_vaoId[0]);
        B2.g_shader.Gl.EnableVertexAttribArray(vertexAttribute);
        B2.g_shader.Gl.EnableVertexAttribArray(sizeAttribute);
        B2.g_shader.Gl.EnableVertexAttribArray(colorAttribute);

        // Vertex buffer
        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId);
        B2.g_shader.Gl.BufferData(GLEnum.ArrayBuffer, e_batchSize * sizeof(PointData), nullptr, GLEnum.DynamicDraw);

        B2.g_shader.Gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, GL_FALSE, sizeof(PointData), (void*)offsetof(PointData, position));
        B2.g_shader.Gl.VertexAttribPointer(sizeAttribute, 1, VertexAttribPointerType.Float, GL_FALSE, sizeof(PointData), (void*)offsetof(PointData, size));
        // save bandwidth by expanding color to floats in the shader
        B2.g_shader.Gl.VertexAttribPointer(colorAttribute, 4, VertexAttribPointerType.UnsignedByte, GL_TRUE, sizeof(PointData), (void*)offsetof(PointData, rgba));

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
            B2.g_shader.Gl.DeleteBuffers(1, &m_vboId);
            m_vaoId = 0;
            m_vboId = 0;
        }

        if (m_programId)
        {
            B2.g_shader.Gl.DeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    // todo instead of flushing, keep a growable array of data
    // this will prevent sorting problems.

    public void AddPoint(B2Vec2 v, float size, B2HexColor c)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(c, 1.0f);
        m_points.Add(new PointData(v, size, rgba));
    }

    public void Flush()
    {
        int count = m_points.Count;
        if (count == 0)
        {
            return;
        }

        B2.g_shader.Gl.UseProgram(m_programId);

        float[] proj = new float[16];
        B2.g_camera.BuildProjectionMatrix(proj, 0.0f);

        glUniformMatrix4fv(m_projectionUniform, 1, GL_FALSE, proj);
        B2.g_shader.Gl.BindVertexArray(m_vaoId);

        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId[0]);
        B2.g_shader.Gl.Enable(GLEnum.ProgramPointSize);

        int @base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);
            B2.g_shader.Gl.BufferSubData(GLEnum.ArrayBuffer, 0, batchCount * sizeof(PointData), &m_points[@base]);
            B2.g_shader.Gl.DrawArrays(GLEnum.Points, 0, batchCount);

            B2.g_shader.CheckErrorGL();

            count -= e_batchSize;
            @base += e_batchSize;
        }

        B2.g_shader.Gl.Disable(GLEnum.ProgramPointSize);
        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.Gl.BindVertexArray(0);
        B2.g_shader.Gl.UseProgram(0);

        m_points.Clear();
    }
}