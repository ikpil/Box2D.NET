﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Continuous;

// This shows that while Box2D uses speculative collision, it does not lead to speculative ghost collisions at small distances
public class SpeculativeGhost : Sample
{
    static int sampleSpeculativeGhost = RegisterSample("Continuous", "Speculative Ghost", Create);

    static Sample Create(Settings settings)
    {
        return new SpeculativeGhost(settings);
    }

    public SpeculativeGhost(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 1.75f);
            Draw.g_camera.m_zoom = 2.0f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment = new b2Segment(new b2Vec2(-10.0f, 0.0f), new b2Vec2(10.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);

            b2Polygon box = b2MakeOffsetBox(1.0f, 0.1f, new b2Vec2(0.0f, 0.9f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;

            // The speculative distance is 0.02 meters, so this avoid it
            bodyDef.position = new b2Vec2(0.015f, 2.515f);
            bodyDef.linearVelocity = new b2Vec2(0.1f * 1.25f * settings.hertz, -0.1f * 1.25f * settings.hertz);
            bodyDef.gravityScale = 0.0f;
            b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Polygon box = b2MakeSquare(0.25f);
            b2CreatePolygonShape(bodyId, shapeDef, box);
        }
    }
}
