// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Stackings;

// From PEEL
public class CardHouse : Sample
{
    private static readonly int SampleCardHouse = SampleFactory.Shared.RegisterSample("Stacking", "Card House", Create);

    private static Sample Create(Settings settings)
    {
        return new CardHouse(settings);
    }

    public CardHouse(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.75f, 0.9f);
            B2.g_camera.m_zoom = 25.0f * 0.05f;
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = new B2Vec2(0.0f, -2.0f);
        B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.friction = 0.7f;

        B2Polygon groundBox = b2MakeBox(40.0f, 2.0f);
        b2CreatePolygonShape(groundId, shapeDef, groundBox);

        float cardHeight = 0.2f;
        float cardThickness = 0.001f;

        float angle0 = 25.0f * B2_PI / 180.0f;
        float angle1 = -25.0f * B2_PI / 180.0f;
        float angle2 = 0.5f * B2_PI;

        B2Polygon cardBox = b2MakeBox(cardThickness, cardHeight);
        bodyDef.type = B2BodyType.b2_dynamicBody;

        int Nb = 5;
        float z0 = 0.0f;
        float y = cardHeight - 0.02f;
        while (0 < Nb)
        {
            float z = z0;
            for (int i = 0; i < Nb; i++)
            {
                if (i != Nb - 1)
                {
                    bodyDef.position = new B2Vec2(z + 0.25f, y + cardHeight - 0.015f);
                    bodyDef.rotation = b2MakeRot(angle2);
                    B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                    b2CreatePolygonShape(bodyId, shapeDef, cardBox);
                }

                {
                    bodyDef.position = new B2Vec2(z, y);
                    bodyDef.rotation = b2MakeRot(angle1);
                    B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                    b2CreatePolygonShape(bodyId, shapeDef, cardBox);

                    z += 0.175f;

                    bodyDef.position = new B2Vec2(z, y);
                    bodyDef.rotation = b2MakeRot(angle0);
                    bodyId = b2CreateBody(m_worldId, bodyDef);
                    b2CreatePolygonShape(bodyId, shapeDef, cardBox);

                    z += 0.175f;
                }
            }

            y += cardHeight * 2.0f - 0.03f;
            z0 += 0.175f;
            Nb--;
        }
    }
}
