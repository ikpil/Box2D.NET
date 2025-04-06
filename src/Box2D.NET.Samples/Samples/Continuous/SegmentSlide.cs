// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Distances;

namespace Box2D.NET.Samples.Samples.Continuous;

public class SegmentSlide : Sample
{
    private static readonly int SampleSegmentSlide = SampleFactory.Shared.RegisterSample("Continuous", "Segment Slide", Create);

    public static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new SegmentSlide(ctx, settings);
    }

    public SegmentSlide(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 10.0f);
            m_context.camera.m_zoom = 15.0f;
        }

#if DEBUG
        b2_toiHitCount = 0;
#endif

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

            segment = new B2Segment(new B2Vec2(40.0f, 0.0f), new B2Vec2(40.0f, 10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.linearVelocity = new B2Vec2(100.0f, 0.0f);
            bodyDef.position = new B2Vec2(-20.0f, 0.7f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            //shapeDef.friction = 0.0f;
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

#if DEBUG
        m_context.draw.DrawString(5, m_textLine, $"toi hits = {b2_toiHitCount}");
        m_textLine += m_textIncrement;
#endif
    }
}
