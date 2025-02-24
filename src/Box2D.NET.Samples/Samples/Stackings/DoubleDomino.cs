﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Stackings;

public class DoubleDomino : Sample
{
    static int sampleDoubleDomino = RegisterSample("Stacking", "Double Domino", Create);

    static Sample Create(Settings settings)
    {
        return new DoubleDomino(settings);
    }

    public DoubleDomino(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 4.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.25f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new b2Vec2(0.0f, -1.0f);
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeBox(100.0f, 1.0f);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        {
            b2Polygon box = b2MakeBox(0.125f, 0.5f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.6f;
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;

            int count = 15;
            float x = -0.5f * count;
            for (int i = 0; i < count; ++i)
            {
                bodyDef.position = new b2Vec2(x, 0.5f);
                b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                b2CreatePolygonShape(bodyId, shapeDef, box);
                if (i == 0)
                {
                    b2Body_ApplyLinearImpulse(bodyId, new b2Vec2(0.2f, 0.0f), new b2Vec2(x, 1.0f), true);
                }

                x += 1.0f;
            }
        }
    }
}
