// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Silk.NET.GLFW;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2Joints;

namespace Box2D.NET.Samples.Samples.Events;

// This shows how to create a projectile that explodes on impact
class ProjectileEvent : Sample
{
    private static readonly int sampleProjectileEvent = SampleFactory.Shared.RegisterSample("Events", "Projectile Event", Create);

    private B2BodyId m_projectileId;
    private B2ShapeId m_projectileShapeId;
    private B2Vec2 m_point1;
    private B2Vec2 m_point2;
    private bool m_dragging;

    private static Sample Create(SampleContext context)
    {
        return new ProjectileEvent(context);
    }

    public ProjectileEvent(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(-7.0f, 9.0f);
            m_context.camera.m_zoom = 14.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, 0.0f);
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.enableSensorEvents = true;

            B2Segment segment = new B2Segment(new B2Vec2(10.0f, 0.0f), new B2Vec2(10.0f, 20.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

            segment = new B2Segment(new B2Vec2(-30.0f, 0.0f), new B2Vec2(30.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        m_projectileId = new B2BodyId();
        m_projectileShapeId = new B2ShapeId();
        m_dragging = false;
        m_point1 = b2Vec2_zero;
        m_point2 = b2Vec2_zero;

        {
            B2Polygon box = b2MakeRoundedBox(0.45f, 0.45f, 0.05f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.enableSensorEvents = true;

            float offset = 0.01f;

            float x = 8.0f;
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            for (int i = 0; i < 8; ++i)
            {
                float shift = (i % 2 == 0 ? -offset : offset);
                bodyDef.position = new B2Vec2(x + shift, 0.5f + 1.0f * i);

                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
            }
        }
    }

    void FireProjectile()
    {
        if (B2_IS_NON_NULL(m_projectileId))
        {
            b2DestroyBody(m_projectileId);
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = m_point1;
        bodyDef.linearVelocity = 4.0f * (m_point2 - m_point1);
        bodyDef.isBullet = true;

        m_projectileId = b2CreateBody(m_worldId, ref bodyDef);

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.25f);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.enableContactEvents = true;
        m_projectileShapeId = b2CreateCircleShape(m_projectileId, ref shapeDef, ref circle);
    }

    public override void MouseDown(B2Vec2 p, MouseButton button, KeyModifiers mod)
    {
        if (button == MouseButton.Left)
        {
            if (mod == KeyModifiers.Control)
            {
                m_dragging = true;
                m_point1 = p;
            }
        }
    }

    public override void MouseUp(B2Vec2 p, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            if (m_dragging)
            {
                m_dragging = false;
                FireProjectile();
            }
        }
    }

    public override void MouseMove(B2Vec2 p)
    {
        if (m_dragging)
        {
            m_point2 = p;
        }
    }

    public override void Step()
    {
        base.Step();


        B2ContactEvents contactEvents = b2World_GetContactEvents(m_worldId);
        for (int i = 0; i < contactEvents.beginCount; ++i)
        {
            ref B2ContactBeginTouchEvent @event = ref contactEvents.beginEvents[i];

            if (B2_ID_EQUALS(@event.shapeIdA, m_projectileShapeId) || B2_ID_EQUALS(@event.shapeIdB, m_projectileShapeId))
            {
                if (b2Contact_IsValid(@event.contactId))
                {
                    B2ContactData data = b2Contact_GetData(@event.contactId);

                    if (data.manifold.pointCount > 0)
                    {
                        B2ExplosionDef explosionDef = b2DefaultExplosionDef();
                        explosionDef.position = data.manifold.points[0].point;
                        explosionDef.radius = 1.0f;
                        explosionDef.impulsePerLength = 20.0f;
                        b2World_Explode(m_worldId, ref explosionDef);

                        b2DestroyBody(m_projectileId);
                        m_projectileId = b2_nullBodyId;
                    }
                }

                break;
            }
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);
        
        DrawTextLine("Use Ctrl + Left Mouse to drag and shoot a projectile");

        if (m_dragging)
        {
            m_draw.DrawLine(m_point1, m_point2, B2HexColor.b2_colorWhite);
            m_draw.DrawPoint(m_point1, 5.0f, B2HexColor.b2_colorGreen);
            m_draw.DrawPoint(m_point2, 5.0f, B2HexColor.b2_colorRed);
        }
    }
}