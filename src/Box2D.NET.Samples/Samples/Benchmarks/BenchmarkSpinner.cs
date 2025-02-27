// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.B2Distances;
using static Box2D.NET.Shared.Benchmarks2;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkSpinner : Sample
{
    static int sampleSpinner = RegisterSample("Benchmark", "Spinner", Create);

    static Sample Create(Settings settings)
    {
        return new BenchmarkSpinner(settings);
    }

    public BenchmarkSpinner(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new B2Vec2(0.0f, 32.0f);
            Draw.g_camera.m_zoom = 42.0f;
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

#if DEBUG
        DrawTextLine("toi calls, hits = %d, %d", b2_toiCalls, b2_toiHitCount);
#endif
    }
}
