﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;
using static Box2D.NET.timer;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkCreateDestroy : Sample
{
    public const int e_maxBaseCount = 100;
    public const int e_maxBodyCount = e_maxBaseCount * (e_maxBaseCount + 1) / 2;

    float m_createTime;
    float m_destroyTime;
    b2BodyId[] m_bodies = new b2BodyId[e_maxBodyCount];
    int m_bodyCount;
    int m_baseCount;
    int m_iterations;

    static int benchmarkCreateDestroy = RegisterSample("Benchmark", "CreateDestroy", Create);

    static Sample Create(Settings settings)
    {
        return new BenchmarkCreateDestroy(settings);
    }

    public BenchmarkCreateDestroy(Settings settings) : base(settings)
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

        m_createTime = 0.0f;
        m_destroyTime = 0.0f;

        m_baseCount = g_sampleDebug ? 40 : 100;
        m_iterations = g_sampleDebug ? 1 : 10;
        m_bodyCount = 0;
    }

    void CreateScene()
    {
        ulong ticks = b2GetTicks();

        for (int i = 0; i < e_maxBodyCount; ++i)
        {
            if (B2_IS_NON_NULL(m_bodies[i]))
            {
                b2DestroyBody(m_bodies[i]);
                m_bodies[i] = b2_nullBodyId;
            }
        }

        m_destroyTime += b2GetMillisecondsAndReset(ref ticks);

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

        m_createTime += b2GetMilliseconds(ticks);

        m_bodyCount = index;

        b2World_Step(m_worldId, 1.0f / 60.0f, 4);
    }

    public override void Step(Settings settings)
    {
        m_createTime = 0.0f;
        m_destroyTime = 0.0f;

        for (int i = 0; i < m_iterations; ++i)
        {
            CreateScene();
        }

        DrawTextLine("total: create = %g ms, destroy = %g ms", m_createTime, m_destroyTime);

        float createPerBody = 1000.0f * m_createTime / m_iterations / m_bodyCount;
        float destroyPerBody = 1000.0f * m_destroyTime / m_iterations / m_bodyCount;
        DrawTextLine("body: create = %g us, destroy = %g us", createPerBody, destroyPerBody);

        base.Step(settings);
    }
}
