// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkSmash : Sample
{
    private static readonly int SampleBenchmarkSmash = SampleFactory.Shared.RegisterSample("Benchmark", "Smash", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BenchmarkSmash(ctx, settings);
    }

    public BenchmarkSmash(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(60.0f, 6.0f);
            m_context.camera.m_zoom = 25.0f * 1.6f;
        }

        CreateSmash(m_worldId);
    }
}
