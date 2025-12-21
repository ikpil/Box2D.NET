// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Benchmarks;

// This benchmark pushes Box2D to the limit for a large pile. It terminates once simulation is deemed to be slow.
// The higher the body count achieved, the better.
// Note: this benchmark stresses the sleep system more than any other benchmark. Better results are achieved if sleeping
// is disabled.
public class BenchmarkCapacity : Sample
{
    private static readonly int benchmarkCapacity = SampleFactory.Shared.RegisterSample("Benchmark", "Capacity", Create);

    private B2Polygon m_square;
    private int m_reachCount;
    private bool m_done;

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkCapacity(context);
    }

    public BenchmarkCapacity(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 150.0f);
            m_context.camera.zoom = 200.0f;
        }

        m_context.enableSleep = false;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position.Y = -5.0f;
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeBox(800.0f, 5.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        m_square = b2MakeSquare(0.5f);
        m_done = false;
        m_reachCount = 0;
    }

    public override void Step()
    {
        base.Step();

        float millisecondLimit = 20.0f;

        B2Profile profile = b2World_GetProfile(m_worldId);
        if (profile.step > millisecondLimit)
        {
            m_reachCount += 1;
            if (m_reachCount > 60)
            {
                // Hit the millisecond limit 60 times in a row
                m_done = true;
            }
        }
        else
        {
            m_reachCount = 0;
        }

        if (m_done == true)
        {
            return;
        }

        if ((m_stepCount & 0x1F) != 0x1F)
        {
            return;
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position.Y = 200.0f;

        B2ShapeDef shapeDef = b2DefaultShapeDef();

        int count = 200;
        float x = -1.0f * count;
        for (int i = 0; i < count; ++i)
        {
            bodyDef.position.X = x;
            bodyDef.position.Y += 0.5f;

            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(bodyId, shapeDef, m_square);

            x += 2.0f;
        }
    }
}
