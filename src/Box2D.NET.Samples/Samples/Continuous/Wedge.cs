// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Continuous;

// This shows the importance of secondary collisions in continuous physics.
// This also shows a difficult setup for the solver with an acute angle.
public class Wedge : Sample
{
    static int sampleWedge = RegisterSample("Continuous", "Wedge", Create);

    static Sample Create(Settings settings)
    {
        return new Wedge(settings);
    }

    public Wedge(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 5.5f);
            Draw.g_camera.m_zoom = 6.0f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment = new b2Segment(new b2Vec2(-4.0f, 8.0f), new b2Vec2(0.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
            segment = new b2Segment(new b2Vec2(0.0f, 0.0f), new b2Vec2(0.0f, 8.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = new b2Vec2(-0.45f, 10.75f);
            bodyDef.linearVelocity = new b2Vec2(0.0f, -200.0f);

            b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            b2Circle circle = new b2Circle(new b2Vec2(), 0.0f);
            circle.radius = 0.3f;
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.2f;
            b2CreateCircleShape(bodyId, shapeDef, circle);
        }
    }
}
