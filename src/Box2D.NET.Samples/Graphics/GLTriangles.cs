// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Diagnostics;
using Silk.NET.OpenGL;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Graphics;

// todo this is not used anymore and has untested changes
public class GLTriangles
{
    // must be multiple of 3
    public const int e_batchSize = 3 * 512;

    List<VertexData> m_points;

    uint[] m_vaoId = new uint[1];
    uint[] m_vboId = new uint[1];
    uint m_programId;
    int m_projectionUniform;

    public void Create()
    {
        string vs = "#version 330\n" +
                    "uniform mat4 projectionMatrix;\n" +
                    "layout(location = 0) in vec2 v_position;\n" +
                    "layout(location = 1) in vec4 v_color;\n" +
                    "out vec4 f_color;\n" +
                    "void main(void)\n" +
                    "{\n" +
                    "	f_color = v_color;\n" +
                    "	gl_Position = projectionMatrix * vec4(v_position, 0.0f, 1.0f);\n" +
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
        uint colorAttribute = 1;

        // Generate
        B2.g_shader.Gl.GenVertexArrays(m_vaoId);
        B2.g_shader.Gl.GenBuffers(m_vboId);

        B2.g_shader.Gl.BindVertexArray(m_vaoId[0]);
        B2.g_shader.Gl.EnableVertexAttribArray(vertexAttribute);
        B2.g_shader.Gl.EnableVertexAttribArray(colorAttribute);

        // Vertex buffer
        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId);
        B2.g_shader.Gl.BufferData(GLEnum.ArrayBuffer, e_batchSize * sizeof(VertexData), nullptr, GLEnum.DynamicDraw);

        B2.g_shader.Gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, GL_FALSE, sizeof(VertexData), (void*)offsetof(VertexData, position));
        // color will get automatically expanded to floats in the shader
        B2.g_shader.Gl.VertexAttribPointer(colorAttribute, 4, VertexAttribPointerType.UnsignedByte, GL_TRUE, sizeof(VertexData), (void*)offsetof(VertexData, rgba));

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

    public void AddTriangle(B2Vec2 p1, B2Vec2 p2, B2Vec2 p3, B2HexColor c)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(c, 1.0f);
        m_points.Add( {
            p1, rgba
        } );
        m_points.Add( {
            p2, rgba
        } );
        m_points.Add( {
            p3, rgba
        } );
    }

    public void Flush()
    {
        int count = (int)m_points.size();
        if (count == 0)
        {
            return;
        }

        Debug.Assert(count % 3 == 0);

        B2.g_shader.Gl.UseProgram(m_programId);

        float proj[16] =  {
            0.0f
        }
        ;
        B2.g_camera.BuildProjectionMatrix(proj, 0.2f);

        glUniformMatrix4fv(m_projectionUniform, 1, GL_FALSE, proj);

        B2.g_shader.Gl.BindVertexArray(m_vaoId);

        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId);
        B2.g_shader.Gl.Enable(GLEnum.Blend);
        B2.g_shader.Gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        int base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            B2.g_shader.Gl.BufferSubData(GLEnum.ArrayBuffer, 0, batchCount * sizeof(VertexData), &m_points[base]);
            B2.g_shader.Gl.DrawArrays(GLEnum.Triangles, 0, batchCount);

            B2.g_shader.CheckErrorGL();

            count -= e_batchSize;
            base += e_batchSize;
        }

        B2.g_shader.Gl.Disable(GLEnum.Blend);

        B2.g_shader.Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2.g_shader.Gl.BindVertexArray(0);
        B2.g_shader.Gl.UseProgram(0);

        m_points.clear();
    }
}