using Box2D.NET.Primitives;
using static Box2D.NET.math_function;
using static Box2D.NET.distance;

namespace Box2D.NET.Samples.Samples.Collisions;

    class TimeOfImpact : Sample
    {
    public:
    explicit TimeOfImpact( Settings settings )
        : base( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.6f, 2.0f };
            Draw.g_camera.m_center = { -16, 45 };
            Draw.g_camera.m_zoom = 5.0f;
        }
    }

    static Sample Create( Settings settings )
    {
        return new TimeOfImpact( settings );
    }

    public override void Step(Settings settings)
    {
        base.Step( settings );

        b2Sweep sweepA = {
            b2Vec2_zero, { 0.0f, 0.0f }, { 0.0f, 0.0f }, b2Rot_identity, b2Rot_identity,
        };
        b2Sweep sweepB = {
            b2Vec2_zero,
            { -15.8332710, 45.3520279 },
            { -15.8324337, 45.3413048 },
            { -0.540891349, 0.841092527 },
            { -0.457797021, 0.889056742 },
        };

        b2TOIInput input;
        input.proxyA = b2MakeProxy( m_verticesA, m_countA, m_radiusA );
        input.proxyB = b2MakeProxy( m_verticesB, m_countB, m_radiusB );
        input.sweepA = sweepA;
        input.sweepB = sweepB;
        input.maxFraction = 1.0f;

        b2TOIOutput output = b2TimeOfImpact( &input );

        Draw.g_draw.DrawString( 5, m_textLine, "toi = %g", output.fraction );
        m_textLine += m_textIncrement;

        // Draw.g_draw.DrawString(5, m_textLine, "max toi iters = %d, max root iters = %d", b2_toiMaxIters,
        //                        b2_toiMaxRootIters);
        m_textLine += m_textIncrement;

        b2Vec2 vertices[B2_MAX_POLYGON_VERTICES];

        // Draw A
        b2Transform transformA = b2GetSweepTransform( &sweepA, 0.0f );
        for ( int i = 0; i < m_countA; ++i )
        {
            vertices[i] = b2TransformPoint( transformA, m_verticesA[i] );
        }
        Draw.g_draw.DrawPolygon( vertices, m_countA, b2HexColor.b2_colorGray );

        // Draw B at t = 0
        b2Transform transformB = b2GetSweepTransform( &sweepB, 0.0f );
        for ( int i = 0; i < m_countB; ++i )
        {
            vertices[i] = b2TransformPoint( transformB, m_verticesB[i] );
        }
        Draw.g_draw.DrawSolidCapsule( vertices[0], vertices[1], m_radiusB, b2HexColor.b2_colorGreen );
        // Draw.g_draw.DrawPolygon( vertices, m_countB, b2HexColor.b2_colorGreen );

        // Draw B at t = hit_time
        transformB = b2GetSweepTransform( &sweepB, output.fraction );
        for ( int i = 0; i < m_countB; ++i )
        {
            vertices[i] = b2TransformPoint( transformB, m_verticesB[i] );
        }
        Draw.g_draw.DrawPolygon( vertices, m_countB, b2HexColor.b2_colorOrange );

        // Draw B at t = 1
        transformB = b2GetSweepTransform( &sweepB, 1.0f );
        for ( int i = 0; i < m_countB; ++i )
        {
            vertices[i] = b2TransformPoint( transformB, m_verticesB[i] );
        }
        Draw.g_draw.DrawSolidCapsule( vertices[0], vertices[1], m_radiusB, b2HexColor.b2_colorRed );
        // Draw.g_draw.DrawPolygon( vertices, m_countB, b2HexColor.b2_colorRed );

        if ( output.state == b2_toiStateHit )
        {
            b2DistanceInput distanceInput;
            distanceInput.proxyA = input.proxyA;
            distanceInput.proxyB = input.proxyB;
            distanceInput.transformA = b2GetSweepTransform( &sweepA, output.fraction );
            distanceInput.transformB = b2GetSweepTransform( &sweepB, output.fraction );
            distanceInput.useRadii = false;
            b2SimplexCache cache = { 0 };
            b2DistanceOutput distanceOutput = b2ShapeDistance( &cache, &distanceInput, nullptr, 0 );
            Draw.g_draw.DrawString( 5, m_textLine, "distance = %g", distanceOutput.distance );
            m_textLine += m_textIncrement;
        }

#if 0
        for (float t = 0.0f; t < 1.0f; t += 0.1f)
        {
            transformB = b2GetSweepTransform(&sweepB, t);
            for (int i = 0; i < m_countB; ++i)
            {
                vertices[i] = b2TransformPoint(transformB, m_verticesB[i]);
            }
            Draw.g_draw.DrawPolygon(vertices, m_countB, {0.3f, 0.3f, 0.3f});
        }
#endif
    }

    b2Vec2 m_verticesA[4] = { { -16.25, 44.75 }, { -15.75, 44.75 }, { -15.75, 45.25 }, { -16.25, 45.25 } };
    b2Vec2 m_verticesB[2] = { { 0.0f, -0.125000000f }, { 0.0f, 0.125000000f } };

    int m_countA = ARRAY_COUNT( m_verticesA );
    int m_countB = ARRAY_COUNT( m_verticesB );

    float m_radiusA = 0.0f;
    float m_radiusB = 0.0299999993f;
    };

    static int sampleTimeOfImpact = RegisterSample( "Collision", "Time of Impact", TimeOfImpact::Create );
