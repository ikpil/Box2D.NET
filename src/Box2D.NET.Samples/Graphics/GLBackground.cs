// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Graphics;

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
        m_programId = B2GL.Shared.CreateProgramFromFiles( "samples/data/background.vs", "samples/data/background.fs" );
        m_timeUniform = B2GL.Shared.Gl.GetUniformLocation( m_programId, "time" );
        m_resolutionUniform = B2GL.Shared.Gl.GetUniformLocation( m_programId, "resolution" );
        m_baseColorUniform = B2GL.Shared.Gl.GetUniformLocation( m_programId, "baseColor" );
        int vertexAttribute = 0;

        // Generate
        B2GL.Shared.Gl.GenVertexArrays( 1, &m_vaoId );
        B2GL.Shared.Gl.GenBuffers( 1, &m_vboId );

        B2GL.Shared.Gl.BindVertexArray( m_vaoId );
        B2GL.Shared.Gl.EnableVertexAttribArray( vertexAttribute );

        // Single quad
        b2Vec2 vertices[] = { { -1.0f, 1.0f }, { -1.0f, -1.0f }, { 1.0f, 1.0f }, { 1.0f, -1.0f } };
        B2GL.Shared.Gl.BindBuffer( GL_ARRAY_BUFFER, m_vboId );
        B2GL.Shared.Gl.BufferData( GL_ARRAY_BUFFER, sizeof( vertices ), vertices, GL_STATIC_DRAW );
        B2GL.Shared.Gl.VertexAttribPointer( vertexAttribute, 2, GL_FLOAT, GL_FALSE, 0, BUFFER_OFFSET( 0 ) );

        CheckErrorGL();

        // Cleanup
        B2GL.Shared.Gl.BindBuffer( GL_ARRAY_BUFFER, 0 );
        B2GL.Shared.Gl.BindVertexArray( 0 );
    }

    public void Destroy()
    {
        if ( m_vaoId )
        {
            B2GL.Shared.Gl.DeleteVertexArrays( 1, &m_vaoId );
            B2GL.Shared.Gl.DeleteBuffers( 1, &m_vboId );
            m_vaoId = 0;
            m_vboId = 0;
        }

        if ( m_programId )
        {
            B2GL.Shared.Gl.DeleteProgram( m_programId );
            m_programId = 0;
        }
    }

    public void Draw()
    {
        B2GL.Shared.Gl.UseProgram( m_programId );

        float time = (float)glfwGetTime();
        time = fmodf(time, 100.0f);
		
        glUniform1f( m_timeUniform, time );
        glUniform2f( m_resolutionUniform, (float)Draw.g_camera.m_width, (float)Draw.g_camera.m_height );

        // struct RGBA8 c8 = RGBA8.MakeRGBA8( b2_colorGray2, 1.0f );
        // glUniform3f(m_baseColorUniform, c8.r/255.0f, c8.g/255.0f, c8.b/255.0f);
        glUniform3f( m_baseColorUniform, 0.2f, 0.2f, 0.2f );

        B2GL.Shared.Gl.BindVertexArray( m_vaoId );

        B2GL.Shared.Gl.BindBuffer( GL_ARRAY_BUFFER, m_vboId );
        glDrawArrays( GL_TRIANGLE_STRIP, 0, 4 );
        B2GL.Shared.Gl.BindBuffer( GL_ARRAY_BUFFER, 0 );
        B2GL.Shared.Gl.BindVertexArray( 0 );
        B2GL.Shared.Gl.UseProgram( 0 );
    }

}

