// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkSpinner : Sample
{
    private static readonly int SampleBenchmarkSpinner = SampleFactory.Shared.RegisterSample("Benchmark", "Spinner", Create);

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkSpinner(context);
    }

    public BenchmarkSpinner(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 32.0f);
            m_camera.m_zoom = 42.0f;
        }

        // b2_toiCalls = 0;
        // b2_toiHitCount = 0;

        CreateSpinner(m_worldId);
    }

    public override void Step()
    {
        base.Step();

        if (m_stepCount == 1000 && false)
        {
            // 0.1 : 46544, 25752
            // 0.25 : 5745, 1947
            // 0.5 : 2197, 660
            m_context.settings.pause = true;
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);
        
        //DrawTextLine($"toi calls, hits = {b2_toiCalls}, {b2_toiHitCount}");
    }
}