﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Continuous;

public class RestitutionThreshold : Sample
{
    private static readonly int SampleRestitutionThreshold = SampleFactory.Shared.RegisterSample("Continuous", "Restitution Threshold", Create);

    private B2BodyId m_ballId;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new RestitutionThreshold(ctx, settings);
    }

    public RestitutionThreshold(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(7.0f, 5.0f);
            m_context.camera.m_zoom = 6.0f;
        }

        float pixelsPerMeter = 30.0f;

        // With the default threshold the ball will not bounce.
        b2World_SetRestitutionThreshold(m_worldId, 0.1f);

        {
            B2BodyDef block0BodyDef = b2DefaultBodyDef();
            block0BodyDef.type = B2BodyType.b2_staticBody;
            block0BodyDef.position = new B2Vec2(205.0f / pixelsPerMeter, 120.0f / pixelsPerMeter);
            block0BodyDef.rotation = b2MakeRot(70.0f * 3.14f / 180.0f);
            B2BodyId block0BodyId = b2CreateBody(m_worldId, ref block0BodyDef);
            B2Polygon block0Shape = b2MakeBox(50.0f / pixelsPerMeter, 5.0f / pixelsPerMeter);
            B2ShapeDef block0ShapeDef = b2DefaultShapeDef();
            block0ShapeDef.friction = 0.0f;
            b2CreatePolygonShape(block0BodyId, ref block0ShapeDef, ref block0Shape);
        }

        {
            // Make a ball
            B2BodyDef ballBodyDef = b2DefaultBodyDef();
            ballBodyDef.type = B2BodyType.b2_dynamicBody;
            ballBodyDef.position = new B2Vec2(200.0f / pixelsPerMeter, 250.0f / pixelsPerMeter);
            m_ballId = b2CreateBody(m_worldId, ref ballBodyDef);

            B2Circle ballShape = new B2Circle(new B2Vec2(), 0.0f);
            ballShape.radius = 5.0f / pixelsPerMeter;
            B2ShapeDef ballShapeDef = b2DefaultShapeDef();
            ballShapeDef.friction = 0.0f;
            ballShapeDef.restitution = 1.0f;
            b2CreateCircleShape(m_ballId, ref ballShapeDef, ref ballShape);

            b2Body_SetLinearVelocity(m_ballId, new B2Vec2(0.0f, -2.9f)); // Initial velocity
            b2Body_SetFixedRotation(m_ballId, true); // Do not rotate a ball
        }
    }

    public override void Step(Settings settings)
    {
        Span<B2ContactData> data = stackalloc B2ContactData[1];
        b2Body_GetContactData(m_ballId, data, data.Length);

        base.Step(settings);
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        B2Vec2 p = b2Body_GetPosition(m_ballId);
        B2Vec2 v = b2Body_GetLinearVelocity(m_ballId);
        m_context.draw.DrawString(5, m_textLine, $"p.x = {p.X:F9}, v.y = {v.Y:F9}");
        m_textLine += m_textIncrement;
    }
}