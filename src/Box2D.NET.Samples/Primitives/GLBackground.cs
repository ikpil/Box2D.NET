// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public class GLBackground
{
    uint m_vaoId;
    uint m_vboId;
    uint m_programId;
    int m_timeUniform;
    int m_resolutionUniform;
    int m_baseColorUniform;
    
    public void Create()
    {
        m_programId = CreateProgramFromFiles( "samples/data/background.vs", "samples/data/background.fs" );
        m_timeUniform = glGetUniformLocation( m_programId, "time" );
        m_resolutionUniform = glGetUniformLocation( m_programId, "resolution" );
        m_baseColorUniform = glGetUniformLocation( m_programId, "baseColor" );
        int vertexAttribute = 0;

        // Generate
        glGenVertexArrays( 1, &m_vaoId );
        glGenBuffers( 1, &m_vboId );

        glBindVertexArray( m_vaoId );
        glEnableVertexAttribArray( vertexAttribute );

        // Single quad
        b2Vec2 vertices[] = { { -1.0f, 1.0f }, { -1.0f, -1.0f }, { 1.0f, 1.0f }, { 1.0f, -1.0f } };
        glBindBuffer( GL_ARRAY_BUFFER, m_vboId );
        glBufferData( GL_ARRAY_BUFFER, sizeof( vertices ), vertices, GL_STATIC_DRAW );
        glVertexAttribPointer( vertexAttribute, 2, GL_FLOAT, GL_FALSE, 0, BUFFER_OFFSET( 0 ) );

        CheckErrorGL();

        // Cleanup
        glBindBuffer( GL_ARRAY_BUFFER, 0 );
        glBindVertexArray( 0 );
    }

    public void Destroy()
    {
        if ( m_vaoId )
        {
            glDeleteVertexArrays( 1, &m_vaoId );
            glDeleteBuffers( 1, &m_vboId );
            m_vaoId = 0;
            m_vboId = 0;
        }

        if ( m_programId )
        {
            glDeleteProgram( m_programId );
            m_programId = 0;
        }
    }

    public void Draw()
    {
        glUseProgram( m_programId );

        float time = (float)glfwGetTime();
        time = fmodf(time, 100.0f);
		
        glUniform1f( m_timeUniform, time );
        glUniform2f( m_resolutionUniform, (float)Draw.g_camera.m_width, (float)Draw.g_camera.m_height );

        // struct RGBA8 c8 = RGBA8.MakeRGBA8( b2_colorGray2, 1.0f );
        // glUniform3f(m_baseColorUniform, c8.r/255.0f, c8.g/255.0f, c8.b/255.0f);
        glUniform3f( m_baseColorUniform, 0.2f, 0.2f, 0.2f );

        glBindVertexArray( m_vaoId );

        glBindBuffer( GL_ARRAY_BUFFER, m_vboId );
        glDrawArrays( GL_TRIANGLE_STRIP, 0, 4 );
        glBindBuffer( GL_ARRAY_BUFFER, 0 );
        glBindVertexArray( 0 );
        glUseProgram( 0 );
    }

}

