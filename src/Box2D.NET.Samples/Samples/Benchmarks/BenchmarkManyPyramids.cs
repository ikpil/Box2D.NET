﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkManyPyramids : Sample
{
    private static readonly int SampleBenchmarkManyPyramids = SampleFactory.Shared.RegisterSample("Benchmark", "Many Pyramids", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BenchmarkManyPyramids(ctx, settings);
    }

    public BenchmarkManyPyramids(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(16.0f, 110.0f);
            m_context.camera.m_zoom = 25.0f * 5.0f;
            settings.enableSleep = false;
        }

        CreateManyPyramids(m_worldId);
    }
}
