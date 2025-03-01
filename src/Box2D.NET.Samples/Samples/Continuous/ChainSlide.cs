// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Distances;

namespace Box2D.NET.Samples.Samples.Continuous;

public class ChainSlide : Sample
{
    private static readonly int SampleChainSlide = SampleFactory.Shared.RegisterSample("Continuous", "Chain Slide", Create);

    private static Sample Create(Settings settings)
    {
        return new ChainSlide(settings);
    }

    public ChainSlide(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.0f, 10.0f);
            B2.g_camera.m_zoom = 15.0f;
        }

#if DEBUG
        b2_toiHitCount = 0;
#endif

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

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

            b2CreateChain(groundId, chainDef);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.linearVelocity = new B2Vec2(100.0f, 0.0f);
            bodyDef.position = new B2Vec2(-19.5f, 0.0f + 0.5f);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.0f;
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            b2CreateCircleShape(bodyId, shapeDef, circle);
        }
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

#if DEBUG
        B2.g_draw.DrawString(5, m_textLine, "toi hits = %d", b2_toiHitCount);
        m_textLine += m_textIncrement;
#endif
    }
}
