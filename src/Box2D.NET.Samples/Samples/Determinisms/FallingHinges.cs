// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using Box2D.NET.Shared;
using Serilog;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Constants;
using static Box2D.NET.Shared.Determinism;

namespace Box2D.NET.Samples.Samples.Determinisms;

// This sample provides a visual representation of the cross platform determinism unit test.
// The scenario is designed to produce a chaotic result engaging:
// - continuous collision
// - joint limits (approximate atan2)
// - b2MakeRot (approximate sin/cos)
// Once all the bodies go to sleep the step counter and transform hash is emitted which
// can then be transferred to the unit test and tested in GitHub build actions.
// See CrossPlatformTest in the unit tests.
public class FallingHinges : Sample
{
    private static readonly ILogger Logger = Log.ForContext<FallingHinges>();

    private static readonly int SampleFallingHinges = SampleFactory.Shared.RegisterSample("Determinism", "Falling Hinges", Create);

    private FallingHingeData m_data;
    private bool m_done;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new FallingHinges(ctx, settings);
    }


    public FallingHinges(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 7.5f);
            m_context.camera.m_zoom = 10.0f;
        }
        m_data = CreateFallingHinges( m_worldId );
        m_done = false;
    }


    public override void Step(Settings settings)
    {
        base.Step(settings);

        if (m_done == false)
        {
            m_done = UpdateFallingHinges( m_worldId, ref m_data );
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        if (m_done)
        {
            m_context.draw.DrawString(5, m_textLine, $"sleep step = {m_data.sleepStep}, hash = 0x{m_data.hash:X8}");
            m_textLine += m_textIncrement;
        }
    }
}