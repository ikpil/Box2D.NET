// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.Shared.benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkTumbler : Sample
{
    static int benchmarkTumbler = RegisterSample("Benchmark", "Tumbler", Create);

    static Sample Create(Settings settings)
    {
        return new BenchmarkTumbler(settings);
    }

    public BenchmarkTumbler(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(1.5f, 10.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.6f;
        }

        CreateTumbler(m_worldId);
    }
}
