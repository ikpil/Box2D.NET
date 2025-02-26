// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Continuous;

public class RestitutionThreshold : Sample
{
    B2BodyId m_ballId;

    static int sampleRestitutionThreshold = RegisterSample("Continuous", "Restitution Threshold", Create);

    static Sample Create(Settings settings)
    {
        return new RestitutionThreshold(settings);
    }

    public RestitutionThreshold(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new B2Vec2(7.0f, 5.0f);
            Draw.g_camera.m_zoom = 6.0f;
        }

        float pixelsPerMeter = 30.0f;

        // With the default threshold the ball will not bounce.
        b2World_SetRestitutionThreshold(m_worldId, 0.1f);

        {
            B2BodyDef block0BodyDef = b2DefaultBodyDef();
            block0BodyDef.type = B2BodyType.b2_staticBody;
            block0BodyDef.position = new B2Vec2(205.0f / pixelsPerMeter, 120.0f / pixelsPerMeter);
            block0BodyDef.rotation = b2MakeRot(70.0f * 3.14f / 180.0f);
            B2BodyId block0BodyId = b2CreateBody(m_worldId, block0BodyDef);
            B2Polygon block0Shape = b2MakeBox(50.0f / pixelsPerMeter, 5.0f / pixelsPerMeter);
            B2ShapeDef block0ShapeDef = b2DefaultShapeDef();
            block0ShapeDef.friction = 0.0f;
            b2CreatePolygonShape(block0BodyId, block0ShapeDef, block0Shape);
        }

        {
            // Make a ball
            B2BodyDef ballBodyDef = b2DefaultBodyDef();
            ballBodyDef.type = B2BodyType.b2_dynamicBody;
            ballBodyDef.position = new B2Vec2(200.0f / pixelsPerMeter, 250.0f / pixelsPerMeter);
            m_ballId = b2CreateBody(m_worldId, ballBodyDef);

            B2Circle ballShape = new B2Circle(new B2Vec2(), 0.0f);
            ballShape.radius = 5.0f / pixelsPerMeter;
            B2ShapeDef ballShapeDef = b2DefaultShapeDef();
            ballShapeDef.friction = 0.0f;
            ballShapeDef.restitution = 1.0f;
            b2CreateCircleShape(m_ballId, ballShapeDef, ballShape);

            b2Body_SetLinearVelocity(m_ballId, new B2Vec2(0.0f, -2.9f)); // Initial velocity
            b2Body_SetFixedRotation(m_ballId, true); // Do not rotate a ball
        }
    }

    public override void Step(Settings settings)
    {
        B2ContactData data = new B2ContactData();
        b2Body_GetContactData(m_ballId, [data], 1);

        B2Vec2 p = b2Body_GetPosition(m_ballId);
        B2Vec2 v = b2Body_GetLinearVelocity(m_ballId);
        Draw.g_draw.DrawString(5, m_textLine, "p.x = %.9f, v.y = %.9f", p.x, v.y);
        m_textLine += m_textIncrement;

        base.Step(settings);
    }
}
