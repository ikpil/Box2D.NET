using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using static Box2D.NET.math_function;

namespace Box2D.NET.Samples.Samples.Collisions;

public class ShapeCast : Sample
{
    public const int e_vertexCount = 8;
    
    b2Vec2 m_vAs[B2_MAX_POLYGON_VERTICES];
    int m_countA;
    float m_radiusA;

    b2Vec2 m_vBs[B2_MAX_POLYGON_VERTICES];
    int m_countB;
    float m_radiusB;

    b2Transform m_transformA;
    b2Transform m_transformB;
    b2Vec2 m_translationB;
    bool m_rayDrag;

    static int sampleShapeCast = RegisterSample( "Collision", "Shape Cast", ShapeCast::Create );
    static Sample Create( Settings settings )
    {
        return new ShapeCast( settings );
    }


    public ShapeCast( Settings settings )
        : base( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { -1.5f, 1.0f };
            Draw.g_camera.m_zoom = 25.0f * 0.2f;
        }

    #if ZERO_DEFINE
        // box swept against a triangle
        m_vAs[0] = {-0.5f, 1.0f};
        m_vAs[1] = {0.5f, 1.0f};
        m_vAs[2] = {0.0f, 0.0f};
        m_countA = 3;
        m_radiusA = 0.0f;

        m_vBs[0] = {-0.5f, -0.5f};
        m_vBs[1] = {0.5f, -0.5f};
        m_vBs[2] = {0.5f, 0.5f};
        m_vBs[3] = {-0.5f, 0.5f};
        m_countB = 4;
        m_radiusB = 0.0f;

        m_transformA.p = {0.0f, 0.25f};
        m_transformA.q = b2Rot_identity;
        m_transformB.p = {-4.0f, 0.0f};
        m_transformB.q = b2Rot_identity;
        m_translationB = {8.0f, 0.0f};
    #elif 1
        // box swept against a segment
        m_vAs[0] = { -2.0f, 0.0f };
        m_vAs[1] = { 2.0f, 0.0f };
        m_countA = 2;
        m_radiusA = 0.0f;

        m_vBs[0] = { -0.25f, -0.25f };
        m_vBs[1] = { 0.25f, -0.25f };
        m_vBs[2] = { 0.25f, 0.25f };
        m_vBs[3] = { -0.25f, 0.25f };
        m_countB = 4;
        m_radiusB = 0.25f;

        m_transformA.p = { 0.0f, 0.0 };
        m_transformA.q = b2MakeRot( 0.25f * B2_PI );
        m_transformB.p = { -8.0f, 0.0f };
        m_transformB.q = b2Rot_identity;
        m_translationB = { 8.0f, 0.0f };
    #elif 0
        // A point swept against a box
        m_vAs[0] = { -0.5f, -0.5f };
        m_vAs[1] = { 0.5f, -0.5f };
        m_vAs[2] = { 0.5f, 0.5f };
        m_vAs[3] = { -0.5f, 0.5f };
        m_countA = 4;
        m_radiusA = 0.0f;

        m_vBs[0] = { 0.0f, 0.0f };
        m_countB = 1;
        m_radiusB = 0.0f;

        m_transformA.p = { 0.0f, 0.0f };
        m_transformA.q = b2Rot_identity;
        m_transformB.p = { -1.0f, 0.0f };
        m_transformB.q = b2Rot_identity;
        m_translationB = { 1.0f, 0.0f };
    #elif 0
        m_vAs[0] = { 0.0f, 0.0f };
        m_countA = 1;
        m_radiusA = 0.5f;

        m_vBs[0] = { 0.0f, 0.0f };
        m_countB = 1;
        m_radiusB = 0.5f;

        m_transformA.p = { 0.0f, 0.25f };
        m_transformA.q = b2Rot_identity;
        m_transformB.p = { -4.0f, 0.0f };
        m_transformB.q = b2Rot_identity;
        m_translationB = { 8.0f, 0.0f };
    #else
        m_vAs[0] = { 0.0f, 0.0f };
        m_vAs[1] = { 2.0f, 0.0f };
        m_countA = 2;
        m_radiusA = 0.0f;

        m_vBs[0] = { 0.0f, 0.0f };
        m_countB = 1;
        m_radiusB = 0.25f;

        // Initial overlap
        m_transformA.p = b2Vec2_zero;
        m_transformA.q = b2Rot_identity;
        m_transformB.p = { -0.244360745f, 0.05999358f };
        m_transformB.q = b2Rot_identity;
        m_translationB = { 0.0f, 0.0399999991f };
    #endif

        m_rayDrag = false;
    }


    public override void MouseDown( b2Vec2 p, int button, int mods )
    {
        if ( button == (int)MouseButton.Left )
        {
            m_transformB.p = p;
            m_rayDrag = true;
        }
    }

    public override void MouseUp( b2Vec2 _, int button )
    {
        if ( button == (int)MouseButton.Left )
        {
            m_rayDrag = false;
        }
    }

    public override void MouseMove( b2Vec2 p )
    {
        if ( m_rayDrag )
        {
            m_translationB = b2Sub( p, m_transformB.p );
        }
    }

    public override void Step(Settings settings)
    {
        base.Step( settings );

        b2ShapeCastPairInput input = { };
        input.proxyA = b2MakeProxy( m_vAs, m_countA, m_radiusA );
        input.proxyB = b2MakeProxy( m_vBs, m_countB, m_radiusB );
        input.transformA = m_transformA;
        input.transformB = m_transformB;
        input.translationB = m_translationB;
        input.maxFraction = 1.0f;

        b2CastOutput output = b2ShapeCast( &input );

        b2Transform transformB2;
        transformB2.q = m_transformB.q;
        transformB2.p = b2MulAdd( m_transformB.p, output.fraction, input.translationB );

        b2DistanceInput distanceInput;
        distanceInput.proxyA = b2MakeProxy( m_vAs, m_countA, m_radiusA );
        distanceInput.proxyB = b2MakeProxy( m_vBs, m_countB, m_radiusB );
        distanceInput.transformA = m_transformA;
        distanceInput.transformB = transformB2;
        distanceInput.useRadii = false;
        b2SimplexCache distanceCache;
        distanceCache.count = 0;
        b2DistanceOutput distanceOutput = b2ShapeDistance( &distanceCache, &distanceInput, nullptr, 0 );

        Draw.g_draw.DrawString( 5, m_textLine, "hit = %s, iters = %d, lambda = %g, distance = %g", output.hit ? "true" : "false",
            output.iterations, output.fraction, distanceOutput.distance );
        m_textLine += m_textIncrement;

        b2Vec2 vertices[B2_MAX_POLYGON_VERTICES];

        for ( int i = 0; i < m_countA; ++i )
        {
            vertices[i] = b2TransformPoint( m_transformA, m_vAs[i] );
        }

        if ( m_countA == 1 )
        {
            if ( m_radiusA > 0.0f )
            {
                Draw.g_draw.DrawSolidCircle( b2Transform_identity, vertices[0], m_radiusA, b2HexColor.b2_colorLightGray );
            }
            else
            {
                Draw.g_draw.DrawPoint( vertices[0], 5.0f, b2HexColor.b2_colorLightGray );
            }
        }
        else
        {
            Draw.g_draw.DrawSolidPolygon( b2Transform_identity, vertices, m_countA, m_radiusA, b2HexColor.b2_colorLightGray );
        }

        for ( int i = 0; i < m_countB; ++i )
        {
            vertices[i] = b2TransformPoint( m_transformB, m_vBs[i] );
        }

        if ( m_countB == 1 )
        {
            if ( m_radiusB > 0.0f )
            {
                Draw.g_draw.DrawSolidCircle( b2Transform_identity, vertices[0], m_radiusB, b2HexColor.b2_colorGreen );
            }
            else
            {
                Draw.g_draw.DrawPoint( vertices[0], 5.0f, b2HexColor.b2_colorGreen );
            }
        }
        else
        {
            Draw.g_draw.DrawSolidPolygon( b2Transform_identity, vertices, m_countB, m_radiusB, b2HexColor.b2_colorGreen );
        }

        for ( int i = 0; i < m_countB; ++i )
        {
            vertices[i] = b2TransformPoint( transformB2, m_vBs[i] );
        }

        if ( m_countB == 1 )
        {
            if ( m_radiusB > 0.0f )
            {
                Draw.g_draw.DrawSolidCircle( b2Transform_identity, vertices[0], m_radiusB, b2HexColor.b2_colorOrange );
            }
            else
            {
                Draw.g_draw.DrawPoint( vertices[0], 5.0f, b2HexColor.b2_colorOrange );
            }
        }
        else
        {
            Draw.g_draw.DrawSolidPolygon( b2Transform_identity, vertices, m_countB, m_radiusB, b2HexColor.b2_colorOrange );
        }

        if ( output.hit )
        {
            b2Vec2 p1 = output.point;
            Draw.g_draw.DrawPoint( p1, 10.0f, b2HexColor.b2_colorRed );
            b2Vec2 p2 = b2MulAdd( p1, 1.0f, output.normal );
            Draw.g_draw.DrawSegment( p1, p2, b2HexColor.b2_colorRed );
        }

        Draw.g_draw.DrawSegment( m_transformB.p, b2Add( m_transformB.p, m_translationB ), b2HexColor.b2_colorGray );
    }

}

