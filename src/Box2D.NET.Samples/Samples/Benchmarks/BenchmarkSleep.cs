// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Joints;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkSleep : Sample
{
    private static readonly int SampleBenchmarkSleep = SampleFactory.Shared.RegisterSample("Benchmark", "Sleep", Create);

    public const int e_maxBaseCount = 100;
    public const int e_maxBodyCount = e_maxBaseCount * (e_maxBaseCount + 1) / 2;

    private B2BodyId[] m_bodies = new B2BodyId[e_maxBodyCount];
    private int m_bodyCount;
    private int m_baseCount;
    private float m_wakeTotal;
    private float m_sleepTotal;
    private bool m_awake;


    private static Sample Create(SampleContext context)
    {
        return new BenchmarkSleep(context);
    }


    public BenchmarkSleep(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 50.0f);
            m_camera.zoom = 25.0f * 2.2f;
        }

        {
            float groundSize = 100.0f;

            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeBox(groundSize, 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        m_baseCount = m_isDebug ? 40 : 100;
        m_bodyCount = 0;

        int count = m_baseCount;
        float rad = 0.5f;
        float shift = rad * 2.0f;
        float centerx = shift * count / 2.0f;
        float centery = shift / 2.0f + 1.0f;

        {
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

                    B2_ASSERT(index < e_maxBodyCount);
                    m_bodies[index] = b2CreateBody(m_worldId, bodyDef);
                    b2CreatePolygonShape(m_bodies[index], ref shapeDef, ref box);

                    index += 1;
                }
            }

            m_bodyCount = index;
            m_wakeTotal = 0.0f;
            m_sleepTotal = 0.0f;
        }
    }

    public override void Step()
    {
        // These operations don't show up in b2Profile
        if (m_stepCount > 20)
        {
            // Creating and destroying a joint will engage the island splitter.
            b2FilterJointDef jointDef = b2DefaultFilterJointDef();
            jointDef.@base.bodyIdA = m_bodies[0];
            jointDef.@base.bodyIdB = m_bodies[1];
            B2JointId jointId = b2CreateFilterJoint(m_worldId, ref jointDef);

            ulong ticks = b2GetTicks();

            // This will wake the island
            b2DestroyJoint(jointId, true);
            m_wakeTotal += b2GetMillisecondsAndReset(ref ticks);

            // Put the island back to sleep. It must be split because a constraint was removed.
            b2Body_SetAwake(m_bodies[0], false);
            m_sleepTotal += b2GetMillisecondsAndReset(ref ticks);
        }

        base.Step();
    }

    public override void Draw()
    {
        base.Draw();

        int count = m_stepCount - 20;
        DrawTextLine($"wake ave = {m_wakeTotal / count:g} ms");
        DrawTextLine($"sleep ave = {m_sleepTotal / count:g} ms");
    }
}