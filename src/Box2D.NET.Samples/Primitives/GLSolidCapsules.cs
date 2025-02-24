using System.Collections.Generic;
using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

// Draw capsules using SDF-based shader
public class GLSolidCapsules
{
    public const int e_batchSize = 2048;

    List<CapsuleData> m_capsules;

    GLuint m_vaoId;
    GLuint m_vboIds[2];
    GLuint m_programId;
    GLint m_projectionUniform;
    GLint m_pixelScaleUniform;

    public void Create()
    {
        m_programId = CreateProgramFromFiles("samples/data/solid_capsule.vs", "samples/data/solid_capsule.fs");

        m_projectionUniform = glGetUniformLocation(m_programId, "projectionMatrix");
        m_pixelScaleUniform = glGetUniformLocation(m_programId, "pixelScale");

        int vertexAttribute = 0;
        int transformInstance = 1;
        int radiusInstance = 2;
        int lengthInstance = 3;
        int colorInstance = 4;

        // Generate
        glGenVertexArrays(1, &m_vaoId);
        glGenBuffers(2, m_vboIds);

        glBindVertexArray(m_vaoId);
        glEnableVertexAttribArray(vertexAttribute);
        glEnableVertexAttribArray(transformInstance);
        glEnableVertexAttribArray(radiusInstance);
        glEnableVertexAttribArray(lengthInstance);
        glEnableVertexAttribArray(colorInstance);

        // Vertex buffer for single quad
        float a = 1.1f;
        b2Vec2 vertices[] =  {
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
        glBindBuffer(GL_ARRAY_BUFFER, m_vboIds[0]);
        glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);
        glVertexAttribPointer(vertexAttribute, 2, GL_FLOAT, GL_FALSE, 0, BUFFER_OFFSET(0));

        // Capsule buffer
        glBindBuffer(GL_ARRAY_BUFFER, m_vboIds[1]);
        glBufferData(GL_ARRAY_BUFFER, e_batchSize * sizeof(CapsuleData), nullptr, GL_DYNAMIC_DRAW);

        glVertexAttribPointer(transformInstance, 4, GL_FLOAT, GL_FALSE, sizeof(CapsuleData),
            (void*)offsetof(CapsuleData, transform));
        glVertexAttribPointer(radiusInstance, 1, GL_FLOAT, GL_FALSE, sizeof(CapsuleData),
            (void*)offsetof(CapsuleData, radius));
        glVertexAttribPointer(lengthInstance, 1, GL_FLOAT, GL_FALSE, sizeof(CapsuleData),
            (void*)offsetof(CapsuleData, length));
        glVertexAttribPointer(colorInstance, 4, GL_UNSIGNED_BYTE, GL_TRUE, sizeof(CapsuleData),
            (void*)offsetof(CapsuleData, rgba));

        glVertexAttribDivisor(transformInstance, 1);
        glVertexAttribDivisor(radiusInstance, 1);
        glVertexAttribDivisor(lengthInstance, 1);
        glVertexAttribDivisor(colorInstance, 1);

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
            glDeleteBuffers(2, m_vboIds);
            m_vaoId = 0;
            m_vboIds[0] = 0;
            m_vboIds[1] = 0;
        }

        if (m_programId)
        {
            glDeleteProgram(m_programId);
            m_programId = 0;
        }
    }

    public void AddCapsule(b2Vec2 p1, b2Vec2 p2, float radius, b2HexColor c)
    {
        b2Vec2 d = p2 - p1;
        float length = b2Length(d);
        if (length < 0.001f)
        {
            printf("WARNING: sample app: capsule too short!\n");
            return;
        }

        b2Vec2 axis = { d.x / length, d.y / length };
        b2Transform transform;
        transform.p = 0.5f * (p1 + p2);
        transform.q.c = axis.x;
        transform.q.s = axis.y;

        RGBA8 rgba = RGBA8.MakeRGBA8(c, 1.0f);

        m_capsules.push_back( {
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

        glUseProgram(m_programId);

        float proj[16] =  {
            0.0f
        }
        ;
        Draw.g_camera.BuildProjectionMatrix(proj, 0.2f);

        glUniformMatrix4fv(m_projectionUniform, 1, GL_FALSE, proj);
        glUniform1f(m_pixelScaleUniform, Draw.g_camera.m_height / Draw.g_camera.m_zoom);

        glBindVertexArray(m_vaoId);

        glBindBuffer(GL_ARRAY_BUFFER, m_vboIds[1]);
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

        int base = 0;
        while (count > 0)
        {
            int batchCount = b2MinInt(count, e_batchSize);

            glBufferSubData(GL_ARRAY_BUFFER, 0, batchCount * sizeof(CapsuleData), &m_capsules[base]);
            glDrawArraysInstanced(GL_TRIANGLES, 0, 6, batchCount);

            CheckErrorGL();

            count -= e_batchSize;
            base += e_batchSize;
        }

        glDisable(GL_BLEND);

        glBindBuffer(GL_ARRAY_BUFFER, 0);
        glBindVertexArray(0);
        glUseProgram(0);

        m_capsules.clear();
    }
}