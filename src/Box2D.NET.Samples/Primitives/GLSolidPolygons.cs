using Box2D.NET.Primitives;

namespace Box2D.NET.Samples.Primitives;

// Rounded and non-rounded convex polygons using an SDF-based shader.
public class GLSolidPolygons
{
    public const int e_batchSize = 512;

    std::vector<PolygonData> m_polygons;

    GLuint m_vaoId;
    GLuint m_vboIds[2];
    GLuint m_programId;
    GLint m_projectionUniform;
    GLint m_pixelScaleUniform;

    public void Create()
    {
        m_programId = CreateProgramFromFiles( "samples/data/solid_polygon.vs", "samples/data/solid_polygon.fs" );

        m_projectionUniform = glGetUniformLocation( m_programId, "projectionMatrix" );
        m_pixelScaleUniform = glGetUniformLocation( m_programId, "pixelScale" );
        int vertexAttribute = 0;
        int instanceTransform = 1;
        int instancePoint12 = 2;
        int instancePoint34 = 3;
        int instancePoint56 = 4;
        int instancePoint78 = 5;
        int instancePointCount = 6;
        int instanceRadius = 7;
        int instanceColor = 8;

        // Generate
        glGenVertexArrays( 1, &m_vaoId );
        glGenBuffers( 2, m_vboIds );

        glBindVertexArray( m_vaoId );
        glEnableVertexAttribArray( vertexAttribute );
        glEnableVertexAttribArray( instanceTransform );
        glEnableVertexAttribArray( instancePoint12 );
        glEnableVertexAttribArray( instancePoint34 );
        glEnableVertexAttribArray( instancePoint56 );
        glEnableVertexAttribArray( instancePoint78 );
        glEnableVertexAttribArray( instancePointCount );
        glEnableVertexAttribArray( instanceRadius );
        glEnableVertexAttribArray( instanceColor );

        // Vertex buffer for single quad
        float a = 1.1f;
        b2Vec2 vertices[] = { { -a, -a }, { a, -a }, { -a, a }, { a, -a }, { a, a }, { -a, a } };
        glBindBuffer( GL_ARRAY_BUFFER, m_vboIds[0] );
        glBufferData( GL_ARRAY_BUFFER, sizeof( vertices ), vertices, GL_STATIC_DRAW );
        glVertexAttribPointer( vertexAttribute, 2, GL_FLOAT, GL_FALSE, 0, BUFFER_OFFSET( 0 ) );

        // Polygon buffer
        glBindBuffer( GL_ARRAY_BUFFER, m_vboIds[1] );
        glBufferData( GL_ARRAY_BUFFER, e_batchSize * sizeof( PolygonData ), nullptr, GL_DYNAMIC_DRAW );
        glVertexAttribPointer( instanceTransform, 4, GL_FLOAT, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, transform ) );
        glVertexAttribPointer( instancePoint12, 4, GL_FLOAT, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, p1 ) );
        glVertexAttribPointer( instancePoint34, 4, GL_FLOAT, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, p3 ) );
        glVertexAttribPointer( instancePoint56, 4, GL_FLOAT, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, p5 ) );
        glVertexAttribPointer( instancePoint78, 4, GL_FLOAT, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, p7 ) );
        glVertexAttribIPointer( instancePointCount, 1, GL_INT, sizeof( PolygonData ), (void*)offsetof( PolygonData, count ) );
        glVertexAttribPointer( instanceRadius, 1, GL_FLOAT, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, radius ) );
        // color will get automatically expanded to floats in the shader
        glVertexAttribPointer( instanceColor, 4, GL_UNSIGNED_BYTE, GL_TRUE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, color ) );

        // These divisors tell glsl how to distribute per instance data
        glVertexAttribDivisor( instanceTransform, 1 );
        glVertexAttribDivisor( instancePoint12, 1 );
        glVertexAttribDivisor( instancePoint34, 1 );
        glVertexAttribDivisor( instancePoint56, 1 );
        glVertexAttribDivisor( instancePoint78, 1 );
        glVertexAttribDivisor( instancePointCount, 1 );
        glVertexAttribDivisor( instanceRadius, 1 );
        glVertexAttribDivisor( instanceColor, 1 );

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
            glDeleteBuffers( 2, m_vboIds );
            m_vaoId = 0;
        }

        if ( m_programId )
        {
            glDeleteProgram( m_programId );
            m_programId = 0;
        }
    }

    public void AddPolygon( const b2Transform& transform, const b2Vec2* points, int count, float radius, b2HexColor color )
    {
        PolygonData data = {};
        data.transform = transform;

        int n = count < 8 ? count : 8;
        b2Vec2* ps = &data.p1;
        for ( int i = 0; i < n; ++i )
        {
            ps[i] = points[i];
        }

        data.count = n;
        data.radius = radius;
        data.color = MakeRGBA8( color, 1.0f );

        m_polygons.push_back( data );
    }

    public void Flush()
    {
        int count = (int)m_polygons.size();
        if ( count == 0 )
        {
            return;
        }

        glUseProgram( m_programId );

        float proj[16] = { 0.0f };
        Draw.g_camera.BuildProjectionMatrix( proj, 0.2f );

        glUniformMatrix4fv( m_projectionUniform, 1, GL_FALSE, proj );
        glUniform1f( m_pixelScaleUniform, Draw.g_camera.m_height / Draw.g_camera.m_zoom );

        glBindVertexArray( m_vaoId );
        glBindBuffer( GL_ARRAY_BUFFER, m_vboIds[1] );

        glEnable( GL_BLEND );
        glBlendFunc( GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA );

        int base = 0;
        while ( count > 0 )
        {
            int batchCount = b2MinInt( count, e_batchSize );

            glBufferSubData( GL_ARRAY_BUFFER, 0, batchCount * sizeof( PolygonData ), &m_polygons[base] );
            glDrawArraysInstanced( GL_TRIANGLES, 0, 6, batchCount );
            CheckErrorGL();

            count -= e_batchSize;
            base += e_batchSize;
        }

        glDisable( GL_BLEND );

        glBindBuffer( GL_ARRAY_BUFFER, 0 );
        glBindVertexArray( 0 );
        glUseProgram( 0 );

        m_polygons.clear();
    }

}
