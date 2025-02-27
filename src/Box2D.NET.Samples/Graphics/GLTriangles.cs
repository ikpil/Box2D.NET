// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Diagnostics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;

namespace Box2D.NET.Samples.Graphics;

// todo this is not used anymore and has untested changes
public class GLTriangles
{
    // must be multiple of 3
    public const int e_batchSize = 3 * 512;

    List<VertexData> m_points;

    uint m_vaoId;
    uint m_vboId;
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

        m_programId = B2GL.Shared.CreateProgramFromStrings(vs, fs);
        m_projectionUniform = B2GL.Shared.Gl.GetUniformLocation(m_programId, "projectionMatrix");
        int vertexAttribute = 0;
        int colorAttribute = 1;

        // Generate
        B2GL.Shared.Gl.GenVertexArrays(1, &m_vaoId);
        B2GL.Shared.Gl.GenBuffers(1, &m_vboId);

        B2GL.Shared.Gl.BindVertexArray(m_vaoId);
        B2GL.Shared.Gl.EnableVertexAttribArray(vertexAttribute);
        B2GL.Shared.Gl.EnableVertexAttribArray(colorAttribute);

        // Vertex buffer
        B2GL.Shared.Gl.BindBuffer(GL_ARRAY_BUFFER, m_vboId);
        B2GL.Shared.Gl.BufferData(GL_ARRAY_BUFFER, e_batchSize * sizeof(VertexData), nullptr, GL_DYNAMIC_DRAW);

        B2GL.Shared.Gl.VertexAttribPointer(vertexAttribute, 2, GL_FLOAT, GL_FALSE, sizeof(VertexData),
            (void*)offsetof(VertexData, position));
        // color will get automatically expanded to floats in the shader
        B2GL.Shared.Gl.VertexAttribPointer(colorAttribute, 4, GL_UNSIGNED_BYTE, GL_TRUE, sizeof(VertexData),
            (void*)offsetof(VertexData, rgba));

        CheckErrorGL();

        // Cleanup
        B2GL.Shared.Gl.BindBuffer(GL_ARRAY_BUFFER, 0);
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

        B2GL.Shared.Gl.UseProgram(m_programId);

        float proj[16] =  {
            0.0f
        }
        ;
        Draw.g_camera.BuildProjectionMatrix(proj, 0.2f);

        glUniformMatrix4fv(m_projectionUniform, 1, GL_FALSE, proj);

        B2GL.Shared.Gl.BindVertexArray(m_vaoId);

        B2GL.Shared.Gl.BindBuffer(GL_ARRAY_BUFFER, m_vboId);
        B2GL.Shared.Gl.Enable(GL_BLEND);
        B2GL.Shared.Gl.BlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

        int base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            B2GL.Shared.Gl.BufferSubData(GL_ARRAY_BUFFER, 0, batchCount * sizeof(VertexData), &m_points[base]);
            glDrawArrays(GL_TRIANGLES, 0, batchCount);

            CheckErrorGL();

            count -= e_batchSize;
            base += e_batchSize;
        }

        B2GL.Shared.Gl.Disable(GL_BLEND);

        B2GL.Shared.Gl.BindBuffer(GL_ARRAY_BUFFER, 0);
        B2GL.Shared.Gl.BindVertexArray(0);
        B2GL.Shared.Gl.UseProgram(0);

        m_points.clear();
    }
}