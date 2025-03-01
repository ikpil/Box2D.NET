// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkManyPyramids : Sample
{
    private static readonly int SampleBenchmarkManyPyramids = SampleFactory.Shared.RegisterSample("Benchmark", "Many Pyramids", Create);

    private static Sample Create(Settings settings)
    {
        return new BenchmarkManyPyramids(settings);
    }

    public BenchmarkManyPyramids(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(16.0f, 110.0f);
            B2.g_camera.m_zoom = 25.0f * 5.0f;
            settings.enableSleep = false;
        }

        CreateManyPyramids(m_worldId);
    }
}
