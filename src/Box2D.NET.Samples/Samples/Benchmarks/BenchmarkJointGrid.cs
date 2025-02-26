// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.Shared.benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkJointGrid : Sample
{
    static int benchmarkJointGridIndex = RegisterSample("Benchmark", "Joint Grid", Create);

    static Sample Create(Settings settings)
    {
        return new BenchmarkJointGrid(settings);
    }

    public BenchmarkJointGrid(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new B2Vec2(60.0f, -57.0f);
            Draw.g_camera.m_zoom = 25.0f * 2.5f;
            settings.enableSleep = false;
        }

        CreateJointGrid(m_worldId);
    }
}