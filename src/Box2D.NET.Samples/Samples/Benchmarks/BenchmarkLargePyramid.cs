// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkLargePyramid : Sample
{
    private static readonly int SampleBenchmarkLargePyramid = SampleFactory.Shared.RegisterSample("Benchmark", "Large Pyramid", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BenchmarkLargePyramid(ctx, settings);
    }

    public BenchmarkLargePyramid(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.g_camera.m_center = new B2Vec2(0.0f, 50.0f);
            m_context.g_camera.m_zoom = 25.0f * 2.2f;
            settings.enableSleep = false;
        }

        CreateLargePyramid(m_worldId);
    }
}
