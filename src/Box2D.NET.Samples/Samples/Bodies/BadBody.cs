// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Bodies;

public class BadBody : Sample
{
    B2BodyId m_badBodyId;

    private static readonly int SampleBadBody = SampleFactory.Shared.RegisterSample("Bodies", "Bad", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BadBody(ctx, settings);
    }

    public BadBody(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(2.3f, 10.0f);
            m_context.camera.m_zoom = 25.0f * 0.5f;
        }

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        // Build a bad body
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 3.0f);
            bodyDef.angularVelocity = 0.5f;
            bodyDef.rotation = b2MakeRot(0.25f * B2_PI);

            m_badBodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -1.0f), new B2Vec2(0.0f, 1.0f), 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            // density set to zero intentionally to create a bad body
            shapeDef.density = 0.0f;
            b2CreateCapsuleShape(m_badBodyId, ref shapeDef, ref capsule);
        }

        // Build a normal body
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(2.0f, 3.0f);
            bodyDef.rotation = b2MakeRot(0.25f * B2_PI);

            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -1.0f), new B2Vec2(0.0f, 1.0f), 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);
        }
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);


        // For science
        b2Body_ApplyForceToCenter(m_badBodyId, new B2Vec2(0.0f, 10.0f), true);
    }

    public override void Draw(Settings setting)
    {
        base.Draw(setting);
        
        m_context.draw.DrawString(5, m_textLine, "A bad body is a dynamic body with no mass and behaves like a kinematic body.");
        m_textLine += m_textIncrement;

        m_context.draw.DrawString(5, m_textLine, "Bad bodies are considered invalid and a user bug. Behavior is not guaranteed.");
        m_textLine += m_textIncrement;
    }
}
