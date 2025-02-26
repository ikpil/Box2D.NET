// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Diagnostics;
using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

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

        m_programId = CreateProgramFromStrings(vs, fs);
        m_projectionUniform = glGetUniformLocation(m_programId, "projectionMatrix");
        int vertexAttribute = 0;
        int colorAttribute = 1;

        // Generate
        glGenVertexArrays(1, &m_vaoId);
        glGenBuffers(1, &m_vboId);

        glBindVertexArray(m_vaoId);
        glEnableVertexAttribArray(vertexAttribute);
        glEnableVertexAttribArray(colorAttribute);

        // Vertex buffer
        glBindBuffer(GL_ARRAY_BUFFER, m_vboId);
        glBufferData(GL_ARRAY_BUFFER, e_batchSize * sizeof(VertexData), nullptr, GL_DYNAMIC_DRAW);

        glVertexAttribPointer(vertexAttribute, 2, GL_FLOAT, GL_FALSE, sizeof(VertexData),
            (void*)offsetof(VertexData, position));
        // save bandwidth by expanding color to floats in the shader
        glVertexAttribPointer(colorAttribute, 4, GL_UNSIGNED_BYTE, GL_TRUE, sizeof(VertexData),
            (void*)offsetof(VertexData, rgba));

        CheckErrorGL();

        // Cleanup
        glBindBuffer(GL_ARRAY_BUFFER, 0);
        glBindVertexArray(0);
    }

    public void Destroy()
    {
        if (m_vaoId)
        {
            glDeleteVertexArrays(1, &m_vaoId);
            glDeleteBuffers(1, &m_vboId);
            m_vaoId = 0;
            m_vboId = 0;
        }

        if (m_programId)
        {
            glDeleteProgram(m_programId);
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

        glUseProgram(m_programId);

        float proj[16] =  {
            0.0f
        }
        ;
        Draw.g_camera.BuildProjectionMatrix(proj, 0.1f);

        glUniformMatrix4fv(m_projectionUniform, 1, GL_FALSE, proj);

        glBindVertexArray(m_vaoId);

        glBindBuffer(GL_ARRAY_BUFFER, m_vboId);

        int base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);
            glBufferSubData(GL_ARRAY_BUFFER, 0, batchCount * sizeof(VertexData), &m_points[base]);

            glDrawArrays(GL_LINES, 0, batchCount);

            CheckErrorGL();

            count -= e_batchSize;
            base += e_batchSize;
        }

        glBindBuffer(GL_ARRAY_BUFFER, 0);
        glBindVertexArray(0);
        glUseProgram(0);

        m_points.clear();
    }
}
