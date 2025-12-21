// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Continuous;

public class SegmentSlide : Sample
{
    private static readonly int SampleSegmentSlide = SampleFactory.Shared.RegisterSample("Continuous", "Segment Slide", Create);

    public static Sample Create(SampleContext context)
    {
        return new SegmentSlide(context);
    }

    public SegmentSlide(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 10.0f);
            m_camera.zoom = 15.0f;
        }

        // b2_toiHitCount = 0;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);

            segment = new B2Segment(new B2Vec2(40.0f, 0.0f), new B2Vec2(40.0f, 10.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.linearVelocity = new B2Vec2(100.0f, 0.0f);
            bodyDef.position = new B2Vec2(-20.0f, 0.7f);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            // shapeDef.friction = 0.0f;
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            b2CreateCircleShape(bodyId, shapeDef, circle);
        }
    }

    public override void Draw()
    {
        base.Draw();

        // DrawTextLine($"toi hits = {b2_toiHitCount}");
    }
}
