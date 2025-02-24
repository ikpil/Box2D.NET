﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Bodies;

// This shows how to set the initial angular velocity to get a specific movement.
public class Pivot : Sample
{
    b2BodyId m_bodyId;
    float m_lever;

    static int samplePivot = RegisterSample("Bodies", "Pivot", Create);

    static Sample Create(Settings settings)
    {
        return new Pivot(settings);
    }

    public Pivot(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.8f, 6.4f);
            Draw.g_camera.m_zoom = 25.0f * 0.4f;
        }

        b2BodyId groundId = b2_nullBodyId;
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);

            b2Segment segment = new b2Segment(new b2Vec2(-20.0f, 0.0f), new b2Vec2(20.0f, 0.0f));
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        // Create a separate body on the ground
        {
            b2Vec2 v = new b2Vec2(5.0f, 0.0f);

            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = new b2Vec2(0.0f, 3.0f);
            bodyDef.gravityScale = 1.0f;
            bodyDef.linearVelocity = v;

            m_bodyId = b2CreateBody(m_worldId, bodyDef);

            m_lever = 3.0f;
            b2Vec2 r = new b2Vec2(0.0f, -m_lever);

            float omega = b2Cross(v, r) / b2Dot(r, r);
            b2Body_SetAngularVelocity(m_bodyId, omega);

            b2Polygon box = b2MakeBox(0.1f, m_lever);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyId, shapeDef, box);
        }
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        b2Vec2 v = b2Body_GetLinearVelocity(m_bodyId);
        float omega = b2Body_GetAngularVelocity(m_bodyId);
        b2Vec2 r = b2Body_GetWorldVector(m_bodyId, new b2Vec2(0.0f, -m_lever));

        b2Vec2 vp = v + b2CrossSV(omega, r);
        Draw.g_draw.DrawString(5, m_textLine, "pivot velocity = (%g, %g)", vp.x, vp.y);
        m_textLine += m_textIncrement;
    }
}
