﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;

public class ConveyorBelt : Sample
{
    static int sampleConveyorBelt = RegisterSample("Shapes", "Conveyor Belt", Create);

    static Sample Create(Settings settings)
    {
        return new ConveyorBelt(settings);
    }

    public ConveyorBelt(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(2.0f, 7.5f);
            Draw.g_camera.m_zoom = 12.0f;
        }

        // Ground
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment = new b2Segment(new b2Vec2(-20.0f, 0.0f), new b2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        // Platform
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new b2Vec2(-5.0f, 5.0f);
            b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeRoundedBox(10.0f, 0.25f, 0.25f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.8f;
            shapeDef.tangentSpeed = 2.0f;

            b2CreatePolygonShape(bodyId, shapeDef, box);
        }

        // Boxes
        {
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Polygon cube = b2MakeSquare(0.5f);
            for (int i = 0; i < 5; ++i)
            {
                b2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = b2BodyType.b2_dynamicBody;
                bodyDef.position = new b2Vec2(-10.0f + 2.0f * i, 7.0f);
                b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

                b2CreatePolygonShape(bodyId, shapeDef, cube);
            }
        }
    }
}
