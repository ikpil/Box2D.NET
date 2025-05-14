// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Continuous;

// This shows that Box2D does not have pixel perfect collision.
public class PixelImperfect : Sample
{
    private static readonly int SamplePixelImperfect = SampleFactory.Shared.RegisterSample("Continuous", "Pixel Imperfect", Create);

    private B2BodyId m_ballId;

    private static Sample Create(SampleContext context)
    {
        return new PixelImperfect(context);
    }

    public PixelImperfect(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(7.0f, 5.0f);
            m_context.camera.m_zoom = 6.0f;
        }

        float pixelsPerMeter = 30.0f;

        {
            B2BodyDef block4BodyDef = b2DefaultBodyDef();
            block4BodyDef.type = B2BodyType.b2_staticBody;
            block4BodyDef.position = new B2Vec2(175.0f / pixelsPerMeter, 150.0f / pixelsPerMeter);
            B2BodyId block4BodyId = b2CreateBody(m_worldId, ref block4BodyDef);
            B2Polygon block4Shape = b2MakeBox(20.0f / pixelsPerMeter, 10.0f / pixelsPerMeter);
            B2ShapeDef block4ShapeDef = b2DefaultShapeDef();
            block4ShapeDef.material.friction = 0.0f;
            b2CreatePolygonShape(block4BodyId, ref block4ShapeDef, ref block4Shape);
        }

        {
            B2BodyDef ballBodyDef = b2DefaultBodyDef();
            ballBodyDef.type = B2BodyType.b2_dynamicBody;
            ballBodyDef.position = new B2Vec2(200.0f / pixelsPerMeter, 275.0f / pixelsPerMeter);
            ballBodyDef.gravityScale = 0.0f;

            m_ballId = b2CreateBody(m_worldId, ref ballBodyDef);
            // Ball shape
            //b2Polygon ballShape = b2MakeBox( 5.f / pixelsPerMeter, 5.f / pixelsPerMeter );
            B2Polygon ballShape = b2MakeRoundedBox(4.0f / pixelsPerMeter, 4.0f / pixelsPerMeter, 0.9f / pixelsPerMeter);
            B2ShapeDef ballShapeDef = b2DefaultShapeDef();
            ballShapeDef.material.friction = 0.0f;
            //ballShapeDef.restitution = 1.f;
            b2CreatePolygonShape(m_ballId, ref ballShapeDef, ref ballShape);
            b2Body_SetLinearVelocity(m_ballId, new B2Vec2(0.0f, -5.0f));
            b2Body_SetFixedRotation(m_ballId, true);
        }
    }

    public override void Step()
    {
        Span<B2ContactData> data = stackalloc B2ContactData[1];
        b2Body_GetContactData(m_ballId, data, data.Length);

        base.Step();
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);
        
        B2Vec2 p = b2Body_GetPosition(m_ballId);
        B2Vec2 v = b2Body_GetLinearVelocity(m_ballId);
        DrawTextLine($"p.x = {p.X:F9}, v.y = {v.Y:F9}");
        
    }
}