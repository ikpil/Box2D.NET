﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Bodies;

// This shows how to set the initial angular velocity to get a specific movement.
public class Pivot : Sample
{
    private static readonly int SamplePivot = SampleFactory.Shared.RegisterSample("Bodies", "Pivot", Create);

    private B2BodyId m_bodyId;
    private float m_lever;

    private static Sample Create(SampleContext context)
    {
        return new Pivot(context);
    }

    public Pivot(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.8f, 6.4f);
            m_camera.m_zoom = 25.0f * 0.4f;
        }

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        // Create a separate body on the ground
        {
            B2Vec2 v = new B2Vec2(5.0f, 0.0f);

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 3.0f);
            bodyDef.gravityScale = 1.0f;
            bodyDef.linearVelocity = v;

            m_bodyId = b2CreateBody(m_worldId, ref bodyDef);

            m_lever = 3.0f;
            B2Vec2 r = new B2Vec2(0.0f, -m_lever);

            float omega = b2Cross(v, r) / b2Dot(r, r);
            b2Body_SetAngularVelocity(m_bodyId, omega);

            B2Polygon box = b2MakeBox(0.1f, m_lever);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyId, ref shapeDef, ref box);
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        B2Vec2 v = b2Body_GetLinearVelocity(m_bodyId);
        float omega = b2Body_GetAngularVelocity(m_bodyId);
        B2Vec2 r = b2Body_GetWorldVector(m_bodyId, new B2Vec2(0.0f, -m_lever));

        B2Vec2 vp = v + b2CrossSV(omega, r);
        DrawTextLine($"pivot velocity = ({vp.X:g}, {vp.Y:g})");
        
    }
}