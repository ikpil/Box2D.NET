// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Contacts;

namespace Box2D.NET.Samples.Samples.Events;

public class PersistentContact : Sample
{
    private static int SamplePersistentContact = SampleFactory.Shared.RegisterSample("Events", "Persistent Contact", Create);

    private B2ContactId m_contactId;

    private static Sample Create(SampleContext context)
    {
        return new PersistentContact(context);
    }

    public PersistentContact(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 6.0f);
            m_context.camera.m_zoom = 7.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Vec2[] points = new B2Vec2[22];
            float x = 10.0f;
            for (int i = 0; i < 20; ++i)
            {
                points[i] = new B2Vec2(x, 0.0f);
                x -= 1.0f;
            }

            points[20] = new B2Vec2(-9.0f, 10.0f);
            points[21] = new B2Vec2(10.0f, 10.0f);

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = 22;
            chainDef.isLoop = true;

            b2CreateChain(groundId, ref chainDef);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-8.0f, 1.0f);
            bodyDef.linearVelocity = new B2Vec2(2.0f, 0.0f);

            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.enableContactEvents = true;
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }

        m_contactId = b2_nullContactId;
    }

    public override void Step()
    {
        base.Step();

        B2ContactEvents events = b2World_GetContactEvents(m_worldId);
        for (int i = 0; i < events.beginCount && i < 1; ++i)
        {
            B2ContactBeginTouchEvent @event = events.beginEvents[i];
            m_contactId = events.beginEvents[i].contactId;
        }

        for (int i = 0; i < events.endCount; ++i)
        {
            if (B2_ID_EQUALS(m_contactId, events.endEvents[i].contactId))
            {
                m_contactId = b2_nullContactId;
                break;
            }
        }

        if (B2_IS_NON_NULL(m_contactId) && b2Contact_IsValid(m_contactId))
        {
            B2ContactData data = b2Contact_GetData(m_contactId);

            for (int i = 0; i < data.manifold.pointCount; ++i)
            {
                ref readonly B2ManifoldPoint manifoldPoint = ref data.manifold.points[i];
                B2Vec2 p1 = manifoldPoint.point;
                B2Vec2 p2 = p1 + manifoldPoint.totalNormalImpulse * data.manifold.normal;
                m_draw.DrawLine(p1, p2, B2HexColor.b2_colorCrimson);
                m_draw.DrawPoint(p1, 6.0f, B2HexColor.b2_colorCrimson);
                m_draw.DrawString(p1, $"{manifoldPoint.totalNormalImpulse:F2}");
            }
        }
        else
        {
            m_contactId = b2_nullContactId;
        }
    }
}