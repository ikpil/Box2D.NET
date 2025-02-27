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

public class GLLines
{
    // need lots of space for lines so they draw last
    // could also consider disabling depth buffer
    // must be multiple of 2
    public const int e_batchSize = 2 * 2048;

    List<VertexData> m_points;

    uint m_vaoId;
    uint m_vboId;
    uint m_programId;
    int m_projectionUniform;

    public void Create()
    {
        string vs = "#version 330\n"
        + "uniform mat4 projectionMatrix;\n"
        + "layout(location = 0) in vec2 v_position;\n"
        + "layout(location = 1) in vec4 v_color;\n"
        + "out vec4 f_color;\n"
        + "void main(void)\n"
        + "{\n"
        + "	f_color = v_color;\n"
        + "	gl_Position = projectionMatrix * vec4(v_position, 0.0f, 1.0f);\n"
        + "}\n";

        string fs = "#version 330\n"
        + "in vec4 f_color;\n"
        + "out vec4 color;\n"
        + "void main(void)\n"
        + "{\n"
        + "	color = f_color;\n"
        + "}\n";

        m_programId = B2GL.Shared.CreateProgramFromStrings(vs, fs);
        m_projectionUniform = B2GL.Shared.Gl.GetUniformLocation(m_programId, "projectionMatrix");
        uint vertexAttribute = 0;
        uint colorAttribute = 1;

        // Generate
        B2GL.Shared.Gl.GenVertexArrays(1, &m_vaoId);
        B2GL.Shared.Gl.GenBuffers(1, &m_vboId);

        B2GL.Shared.Gl.BindVertexArray(m_vaoId);
        B2GL.Shared.Gl.EnableVertexAttribArray(vertexAttribute);
        B2GL.Shared.Gl.EnableVertexAttribArray(colorAttribute);

        // Vertex buffer
        B2GL.Shared.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId);
        B2GL.Shared.Gl.BufferData(GLEnum.ArrayBuffer, e_batchSize * sizeof(VertexData), nullptr, GLEnum.DynamicDraw);

        B2GL.Shared.Gl.VertexAttribPointer(vertexAttribute, 2, VertexAttribPointerType.Float, GL_FALSE, sizeof(VertexData), (void*)offsetof(VertexData, position));
        // save bandwidth by expanding color to floats in the shader
        B2GL.Shared.Gl.VertexAttribPointer(colorAttribute, 4, VertexAttribPointerType.UnsignedByte, GL_TRUE, sizeof(VertexData), (void*)offsetof(VertexData, rgba));

        CheckErrorGL();

        // Cleanup
        B2GL.Shared.Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2GL.Shared.Gl.BindVertexArray(0);
    }

    public void Destroy()
    {
        if (m_vaoId)
        {
            B2GL.Shared.Gl.DeleteVertexArrays(1, &m_vaoId);
            B2GL.Shared.Gl.DeleteBuffers(1, &m_vboId);
            m_vaoId = 0;
            m_vboId = 0;
        }

        if (m_programId)
        {
            B2GL.Shared.Gl.DeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    public void AddLine(B2Vec2 p1, B2Vec2 p2, B2HexColor c)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(c, 1.0f);
        m_points.Add( new VertexData( p1, rgba ));
        m_points.Add( new VertexData( p2, rgba ));
    }

    public void Flush()
    {
        int count = (int)m_points.size();
        if (count == 0)
        {
            return;
        }

        Debug.Assert(count % 2 == 0);

        B2GL.Shared.Gl.UseProgram(m_programId);

        float proj[16] =  {
            0.0f
        }
        ;
        Draw.g_camera.BuildProjectionMatrix(proj, 0.1f);

        glUniformMatrix4fv(m_projectionUniform, 1, GL_FALSE, proj);

        B2GL.Shared.Gl.BindVertexArray(m_vaoId);

        B2GL.Shared.Gl.BindBuffer(GLEnum.ArrayBuffer, m_vboId);

        int base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);
            B2GL.Shared.Gl.BufferSubData(GLEnum.ArrayBuffer, 0, batchCount * sizeof(VertexData), &m_points[base]);

            B2GL.Shared.Gl.DrawArrays(GL_LINES, 0, batchCount);

            CheckErrorGL();

            count -= e_batchSize;
            base += e_batchSize;
        }

        B2GL.Shared.Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        B2GL.Shared.Gl.BindVertexArray(0);
        B2GL.Shared.Gl.UseProgram(0);

        m_points.clear();
    }
}
