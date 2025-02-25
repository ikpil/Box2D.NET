// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.Shared.benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkLargePyramid : Sample
{
    static int benchmarkLargePyramid = RegisterSample("Benchmark", "Large Pyramid", Create);

    static Sample Create(Settings settings)
    {
        return new BenchmarkLargePyramid(settings);
    }

    public BenchmarkLargePyramid(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 50.0f);
            Draw.g_camera.m_zoom = 25.0f * 2.2f;
            settings.enableSleep = false;
        }

        CreateLargePyramid(m_worldId);
    }
}
