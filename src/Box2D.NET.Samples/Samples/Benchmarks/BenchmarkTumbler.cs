// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkTumbler : Sample
{
    private static readonly int SampleBenchmarkTumbler = SampleFactory.Shared.RegisterSample("Benchmark", "Tumbler", Create);

    private static Sample Create(Settings settings)
    {
        return new BenchmarkTumbler(settings);
    }

    public BenchmarkTumbler(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(1.5f, 10.0f);
            B2.g_camera.m_zoom = 25.0f * 0.6f;
        }

        CreateTumbler(m_worldId);
    }
}
