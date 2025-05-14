// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkTumbler : Sample
{
    private static readonly int SampleBenchmarkTumbler = SampleFactory.Shared.RegisterSample("Benchmark", "Tumbler", Create);

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkTumbler(context);
    }

    public BenchmarkTumbler(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(1.5f, 10.0f);
            m_context.camera.m_zoom = 25.0f * 0.6f;
        }

        CreateTumbler(m_worldId);
    }
}