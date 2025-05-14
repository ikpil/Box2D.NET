// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Stackings;

// A simple circle stack that also shows how to collect hit events
public class CircleStack : Sample
{
    private static readonly int SampleCircleStack = SampleFactory.Shared.RegisterSample("Stacking", "Circle Stack", Create);
    private List<Event> m_events = new List<Event>();

    private static Sample Create(SampleContext context)
    {
        return new CircleStack(context);
    }

    public struct Event
    {
        public int indexA, indexB;

        public Event(int indexA, int indexB)
        {
            this.indexA = indexA;
            this.indexB = indexB;
        }
    };

    public CircleStack(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 5.0f);
            m_context.camera.m_zoom = 6.0f;
        }

        int shapeIndex = 0;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.userData = shapeIndex;
            shapeIndex += 1;

            B2Segment segment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(10.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        b2World_SetGravity(m_worldId, new B2Vec2(0.0f, -20.0f));
        b2World_SetContactTuning(m_worldId, 0.25f * 360.0f, 10.0f, 3.0f);

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            B2Circle circle = new B2Circle();
            circle.radius = 0.5f;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.enableHitEvents = true;
            //shapeDef.rollingResistance = 0.2f;
            shapeDef.material.friction = 0.0f;

            float y = 0.75f;

            for ( int i = 0; i < 10; ++i )
            {
                bodyDef.position.Y = y;

                B2BodyId bodyId = b2CreateBody( m_worldId, ref bodyDef );

                shapeDef.userData = shapeIndex;
                shapeDef.density = 1.0f + 4.0f * i;
                shapeIndex += 1;
                b2CreateCircleShape( bodyId, ref shapeDef, ref circle );

                y += 1.25f;
            }
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        B2ContactEvents events = b2World_GetContactEvents(m_worldId);
        for (int i = 0; i < events.hitCount; ++i)
        {
            ref B2ContactHitEvent @event = ref events.hitEvents[i];

            object userDataA = b2Shape_GetUserData(@event.shapeIdA);
            object userDataB = b2Shape_GetUserData(@event.shapeIdB);
            int indexA = (int)userDataA;
            int indexB = (int)userDataB;

            m_context.draw.DrawPoint(@event.point, 10.0f, B2HexColor.b2_colorWhite);

            m_events.Add(new Event(indexA, indexB));
        }

        int eventCount = m_events.Count;
        for (int i = 0; i < eventCount; ++i)
        {
            DrawTextLine($"{m_events[i].indexA}, {m_events[i].indexB}");
            
        }
    }
}