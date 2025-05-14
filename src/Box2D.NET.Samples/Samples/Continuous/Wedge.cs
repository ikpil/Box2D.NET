﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Continuous;

// This shows the importance of secondary collisions in continuous physics.
// This also shows a difficult setup for the solver with an acute angle.
public class Wedge : Sample
{
    private static readonly int SampleWedge = SampleFactory.Shared.RegisterSample("Continuous", "Wedge", Create);

    private static Sample Create(SampleContext context)
    {
        return new Wedge(context);
    }

    public Wedge(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 5.5f);
            m_context.camera.m_zoom = 6.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-4.0f, 8.0f), new B2Vec2(0.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
            segment = new B2Segment(new B2Vec2(0.0f, 0.0f), new B2Vec2(0.0f, 8.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-0.45f, 10.75f);
            bodyDef.linearVelocity = new B2Vec2(0.0f, -200.0f);

            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Circle circle = new B2Circle(new B2Vec2(), 0.0f);
            circle.radius = 0.3f;
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.2f;
            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }
    }
}
