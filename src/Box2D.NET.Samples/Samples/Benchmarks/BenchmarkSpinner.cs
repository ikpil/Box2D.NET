// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Distances;
using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkSpinner : Sample
{
    private static readonly int SampleBenchmarkSpinner = SampleFactory.Shared.RegisterSample("Benchmark", "Spinner", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BenchmarkSpinner(ctx, settings);
    }

    public BenchmarkSpinner(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 32.0f);
            m_context.camera.m_zoom = 42.0f;
        }

#if DEBUG
        b2_toiCalls = 0;
        b2_toiHitCount = 0;
#endif

        CreateSpinner(m_worldId);
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        if (m_stepCount == 1000 && false)
        {
            // 0.1 : 46544, 25752
            // 0.25 : 5745, 1947
            // 0.5 : 2197, 660
            settings.pause = true;
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);
        
#if DEBUG
        DrawTextLine("toi calls, hits = %d, %d", b2_toiCalls, b2_toiHitCount);
#endif
    }
}