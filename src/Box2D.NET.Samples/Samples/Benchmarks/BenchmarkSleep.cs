// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.timer;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkSleep : Sample
{
    public const int e_maxBaseCount = 100;
    public const int e_maxBodyCount = e_maxBaseCount * (e_maxBaseCount + 1) / 2;

    b2BodyId[] m_bodies = new b2BodyId[e_maxBodyCount];
    int m_bodyCount;
    int m_baseCount;
    int m_iterations;
    float m_wakeTotal;
    float m_sleepTotal;
    int m_wakeCount;
    int m_sleepCount;
    bool m_awake;

    static int benchmarkSleep = RegisterSample("Benchmark", "Sleep", Create);

    static Sample Create(Settings settings)
    {
        return new BenchmarkSleep(settings);
    }


    public BenchmarkSleep(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 50.0f);
            Draw.g_camera.m_zoom = 25.0f * 2.2f;
        }

        float groundSize = 100.0f;

        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        b2Polygon box = b2MakeBox(groundSize, 1.0f);
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(groundId, shapeDef, box);

        for (int i = 0; i < e_maxBodyCount; ++i)
        {
            m_bodies[i] = b2_nullBodyId;
        }

        m_baseCount = g_sampleDebug ? 40 : 100;
        m_iterations = g_sampleDebug ? 1 : 41;
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

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.friction = 0.5f;

        float h = 0.5f;
        b2Polygon box = b2MakeRoundedBox(h, h, 0.0f);

        int index = 0;

        for (int i = 0; i < count; ++i)
        {
            float y = i * shift + centery;

            for (int j = i; j < count; ++j)
            {
                float x = 0.5f * i * shift + (j - i) * shift - centerx;
                bodyDef.position = new b2Vec2(x, y);

                Debug.Assert(index < e_maxBodyCount);
                m_bodies[index] = b2CreateBody(m_worldId, bodyDef);
                b2CreatePolygonShape(m_bodies[index], shapeDef, box);

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

        if (m_wakeCount > 0)
        {
            Draw.g_draw.DrawString(5, m_textLine, "wake ave = %g ms", m_wakeTotal / m_wakeCount);
            m_textLine += m_textIncrement;
        }

        if (m_sleepCount > 0)
        {
            Draw.g_draw.DrawString(5, m_textLine, "sleep ave = %g ms", m_sleepTotal / m_sleepCount);
            m_textLine += m_textIncrement;
        }

        base.Step(settings);
    }
}
