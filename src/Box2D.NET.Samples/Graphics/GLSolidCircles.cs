// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;

namespace Box2D.NET.Samples.Graphics;

// Draws SDF circles using quad instancing. Apparently instancing of quads can be slow on older GPUs.
// https://www.reddit.com/r/opengl/comments/q7yikr/how_to_draw_several_quads_through_instancing/
// https://www.g-truc.net/post-0666.html
public class GLSolidCircles
{
    public const int e_batchSize = 2048;

    List<SolidCircleData> m_circles;

    uint m_vaoId;
    uint m_vboIds[2];
    uint m_programId;
    int m_projectionUniform;
    int m_pixelScaleUniform;

    public void Create()
    {
        m_programId = B2GL.Shared.CreateProgramFromFiles("samples/data/solid_circle.vs", "samples/data/solid_circle.fs");
        m_projectionUniform = B2GL.Shared.Gl.GetUniformLocation(m_programId, "projectionMatrix");
        m_pixelScaleUniform = B2GL.Shared.Gl.GetUniformLocation(m_programId, "pixelScale");

        // Generate
        B2GL.Shared.Gl.GenVertexArrays(1, &m_vaoId);
        B2GL.Shared.Gl.GenBuffers(2, m_vboIds);

        B2GL.Shared.Gl.BindVertexArray(m_vaoId);

        int vertexAttribute = 0;
        int transformInstance = 1;
        int radiusInstance = 2;
        int colorInstance = 3;
        B2GL.Shared.Gl.EnableVertexAttribArray(vertexAttribute);
        B2GL.Shared.Gl.EnableVertexAttribArray(transformInstance);
        B2GL.Shared.Gl.EnableVertexAttribArray(radiusInstance);
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
        B2GL.Shared.Gl.BindBuffer(GL_ARRAY_BUFFER, m_vboIds[0]);
        B2GL.Shared.Gl.BufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);
        B2GL.Shared.Gl.VertexAttribPointer(vertexAttribute, 2, GL_FLOAT, GL_FALSE, 0, BUFFER_OFFSET(0));

        // Circle buffer
        B2GL.Shared.Gl.BindBuffer(GL_ARRAY_BUFFER, m_vboIds[1]);
        B2GL.Shared.Gl.BufferData(GL_ARRAY_BUFFER, e_batchSize * sizeof(SolidCircleData), nullptr, GL_DYNAMIC_DRAW);

        B2GL.Shared.Gl.VertexAttribPointer(transformInstance, 4, GL_FLOAT, GL_FALSE, sizeof(SolidCircleData),
            (void*)offsetof(SolidCircleData, transform));
        B2GL.Shared.Gl.VertexAttribPointer(radiusInstance, 1, GL_FLOAT, GL_FALSE, sizeof(SolidCircleData),
            (void*)offsetof(SolidCircleData, radius));
        B2GL.Shared.Gl.VertexAttribPointer(colorInstance, 4, GL_UNSIGNED_BYTE, GL_TRUE, sizeof(SolidCircleData),
            (void*)offsetof(SolidCircleData, rgba));

        B2GL.Shared.Gl.VertexAttribDivisor(transformInstance, 1);
        B2GL.Shared.Gl.VertexAttribDivisor(radiusInstance, 1);
        B2GL.Shared.Gl.VertexAttribDivisor(colorInstance, 1);

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

    public void AddCircle(ref B2Transform transform, float radius, B2HexColor color)
    {
        RGBA8 rgba = RGBA8.MakeRGBA8(color, 1.0f);
        m_circles.Add( {
            transform, radius, rgba
        } );
    }

    public void Flush()
    {
        int count = (int)m_circles.size();
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
        glUniform1f(m_pixelScaleUniform, Draw.g_camera.m_height / Draw.g_camera.m_zoom);

        B2GL.Shared.Gl.BindVertexArray(m_vaoId);

        B2GL.Shared.Gl.BindBuffer(GL_ARRAY_BUFFER, m_vboIds[1]);
        B2GL.Shared.Gl.Enable(GL_BLEND);
        B2GL.Shared.Gl.BlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

        int base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            B2GL.Shared.Gl.BufferSubData(GL_ARRAY_BUFFER, 0, batchCount * sizeof(SolidCircleData), &m_circles[base]);
            B2GL.Shared.Gl.DrawArraysInstanced(GL_TRIANGLES, 0, 6, batchCount);

            CheckErrorGL();

            count -= e_batchSize;
            base += e_batchSize;
        }

        B2GL.Shared.Gl.Disable(GL_BLEND);

        B2GL.Shared.Gl.BindBuffer(GL_ARRAY_BUFFER, 0);
        B2GL.Shared.Gl.BindVertexArray(0);
        B2GL.Shared.Gl.UseProgram(0);

        m_circles.clear();
    }
}
