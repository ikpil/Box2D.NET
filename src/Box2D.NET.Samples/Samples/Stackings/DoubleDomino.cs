// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Stackings;

public class DoubleDomino : Sample
{
    private static readonly int SampleDoubleDomino = SampleFactory.Shared.RegisterSample("Stacking", "Double Domino", Create);

    private static Sample Create(SampleContext context)
    {
        return new DoubleDomino(context);
    }

    public DoubleDomino(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 4.0f);
            m_camera.zoom = 25.0f * 0.25f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, -1.0f);
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeBox(100.0f, 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        {
            B2Polygon box = b2MakeBox(0.125f, 0.5f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.6f;
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            int count = 15;
            float x = -0.5f * count;
            for (int i = 0; i < count; ++i)
            {
                bodyDef.position = new B2Vec2(x, 0.5f);
                B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
                if (i == 0)
                {
                    b2Body_ApplyLinearImpulse(bodyId, new B2Vec2(0.2f, 0.0f), new B2Vec2(x, 1.0f), true);
                }

                x += 1.0f;
            }
        }
    }
}
