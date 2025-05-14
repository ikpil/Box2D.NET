// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Stackings;

public class Confined : Sample
{
    private static readonly int SampleConfined = SampleFactory.Shared.RegisterSample("Stacking", "Confined", Create);
    
    public const int m_gridCount = 25;
    public const int m_maxCount = m_gridCount * m_gridCount;

    int m_row;
    int m_column;
    int m_count;


    private static Sample Create(SampleContext context)
    {
        return new Confined(context);
    }


    public Confined(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 10.0f);
            m_context.camera.m_zoom = 25.0f * 0.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Capsule capsule;
            capsule = new B2Capsule(new B2Vec2(-10.5f, 0.0f), new B2Vec2(10.5f, 0.0f), 0.5f);
            b2CreateCapsuleShape(groundId, ref shapeDef, ref capsule);
            capsule = new B2Capsule(new B2Vec2(-10.5f, 0.0f), new B2Vec2(-10.5f, 20.5f), 0.5f);
            b2CreateCapsuleShape(groundId, ref shapeDef, ref capsule);
            capsule = new B2Capsule(new B2Vec2(10.5f, 0.0f), new B2Vec2(10.5f, 20.5f), 0.5f);
            b2CreateCapsuleShape(groundId, ref shapeDef, ref capsule);
            capsule = new B2Capsule(new B2Vec2(-10.5f, 20.5f), new B2Vec2(10.5f, 20.5f), 0.5f);
            b2CreateCapsuleShape(groundId, ref shapeDef, ref capsule);
        }

        m_row = 0;
        m_column = 0;
        m_count = 0;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.gravityScale = 0.0f;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);

            while (m_count < m_maxCount)
            {
                m_row = 0;
                for (int i = 0; i < m_gridCount; ++i)
                {
                    float x = -8.75f + m_column * 18.0f / m_gridCount;
                    float y = 1.5f + m_row * 18.0f / m_gridCount;

                    bodyDef.position = new B2Vec2(x, y);
                    B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                    b2CreateCircleShape(bodyId, ref shapeDef, ref circle);

                    m_count += 1;
                    m_row += 1;
                }

                m_column += 1;
            }
        }
    }
}
