// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkLargePyramid : Sample
{
    private static readonly int SampleBenchmarkLargePyramid = SampleFactory.Shared.RegisterSample("Benchmark", "Large Pyramid", Create);

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkLargePyramid(context);
    }

    public BenchmarkLargePyramid(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 50.0f);
            m_camera.zoom = 25.0f * 2.2f;
            m_context.enableSleep = false;
        }

        CreateLargePyramid(m_worldId);
    }
}
