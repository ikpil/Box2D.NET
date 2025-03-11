// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Shared.Primitives;
using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkRain : Sample
{
    private static readonly int SampleBenchmarkRain = SampleFactory.Shared.RegisterSample("Benchmark", "Rain", Create);

    private RainData m_rainData;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BenchmarkRain(ctx, settings);
    }

    public BenchmarkRain(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 110.0f);
            m_context.camera.m_zoom = 125.0f;
            settings.enableSleep = true;
        }

        settings.drawJoints = false;

        m_rainData = CreateRain(m_worldId);
    }

    public override void Step(Settings settings)
    {
        if (settings.pause == false || settings.singleStep == true)
        {
            StepRain(m_rainData, m_worldId, m_stepCount);
        }

        base.Step(settings);

        if (m_stepCount % 1000 == 0)
        {
            m_stepCount += 0;
        }
    }
}
