using Box2D.NET.Primitives;
using static Box2D.NET.math_function;
using static Box2D.NET.distance;
using static Box2D.NET.constants;

namespace Box2D.NET.Samples.Samples.Collisions;

public class TimeOfImpact : Sample
{
    b2Vec2[] m_verticesA = new b2Vec2[4] { new b2Vec2(-16.25f, 44.75f), new b2Vec2(-15.75f, 44.75f), new b2Vec2(-15.75f, 45.25f), new b2Vec2(-16.25f, 45.25f) };
    b2Vec2[] m_verticesB = new b2Vec2[2] { new b2Vec2(0.0f, -0.125000000f), new b2Vec2(0.0f, 0.125000000f) };

    int m_countA;
    int m_countB;

    float m_radiusA = 0.0f;
    float m_radiusB = 0.0299999993f;
    static int sampleTimeOfImpact = RegisterSample("Collision", "Time of Impact", Create);

    static Sample Create(Settings settings)
    {
        return new TimeOfImpact(settings);
    }

    public TimeOfImpact(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.6f, 2.0f);
            Draw.g_camera.m_center = new b2Vec2(-16, 45);
            Draw.g_camera.m_zoom = 5.0f;
        }

        m_countA = m_verticesA.Length;
        m_countB = m_verticesB.Length;
    }


    public override void Step(Settings settings)
    {
        base.Step(settings);

        b2Sweep sweepA = new b2Sweep(b2Vec2_zero, new b2Vec2(0.0f, 0.0f), new b2Vec2(0.0f, 0.0f), b2Rot_identity, b2Rot_identity);
        b2Sweep sweepB = new b2Sweep(
            b2Vec2_zero,
            new b2Vec2(-15.8332710f, 45.3520279f),
            new b2Vec2(-15.8324337f, 45.3413048f),
            new b2Rot(-0.540891349f, 0.841092527f),
            new b2Rot(-0.457797021f, 0.889056742f)
        );

        b2TOIInput input = new b2TOIInput();
        input.proxyA = b2MakeProxy(m_verticesA, m_countA, m_radiusA);
        input.proxyB = b2MakeProxy(m_verticesB, m_countB, m_radiusB);
        input.sweepA = sweepA;
        input.sweepB = sweepB;
        input.maxFraction = 1.0f;

        b2TOIOutput output = b2TimeOfImpact(input);

        Draw.g_draw.DrawString(5, m_textLine, "toi = %g", output.fraction);
        m_textLine += m_textIncrement;

        // Draw.g_draw.DrawString(5, m_textLine, "max toi iters = %d, max root iters = %d", b2_toiMaxIters,
        //                        b2_toiMaxRootIters);
        m_textLine += m_textIncrement;

        b2Vec2[] vertices = new b2Vec2[B2_MAX_POLYGON_VERTICES];

        // Draw A
        b2Transform transformA = b2GetSweepTransform(sweepA, 0.0f);
        for (int i = 0; i < m_countA; ++i)
        {
            vertices[i] = b2TransformPoint(ref transformA, m_verticesA[i]);
        }

        Draw.g_draw.DrawPolygon(vertices, m_countA, b2HexColor.b2_colorGray);

        // Draw B at t = 0
        b2Transform transformB = b2GetSweepTransform(sweepB, 0.0f);
        for (int i = 0; i < m_countB; ++i)
        {
            vertices[i] = b2TransformPoint(ref transformB, m_verticesB[i]);
        }

        Draw.g_draw.DrawSolidCapsule(vertices[0], vertices[1], m_radiusB, b2HexColor.b2_colorGreen);
        // Draw.g_draw.DrawPolygon( vertices, m_countB, b2HexColor.b2_colorGreen );

        // Draw B at t = hit_time
        transformB = b2GetSweepTransform(sweepB, output.fraction);
        for (int i = 0; i < m_countB; ++i)
        {
            vertices[i] = b2TransformPoint(ref transformB, m_verticesB[i]);
        }

        Draw.g_draw.DrawPolygon(vertices, m_countB, b2HexColor.b2_colorOrange);

        // Draw B at t = 1
        transformB = b2GetSweepTransform(sweepB, 1.0f);
        for (int i = 0; i < m_countB; ++i)
        {
            vertices[i] = b2TransformPoint(ref transformB, m_verticesB[i]);
        }

        Draw.g_draw.DrawSolidCapsule(vertices[0], vertices[1], m_radiusB, b2HexColor.b2_colorRed);
        // Draw.g_draw.DrawPolygon( vertices, m_countB, b2HexColor.b2_colorRed );

        if (output.state == b2TOIState.b2_toiStateHit)
        {
            b2DistanceInput distanceInput = new b2DistanceInput();
            distanceInput.proxyA = input.proxyA;
            distanceInput.proxyB = input.proxyB;
            distanceInput.transformA = b2GetSweepTransform(sweepA, output.fraction);
            distanceInput.transformB = b2GetSweepTransform(sweepB, output.fraction);
            distanceInput.useRadii = false;
            b2SimplexCache cache = new b2SimplexCache();
            b2DistanceOutput distanceOutput = b2ShapeDistance(ref cache, ref distanceInput, null, 0);
            Draw.g_draw.DrawString(5, m_textLine, "distance = %g", distanceOutput.distance);
            m_textLine += m_textIncrement;
        }

#if FALSE
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
}