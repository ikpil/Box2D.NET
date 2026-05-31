// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Shared;
using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkJunkyard : Sample
{
    private static readonly int BenchmarkJunkyardIndex = SampleFactory.Shared.RegisterSample("Benchmark", "Junkyard", Create);

    private readonly JunkyardData m_junkyardData;

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkJunkyard(context);
    }

    public BenchmarkJunkyard(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(8.0f, 25.0f);
            m_context.camera.zoom = 60.0f;
        }

        m_junkyardData = CreateJunkyard(m_worldId);
    }

    public override void Step()
    {
        if (m_context.pause == false || m_context.singleStep == true)
        {
            StepJunkyard(m_junkyardData, m_worldId, m_stepCount);
        }

        base.Step();
    }
}
