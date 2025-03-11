// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Joints;

public class SoftBody : Sample
{
    Donut m_donut;
    private static readonly int SampleDonut = SampleFactory.Shared.RegisterSample("Joints", "Soft Body", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new SoftBody(ctx, settings);
    }

    public SoftBody(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.g_camera.m_center = new B2Vec2(0.0f, 5.0f);
            m_context.g_camera.m_zoom = 25.0f * 0.25f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        m_donut = new();
        m_donut.Spawn(m_worldId, new B2Vec2(0.0f, 10.0f), 2.0f, 0, null);
    }
}