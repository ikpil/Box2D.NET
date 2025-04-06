// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Constants;

namespace Box2D.NET.Samples.Samples.Collisions;

public class TimeOfImpact : Sample
{
    private static readonly int SampleTimeOfImpact = SampleFactory.Shared.RegisterSample("Collision", "Time of Impact", Create);

    private B2Vec2[] m_verticesA = new B2Vec2[4] { new B2Vec2(-16.25f, 44.75f), new B2Vec2(-15.75f, 44.75f), new B2Vec2(-15.75f, 45.25f), new B2Vec2(-16.25f, 45.25f) };
    private B2Vec2[] m_verticesB = new B2Vec2[2] { new B2Vec2(0.0f, -0.125000000f), new B2Vec2(0.0f, 0.125000000f) };

    private int m_countA;
    private int m_countB;

    private float m_radiusA = 0.0f;
    private float m_radiusB = 0.0299999993f;

    private B2Sweep _sweepA;
    private B2Sweep _sweepB;
    private B2TOIInput _input;
    private B2TOIOutput _output;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new TimeOfImpact(ctx, settings);
    }

    public TimeOfImpact(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.6f, 2.0f);
            m_context.camera.m_center = new B2Vec2(-16, 45);
            m_context.camera.m_zoom = 5.0f;
        }

        m_countA = m_verticesA.Length;
        m_countB = m_verticesB.Length;
    }


    public override void Step(Settings settings)
    {
        base.Step(settings);

        _sweepA = new B2Sweep(b2Vec2_zero, new B2Vec2(0.0f, 0.0f), new B2Vec2(0.0f, 0.0f), b2Rot_identity, b2Rot_identity);
        _sweepB = new B2Sweep(
            b2Vec2_zero,
            new B2Vec2(-15.8332710f, 45.3520279f),
            new B2Vec2(-15.8324337f, 45.3413048f),
            new B2Rot(-0.540891349f, 0.841092527f),
            new B2Rot(-0.457797021f, 0.889056742f)
        );

        _input = new B2TOIInput();
        _input.proxyA = b2MakeProxy(m_verticesA, m_countA, m_radiusA);
        _input.proxyB = b2MakeProxy(m_verticesB, m_countB, m_radiusB);
        _input.sweepA = _sweepA;
        _input.sweepB = _sweepB;
        _input.maxFraction = 1.0f;

        _output = b2TimeOfImpact(ref _input);
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        m_context.draw.DrawString(5, m_textLine, $"toi = {_output.fraction:g}");
        m_textLine += m_textIncrement;

        // m_context.g_draw.DrawString(5, m_textLine, "max toi iters = %d, max root iters = %d", b2_toiMaxIters,
        //                        b2_toiMaxRootIters);
        m_textLine += m_textIncrement;

        B2Vec2[] vertices = new B2Vec2[B2_MAX_POLYGON_VERTICES];

        // Draw A
        B2Transform transformA = b2GetSweepTransform(ref _sweepA, 0.0f);
        for (int i = 0; i < m_countA; ++i)
        {
            vertices[i] = b2TransformPoint(ref transformA, m_verticesA[i]);
        }

        m_context.draw.DrawPolygon(vertices, m_countA, B2HexColor.b2_colorGray);

        // Draw B at t = 0
        B2Transform transformB = b2GetSweepTransform(ref _sweepB, 0.0f);
        for (int i = 0; i < m_countB; ++i)
        {
            vertices[i] = b2TransformPoint(ref transformB, m_verticesB[i]);
        }

        m_context.draw.DrawSolidCapsule(vertices[0], vertices[1], m_radiusB, B2HexColor.b2_colorGreen);
        // m_context.g_draw.DrawPolygon( vertices, m_countB, b2HexColor.b2_colorGreen );

        // Draw B at t = hit_time
        transformB = b2GetSweepTransform(ref _sweepB, _output.fraction);
        for (int i = 0; i < m_countB; ++i)
        {
            vertices[i] = b2TransformPoint(ref transformB, m_verticesB[i]);
        }

        m_context.draw.DrawPolygon(vertices, m_countB, B2HexColor.b2_colorOrange);

        // Draw B at t = 1
        transformB = b2GetSweepTransform(ref _sweepB, 1.0f);
        for (int i = 0; i < m_countB; ++i)
        {
            vertices[i] = b2TransformPoint(ref transformB, m_verticesB[i]);
        }

        m_context.draw.DrawSolidCapsule(vertices[0], vertices[1], m_radiusB, B2HexColor.b2_colorRed);
        // m_context.g_draw.DrawPolygon( vertices, m_countB, b2HexColor.b2_colorRed );

        if (_output.state == B2TOIState.b2_toiStateHit)
        {
            B2DistanceInput distanceInput = new B2DistanceInput();
            distanceInput.proxyA = _input.proxyA;
            distanceInput.proxyB = _input.proxyB;
            distanceInput.transformA = b2GetSweepTransform(ref _sweepA, _output.fraction);
            distanceInput.transformB = b2GetSweepTransform(ref _sweepB, _output.fraction);
            distanceInput.useRadii = false;
            B2SimplexCache cache = new B2SimplexCache();
            B2DistanceOutput distanceOutput = b2ShapeDistance(ref distanceInput, ref cache, null, 0);
            m_context.draw.DrawString(5, m_textLine, $"distance = {distanceOutput.distance}:g");
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
            m_context.g_draw.DrawPolygon(vertices, m_countB, {0.3f, 0.3f, 0.3f});
        }
#endif
    }
}