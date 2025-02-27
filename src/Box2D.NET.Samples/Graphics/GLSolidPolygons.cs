// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Silk.NET.OpenGL;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Graphics;

// Rounded and non-rounded convex polygons using an SDF-based shader.
public class GLSolidPolygons
{
    public const int e_batchSize = 512;

    List<PolygonData> m_polygons;

    uint m_vaoId;
    uint m_vboIds[2];
    uint m_programId;
    int m_projectionUniform;
    int m_pixelScaleUniform;

    public void Create()
    {
        m_programId = B2GL.Shared.CreateProgramFromFiles( "samples/data/solid_polygon.vs", "samples/data/solid_polygon.fs" );

        m_projectionUniform = B2GL.Shared.Gl.GetUniformLocation( m_programId, "projectionMatrix" );
        m_pixelScaleUniform = B2GL.Shared.Gl.GetUniformLocation( m_programId, "pixelScale" );
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
        B2GL.Shared.Gl.GenVertexArrays( 1, &m_vaoId );
        B2GL.Shared.Gl.GenBuffers( 2, m_vboIds );

        B2GL.Shared.Gl.BindVertexArray( m_vaoId );
        B2GL.Shared.Gl.EnableVertexAttribArray( vertexAttribute );
        B2GL.Shared.Gl.EnableVertexAttribArray( instanceTransform );
        B2GL.Shared.Gl.EnableVertexAttribArray( instancePoint12 );
        B2GL.Shared.Gl.EnableVertexAttribArray( instancePoint34 );
        B2GL.Shared.Gl.EnableVertexAttribArray( instancePoint56 );
        B2GL.Shared.Gl.EnableVertexAttribArray( instancePoint78 );
        B2GL.Shared.Gl.EnableVertexAttribArray( instancePointCount );
        B2GL.Shared.Gl.EnableVertexAttribArray( instanceRadius );
        B2GL.Shared.Gl.EnableVertexAttribArray( instanceColor );

        // Vertex buffer for single quad
        float a = 1.1f;
        B2Vec2 vertices[] = { { -a, -a }, { a, -a }, { -a, a }, { a, -a }, { a, a }, { -a, a } };
        B2GL.Shared.Gl.BindBuffer( GLEnum.ArrayBuffer, m_vboIds[0] );
        B2GL.Shared.Gl.BufferData( GLEnum.ArrayBuffer, sizeof( vertices ), vertices, GLEnum.StaticDraw );
        B2GL.Shared.Gl.VertexAttribPointer( vertexAttribute, 2, VertexAttribPointerType.Float, GL_FALSE, 0, BUFFER_OFFSET( 0 ) );

        // Polygon buffer
        B2GL.Shared.Gl.BindBuffer( GLEnum.ArrayBuffer, m_vboIds[1] );
        B2GL.Shared.Gl.BufferData( GLEnum.ArrayBuffer, e_batchSize * sizeof( PolygonData ), nullptr, GLEnum.DynamicDraw );
        B2GL.Shared.Gl.VertexAttribPointer( instanceTransform, 4, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, transform ) );
        B2GL.Shared.Gl.VertexAttribPointer( instancePoint12, 4, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, p1 ) );
        B2GL.Shared.Gl.VertexAttribPointer( instancePoint34, 4, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, p3 ) );
        B2GL.Shared.Gl.VertexAttribPointer( instancePoint56, 4, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, p5 ) );
        B2GL.Shared.Gl.VertexAttribPointer( instancePoint78, 4, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, p7 ) );
        glVertexAttribIPointer( instancePointCount, 1, GL_INT, sizeof( PolygonData ), (void*)offsetof( PolygonData, count ) );
        B2GL.Shared.Gl.VertexAttribPointer( instanceRadius, 1, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, radius ) );
        // color will get automatically expanded to floats in the shader
        B2GL.Shared.Gl.VertexAttribPointer( instanceColor, 4, VertexAttribPointerType.UnsignedByte, GL_TRUE, sizeof( PolygonData ),
            (void*)offsetof( PolygonData, color ) );

        // These divisors tell glsl how to distribute per instance data
        B2GL.Shared.Gl.VertexAttribDivisor( instanceTransform, 1 );
        B2GL.Shared.Gl.VertexAttribDivisor( instancePoint12, 1 );
        B2GL.Shared.Gl.VertexAttribDivisor( instancePoint34, 1 );
        B2GL.Shared.Gl.VertexAttribDivisor( instancePoint56, 1 );
        B2GL.Shared.Gl.VertexAttribDivisor( instancePoint78, 1 );
        B2GL.Shared.Gl.VertexAttribDivisor( instancePointCount, 1 );
        B2GL.Shared.Gl.VertexAttribDivisor( instanceRadius, 1 );
        B2GL.Shared.Gl.VertexAttribDivisor( instanceColor, 1 );

        CheckErrorGL();

        // Cleanup
        B2GL.Shared.Gl.BindBuffer( GLEnum.ArrayBuffer, 0 );
        B2GL.Shared.Gl.BindVertexArray( 0 );
    }

    public void Destroy()
    {
        if ( m_vaoId )
        {
            B2GL.Shared.Gl.DeleteVertexArrays( 1, &m_vaoId );
            B2GL.Shared.Gl.DeleteBuffers( 2, m_vboIds );
            m_vaoId = 0;
        }

        if ( m_programId )
        {
            B2GL.Shared.Gl.DeleteProgram( m_programId );
            m_programId = 0;
        }
    }

    public void AddPolygon( ref B2Transform transform, ReadOnlySpan<B2Vec2> points, int count, float radius, B2HexColor color )
    {
        PolygonData data = {};
        data.transform = transform;

        int n = count < 8 ? count : 8;
        B2Vec2* ps = &data.p1;
        for ( int i = 0; i < n; ++i )
        {
            ps[i] = points[i];
        }

        data.count = n;
        data.radius = radius;
        data.color = RGBA8.MakeRGBA8( color, 1.0f );

        m_polygons.Add( data );
    }

    public void Flush()
    {
        int count = (int)m_polygons.size();
        if ( count == 0 )
        {
            return;
        }

        B2GL.Shared.Gl.UseProgram( m_programId );

        float proj[16] = { 0.0f };
        Draw.g_camera.BuildProjectionMatrix( proj, 0.2f );

        glUniformMatrix4fv( m_projectionUniform, 1, GL_FALSE, proj );
        glUniform1f( m_pixelScaleUniform, Draw.g_camera.m_height / Draw.g_camera.m_zoom );

        B2GL.Shared.Gl.BindVertexArray( m_vaoId );
        B2GL.Shared.Gl.BindBuffer( GLEnum.ArrayBuffer, m_vboIds[1] );

        B2GL.Shared.Gl.Enable( GLEnum.Blend );
        B2GL.Shared.Gl.BlendFunc( GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha );

        int base = 0;
        while ( count > 0 )
        {
            int batchCount = b2MinInt( count, e_batchSize );

            B2GL.Shared.Gl.BufferSubData( GLEnum.ArrayBuffer, 0, batchCount * sizeof( PolygonData ), &m_polygons[base] );
            B2GL.Shared.Gl.DrawArraysInstanced( GLEnum.Triangles, 0, 6, batchCount );
            CheckErrorGL();

            count -= e_batchSize;
            base += e_batchSize;
        }

        B2GL.Shared.Gl.Disable( GLEnum.Blend );

        B2GL.Shared.Gl.BindBuffer( GLEnum.ArrayBuffer, 0 );
        B2GL.Shared.Gl.BindVertexArray( 0 );
        B2GL.Shared.Gl.UseProgram( 0 );

        m_polygons.clear();
    }

}

