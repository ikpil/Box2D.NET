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

    uint[] m_vaoId = new uint[1];
    uint[] m_vboIds = new uint[2];
    uint m_programId;
    int m_projectionUniform;
    int m_pixelScaleUniform;

    public void Create()
    {
        m_programId = B2.g_shader.CreateProgramFromFiles( "samples/data/solid_polygon.vs", "samples/data/solid_polygon.fs" );

        m_projectionUniform = B2.g_shader.Gl.GetUniformLocation( m_programId, "projectionMatrix" );
        m_pixelScaleUniform = B2.g_shader.Gl.GetUniformLocation( m_programId, "pixelScale" );
        uint vertexAttribute = 0;
        uint instanceTransform = 1;
        uint instancePoint12 = 2;
        uint instancePoint34 = 3;
        uint instancePoint56 = 4;
        uint instancePoint78 = 5;
        uint instancePointCount = 6;
        uint instanceRadius = 7;
        uint instanceColor = 8;

        // Generate
        B2.g_shader.Gl.GenVertexArrays( m_vaoId );
        B2.g_shader.Gl.GenBuffers( m_vboIds );

        B2.g_shader.Gl.BindVertexArray( m_vaoId[0] );
        B2.g_shader.Gl.EnableVertexAttribArray( vertexAttribute );
        B2.g_shader.Gl.EnableVertexAttribArray( instanceTransform );
        B2.g_shader.Gl.EnableVertexAttribArray( instancePoint12 );
        B2.g_shader.Gl.EnableVertexAttribArray( instancePoint34 );
        B2.g_shader.Gl.EnableVertexAttribArray( instancePoint56 );
        B2.g_shader.Gl.EnableVertexAttribArray( instancePoint78 );
        B2.g_shader.Gl.EnableVertexAttribArray( instancePointCount );
        B2.g_shader.Gl.EnableVertexAttribArray( instanceRadius );
        B2.g_shader.Gl.EnableVertexAttribArray( instanceColor );

        // Vertex buffer for single quad
        float a = 1.1f;
        B2Vec2 vertices[] = { { -a, -a }, { a, -a }, { -a, a }, { a, -a }, { a, a }, { -a, a } };
        B2.g_shader.Gl.BindBuffer( GLEnum.ArrayBuffer, m_vboIds[0] );
        B2.g_shader.Gl.BufferData( GLEnum.ArrayBuffer, sizeof( vertices ), vertices, GLEnum.StaticDraw );
        B2.g_shader.Gl.VertexAttribPointer( vertexAttribute, 2, VertexAttribPointerType.Float, GL_FALSE, 0, BUFFER_OFFSET( 0 ) );

        // Polygon buffer
        B2.g_shader.Gl.BindBuffer( GLEnum.ArrayBuffer, m_vboIds[1] );
        B2.g_shader.Gl.BufferData( GLEnum.ArrayBuffer, e_batchSize * sizeof( PolygonData ), nullptr, GLEnum.DynamicDraw );
        B2.g_shader.Gl.VertexAttribPointer( instanceTransform, 4, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ), (void*)offsetof( PolygonData, transform ) );
        B2.g_shader.Gl.VertexAttribPointer( instancePoint12, 4, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ), (void*)offsetof( PolygonData, p1 ) );
        B2.g_shader.Gl.VertexAttribPointer( instancePoint34, 4, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ), (void*)offsetof( PolygonData, p3 ) );
        B2.g_shader.Gl.VertexAttribPointer( instancePoint56, 4, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ), (void*)offsetof( PolygonData, p5 ) );
        B2.g_shader.Gl.VertexAttribPointer( instancePoint78, 4, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ), (void*)offsetof( PolygonData, p7 ) );
        glVertexAttribIPointer( instancePointCount, 1, GL_INT, sizeof( PolygonData ), (void*)offsetof( PolygonData, count ) );
        B2.g_shader.Gl.VertexAttribPointer( instanceRadius, 1, VertexAttribPointerType.Float, GL_FALSE, sizeof( PolygonData ), (void*)offsetof( PolygonData, radius ) );
        // color will get automatically expanded to floats in the shader
        B2.g_shader.Gl.VertexAttribPointer( instanceColor, 4, VertexAttribPointerType.UnsignedByte, GL_TRUE, sizeof( PolygonData ), (void*)offsetof( PolygonData, color ) );

        // These divisors tell glsl how to distribute per instance data
        B2.g_shader.Gl.VertexAttribDivisor( instanceTransform, 1 );
        B2.g_shader.Gl.VertexAttribDivisor( instancePoint12, 1 );
        B2.g_shader.Gl.VertexAttribDivisor( instancePoint34, 1 );
        B2.g_shader.Gl.VertexAttribDivisor( instancePoint56, 1 );
        B2.g_shader.Gl.VertexAttribDivisor( instancePoint78, 1 );
        B2.g_shader.Gl.VertexAttribDivisor( instancePointCount, 1 );
        B2.g_shader.Gl.VertexAttribDivisor( instanceRadius, 1 );
        B2.g_shader.Gl.VertexAttribDivisor( instanceColor, 1 );

        B2.g_shader.CheckErrorGL();

        // Cleanup
        B2.g_shader.Gl.BindBuffer( GLEnum.ArrayBuffer, 0 );
        B2.g_shader.Gl.BindVertexArray( 0 );
    }

    public void Destroy()
    {
        if ( m_vaoId )
        {
            B2.g_shader.Gl.DeleteVertexArrays( 1, &m_vaoId );
            B2.g_shader.Gl.DeleteBuffers( 2, m_vboIds );
            m_vaoId = 0;
        }

        if ( m_programId )
        {
            B2.g_shader.Gl.DeleteProgram( m_programId );
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

        B2.g_shader.Gl.UseProgram( m_programId );

        float proj[16] = { 0.0f };
        B2.g_camera.BuildProjectionMatrix( proj, 0.2f );

        glUniformMatrix4fv( m_projectionUniform, 1, GL_FALSE, proj );
        B2.g_shader.Gl.Uniform1( m_pixelScaleUniform, B2.g_camera.m_height / B2.g_camera.m_zoom );

        B2.g_shader.Gl.BindVertexArray( m_vaoId );
        B2.g_shader.Gl.BindBuffer( GLEnum.ArrayBuffer, m_vboIds[1] );

        B2.g_shader.Gl.Enable( GLEnum.Blend );
        B2.g_shader.Gl.BlendFunc( GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha );

        int base = 0;
        while ( count > 0 )
        {
            int batchCount = b2MinInt( count, e_batchSize );

            B2.g_shader.Gl.BufferSubData( GLEnum.ArrayBuffer, 0, batchCount * sizeof( PolygonData ), &m_polygons[base] );
            B2.g_shader.Gl.DrawArraysInstanced( GLEnum.Triangles, 0, 6, batchCount );
            B2.g_shader.CheckErrorGL();

            count -= e_batchSize;
            base += e_batchSize;
        }

        B2.g_shader.Gl.Disable( GLEnum.Blend );

        B2.g_shader.Gl.BindBuffer( GLEnum.ArrayBuffer, 0 );
        B2.g_shader.Gl.BindVertexArray( 0 );
        B2.g_shader.Gl.UseProgram( 0 );

        m_polygons.clear();
    }

}

