using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

public class GLCircles
{
    public const int e_batchSize = 2048;

    std::vector<CircleData> m_circles;

    GLuint m_vaoId;
    GLuint m_vboIds[2];
    GLuint m_programId;
    GLint m_projectionUniform;
    GLint m_pixelScaleUniform;


    public void Create()
    {
        m_programId = CreateProgramFromFiles("samples/data/circle.vs", "samples/data/circle.fs");
        m_projectionUniform = glGetUniformLocation(m_programId, "projectionMatrix");
        m_pixelScaleUniform = glGetUniformLocation(m_programId, "pixelScale");
        int vertexAttribute = 0;
        int positionInstance = 1;
        int radiusInstance = 2;
        int colorInstance = 3;

        // Generate
        glGenVertexArrays(1, &m_vaoId);
        glGenBuffers(2, m_vboIds);

        glBindVertexArray(m_vaoId);
        glEnableVertexAttribArray(vertexAttribute);
        glEnableVertexAttribArray(positionInstance);
        glEnableVertexAttribArray(radiusInstance);
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

        // Circle buffer
        glBindBuffer(GL_ARRAY_BUFFER, m_vboIds[1]);
        glBufferData(GL_ARRAY_BUFFER, e_batchSize * sizeof(CircleData), nullptr, GL_DYNAMIC_DRAW);

        glVertexAttribPointer(positionInstance, 2, GL_FLOAT, GL_FALSE, sizeof(CircleData),
            (void*)offsetof(CircleData, position));
        glVertexAttribPointer(radiusInstance, 1, GL_FLOAT, GL_FALSE, sizeof(CircleData),
            (void*)offsetof(CircleData, radius));
        glVertexAttribPointer(colorInstance, 4, GL_UNSIGNED_BYTE, GL_TRUE, sizeof(CircleData),
            (void*)offsetof(CircleData, rgba));

        glVertexAttribDivisor(positionInstance, 1);
        glVertexAttribDivisor(radiusInstance, 1);
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

    public void AddCircle(b2Vec2 center, float radius, b2HexColor color)
    {
        RGBA8 rgba = MakeRGBA8(color, 1.0f);
        m_circles.push_back( {
            center, radius, rgba
        } );
    }

    public void Flush()
    {
        int count = (int)m_circles.size();
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

            glBufferSubData(GL_ARRAY_BUFFER, 0, batchCount * sizeof(CircleData), &m_circles[base]);
            glDrawArraysInstanced(GL_TRIANGLES, 0, 6, batchCount);

            CheckErrorGL();

            count -= e_batchSize;
            base += e_batchSize;
        }

        glDisable(GL_BLEND);

        glBindBuffer(GL_ARRAY_BUFFER, 0);
        glBindVertexArray(0);
        glUseProgram(0);

        m_circles.clear();
    }
}