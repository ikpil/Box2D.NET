// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkJointGrid : Sample
{
    private static readonly int BenchmarkJointGridIndex = SampleFactory.Shared.RegisterSample("Benchmark", "Joint Grid", Create);

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkJointGrid(context);
    }

    public BenchmarkJointGrid(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(60.0f, -57.0f);
            m_context.camera.m_zoom = 25.0f * 2.5f;
            m_context.settings.enableSleep = false;
        }

        CreateJointGrid(m_worldId);
    }
}