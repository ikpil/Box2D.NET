// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Timers;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkSleep : Sample
{
    private static readonly int SampleBenchmarkSleep = SampleFactory.Shared.RegisterSample("Benchmark", "Sleep", Create);

    public const int e_maxBaseCount = 100;
    public const int e_maxBodyCount = e_maxBaseCount * (e_maxBaseCount + 1) / 2;

    private B2BodyId[] m_bodies = new B2BodyId[e_maxBodyCount];
    private int m_bodyCount;
    private int m_baseCount;
    private int m_iterations;
    private float m_wakeTotal;
    private float m_sleepTotal;
    private int m_wakeCount;
    private int m_sleepCount;
    private bool m_awake;


    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BenchmarkSleep(ctx, settings);
    }


    public BenchmarkSleep(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 50.0f);
            m_context.camera.m_zoom = 25.0f * 2.2f;
        }

        float groundSize = 100.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

        B2Polygon box = b2MakeBox(groundSize, 1.0f);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(groundId, ref shapeDef, ref box);

        for (int i = 0; i < e_maxBodyCount; ++i)
        {
            m_bodies[i] = b2_nullBodyId;
        }

        m_baseCount = m_context.sampleDebug ? 40 : 100;
        m_iterations = m_context.sampleDebug ? 1 : 41;
        m_bodyCount = 0;
        m_awake = false;

        m_wakeTotal = 0.0f;
        m_wakeCount = 0;

        m_sleepTotal = 0.0f;
        m_sleepCount = 0;

        CreateScene();
    }

    void CreateScene()
    {
        for (int i = 0; i < e_maxBodyCount; ++i)
        {
            if (B2_IS_NON_NULL(m_bodies[i]))
            {
                b2DestroyBody(m_bodies[i]);
                m_bodies[i] = b2_nullBodyId;
            }
        }

        int count = m_baseCount;
        float rad = 0.5f;
        float shift = rad * 2.0f;
        float centerx = shift * count / 2.0f;
        float centery = shift / 2.0f + 1.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = 0.5f;

        float h = 0.5f;
        B2Polygon box = b2MakeRoundedBox(h, h, 0.0f);

        int index = 0;

        for (int i = 0; i < count; ++i)
        {
            float y = i * shift + centery;

            for (int j = i; j < count; ++j)
            {
                float x = 0.5f * i * shift + (j - i) * shift - centerx;
                bodyDef.position = new B2Vec2(x, y);

                Debug.Assert(index < e_maxBodyCount);
                m_bodies[index] = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(m_bodies[index], ref shapeDef, ref box);

                index += 1;
            }
        }

        m_bodyCount = index;
    }

    public override void Step(Settings settings)
    {
        ulong ticks = b2GetTicks();

        for (int i = 0; i < m_iterations; ++i)
        {
            b2Body_SetAwake(m_bodies[0], m_awake);
            if (m_awake)
            {
                m_wakeTotal += b2GetMillisecondsAndReset(ref ticks);
                m_wakeCount += 1;
            }
            else
            {
                m_sleepTotal += b2GetMillisecondsAndReset(ref ticks);
                m_sleepCount += 1;
            }

            m_awake = !m_awake;
        }


        base.Step(settings);
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);
        
        if (m_wakeCount > 0)
        {
            m_context.draw.DrawString(5, m_textLine, $"wake ave = {m_wakeTotal / m_wakeCount:g} ms");
            m_textLine += m_textIncrement;
        }

        if (m_sleepCount > 0)
        {
            m_context.draw.DrawString(5, m_textLine, $"sleep ave = {m_sleepTotal / m_sleepCount:g} ms");
            m_textLine += m_textIncrement;
        }
    }
}