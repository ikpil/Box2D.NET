// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using Box2D.NET.Shared.Primitives;
using static Box2D.NET.Shared.benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkRain : Sample
{
    static int benchmarkRain = RegisterSample("Benchmark", "Rain", Create);

    private RainData m_rainData;

    static Sample Create(Settings settings)
    {
        return new BenchmarkRain(settings);
    }

    public BenchmarkRain(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 110.0f);
            Draw.g_camera.m_zoom = 125.0f;
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
