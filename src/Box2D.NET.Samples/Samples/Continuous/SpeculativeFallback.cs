﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Continuous;

// Speculative collision failure case suggested by Dirk Gregorius. This uses
// a simple fallback scheme to prevent tunneling.
public class SpeculativeFallback : Sample
{
    private static readonly int SampleSpeculativeFallback = SampleFactory.Shared.RegisterSample("Continuous", "Speculative Fallback", Create);

    private static Sample Create(Settings settings)
    {
        return new SpeculativeFallback(settings);
    }

    public SpeculativeFallback(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(1.0f, 5.0f);
            B2.g_camera.m_zoom = 25.0f * 0.25f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(10.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);

            B2Vec2[] points = new B2Vec2[5]
            {
                new B2Vec2(-2.0f, 4.0f),
                new B2Vec2(2.0f, 4.0f),
                new B2Vec2(2.0f, 4.1f),
                new B2Vec2(-0.5f, 4.2f),
                new B2Vec2(-2.0f, 4.2f),
            };
            B2Hull hull = b2ComputeHull(points, 5);
            B2Polygon poly = b2MakePolygon(hull, 0.0f);
            b2CreatePolygonShape(groundId, shapeDef, poly);
        }

        // Fast moving skinny box. Also testing a large shape offset.
        {
            float offset = 8.0f;
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(offset, 12.0f);
            bodyDef.linearVelocity = new B2Vec2(0.0f, -100.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(2.0f, 0.05f, new B2Vec2(-offset, 0.0f), b2MakeRot(B2_PI));
            b2CreatePolygonShape(bodyId, shapeDef, box);
        }
    }
}
