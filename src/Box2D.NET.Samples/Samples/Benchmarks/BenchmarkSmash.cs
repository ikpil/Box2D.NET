// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkSmash : Sample
{
    private static readonly int SampleBenchmarkSmash = SampleFactory.Shared.RegisterSample("Benchmark", "Smash", Create);

    private static Sample Create(Settings settings)
    {
        return new BenchmarkSmash(settings);
    }

    public BenchmarkSmash(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(60.0f, 6.0f);
            B2.g_camera.m_zoom = 25.0f * 1.6f;
        }

        CreateSmash(m_worldId);
    }
}
