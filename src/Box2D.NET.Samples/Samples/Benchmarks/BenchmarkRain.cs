// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Shared;
using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkRain : Sample
{
    private static readonly int SampleBenchmarkRain = SampleFactory.Shared.RegisterSample("Benchmark", "Rain", Create);

    private RainData m_rainData;

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkRain(context);
    }

    public BenchmarkRain(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 110.0f);
            m_camera.zoom = 125.0f;
            m_context.enableSleep = true;
        }

        m_context.debugDraw.drawJoints = false;

        m_rainData = CreateRain(m_worldId);
    }

    public override void Step()
    {
        if (m_context.pause == false || m_context.singleStep == true)
        {
            StepRain(m_rainData, m_worldId, m_stepCount);
        }

        base.Step();

        if (m_stepCount % 1000 == 0)
        {
            m_stepCount += 0;
        }
    }
}
