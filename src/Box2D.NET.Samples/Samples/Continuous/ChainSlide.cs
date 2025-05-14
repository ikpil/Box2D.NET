// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Continuous;

public class ChainSlide : Sample
{
    private static readonly int SampleChainSlide = SampleFactory.Shared.RegisterSample("Continuous", "Chain Slide", Create);

    private static Sample Create(SampleContext context)
    {
        return new ChainSlide(context);
    }

    public ChainSlide(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 10.0f);
            m_context.camera.m_zoom = 15.0f;
        }

        // b2_toiHitCount = 0;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            const int count = 80;
            B2Vec2[] points = new B2Vec2[count];

            float w = 2.0f;
            float h = 1.0f;
            float x = 20.0f, y = 0.0f;
            for (int i = 0; i < 20; ++i)
            {
                points[i] = new B2Vec2(x, y);
                x -= w;
            }

            for (int i = 20; i < 40; ++i)
            {
                points[i] = new B2Vec2(x, y);
                y += h;
            }

            for (int i = 40; i < 60; ++i)
            {
                points[i] = new B2Vec2(x, y);
                x += w;
            }

            for (int i = 60; i < 80; ++i)
            {
                points[i] = new B2Vec2(x, y);
                y -= h;
            }

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = count;
            chainDef.isLoop = true;

            b2CreateChain(groundId, ref chainDef);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.linearVelocity = new B2Vec2(100.0f, 0.0f);
            bodyDef.position = new B2Vec2(-19.5f, 0.0f + 0.5f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.0f;
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        // DrawTextLine($"toi hits = {b2_toiHitCount}");
    }
}