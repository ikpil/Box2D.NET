// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples.Samples.Stackings;

public class CircleImpulse : Sample
{
    private static readonly int SampleCircleImpulse = SampleFactory.Shared.RegisterSample("Stacking", "Circle Impulse", Create);

    public struct Event
    {
        public float impulse;
        public float totalImpulse;
        public float speed;
    };

    private float m_mass;
    private List<Event> m_events = new List<Event>();
    private B2BodyId m_bodyId;
    private float m_gravity;
    private float m_restitution;
    private bool m_useGravity;
    private bool m_useRestitution;

    private static Sample Create(SampleContext context)
    {
        return new CircleImpulse(context);
    }

    public CircleImpulse(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 2.7f);
            m_context.camera.zoom = 3.4f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();

            B2Segment segment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(10.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        m_gravity = 10.0f;
        m_restitution = 0.25f;
        m_useGravity = false;
        m_useRestitution = false;
        m_mass = 1.0f;
        m_bodyId = b2_nullBodyId;

        Spawn();
    }

    void Spawn()
    {
        if (B2_IS_NON_NULL(m_bodyId))
        {
            b2DestroyBody(m_bodyId);
            m_bodyId = b2_nullBodyId;
        }

        m_events.Clear();

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.gravityScale = m_useGravity ? 1.0f : 0.0f;
        bodyDef.linearVelocity.Y = -25.0f;
        bodyDef.position.Y = 5.5f;

        B2Circle circle = new B2Circle();
        circle.radius = 0.25f;

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.enableHitEvents = true;
        shapeDef.material.friction = 0.0f;
        shapeDef.material.restitution = m_useRestitution ? m_restitution : 0.0f;

        m_bodyId = b2CreateBody(m_worldId, bodyDef);

        b2CreateCircleShape(m_bodyId, shapeDef, circle);

        // Override mass
        B2MassData massData = b2Body_GetMassData(m_bodyId);
        float ratio = m_mass / massData.mass;
        massData.mass = m_mass;
        massData.rotationalInertia *= ratio;
        b2Body_SetMassData(m_bodyId, massData);
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 6.0f * fontSize;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(10.0f * fontSize, height));

        ImGui.Begin("Circle Impulse", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (ImGui.Checkbox("gravity", ref m_useGravity))
        {
            Spawn();
        }

        if (ImGui.Checkbox("restitution", ref m_useRestitution))
        {
            Spawn();
        }

        ImGui.End();
    }

    public override void Step()
    {
        base.Step();

        B2ContactEvents events = b2World_GetContactEvents(m_worldId);
        for (int i = 0; i < events.hitCount; ++i)
        {
            ref readonly B2ContactHitEvent @event = ref events.hitEvents[i];

            DrawPoint(m_draw, @event.point, 10.0f, B2HexColor.b2_colorWhite);

            B2ContactData data = b2Contact_GetData(@event.contactId);

            Event e = new Event();
            e.speed = @event.approachSpeed;

            if (data.manifold.pointCount > 0)
            {
                e.impulse = data.manifold.points[0].normalImpulse;
                e.totalImpulse = data.manifold.points[0].totalNormalImpulse;
            }

            m_events.Add(e);
        }

        DrawTextLine($"mass = {m_mass}, gravity = {(m_useGravity ? 10.0f : 0.0f)}, restitution = {(m_useRestitution ? m_restitution : 0.0f)}");

        int eventCount = m_events.Count;
        var eventsSpan = CollectionsMarshal.AsSpan(m_events);
        for (int i = 0; i < eventCount; ++i)
        {
            ref readonly Event e = ref eventsSpan[i];
            DrawTextLine($"hit speed = {e.speed}, hit momentum = {m_mass * e.speed}, final impulse = {e.impulse}, total impulse = {e.totalImpulse}");
        }
    }
}