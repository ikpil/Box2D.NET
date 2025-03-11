// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Events;

public class SensorBookend : Sample
{
    private static readonly int SampleSensorBookendEvent = SampleFactory.Shared.RegisterSample("Events", "Sensor Bookend", Create);
    
    private B2BodyId m_sensorBodyId;
    private B2ShapeId m_sensorShapeId;

    private B2BodyId m_visitorBodyId;
    private B2ShapeId m_visitorShapeId;
    private bool m_isVisiting;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new SensorBookend(ctx, settings);
    }

    public SensorBookend(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 6.0f);
            m_context.camera.m_zoom = 7.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            B2Segment groundSegment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(10.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref groundSegment);

            groundSegment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(-10.0f, 10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref groundSegment);

            groundSegment = new B2Segment(new B2Vec2(10.0f, 0.0f), new B2Vec2(10.0f, 10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref groundSegment);

            m_isVisiting = false;
        }

        CreateSensor();

        CreateVisitor();
    }

    void CreateSensor()
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();

        bodyDef.position = new B2Vec2(0.0f, 1.0f);
        m_sensorBodyId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.isSensor = true;
        B2Polygon box = b2MakeSquare(1.0f);
        m_sensorShapeId = b2CreatePolygonShape(m_sensorBodyId, ref shapeDef, ref box);
    }

    void CreateVisitor()
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = new B2Vec2(-4.0f, 1.0f);
        bodyDef.type = B2BodyType.b2_dynamicBody;

        m_visitorBodyId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        m_visitorShapeId = b2CreateCircleShape(m_visitorBodyId, ref shapeDef, ref circle);
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 90.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(140.0f, height));

        ImGui.Begin("Sensor Bookend", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (B2_IS_NULL(m_visitorBodyId))
        {
            if (ImGui.Button("create visitor"))
            {
                CreateVisitor();
            }
        }
        else
        {
            if (ImGui.Button("destroy visitor"))
            {
                b2DestroyBody(m_visitorBodyId);
                m_visitorBodyId = b2_nullBodyId;
                m_visitorShapeId = b2_nullShapeId;
            }
        }

        if (B2_IS_NULL(m_sensorBodyId))
        {
            if (ImGui.Button("create sensor"))
            {
                CreateSensor();
            }
        }
        else
        {
            if (ImGui.Button("destroy sensor"))
            {
                b2DestroyBody(m_sensorBodyId);
                m_sensorBodyId = b2_nullBodyId;
                m_sensorShapeId = b2_nullShapeId;
            }
        }

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        B2SensorEvents sensorEvents = b2World_GetSensorEvents(m_worldId);
        for (int i = 0; i < sensorEvents.beginCount; ++i)
        {
            B2SensorBeginTouchEvent @event = sensorEvents.beginEvents[i];

            if (B2_ID_EQUALS(@event.visitorShapeId, m_visitorShapeId))
            {
                Debug.Assert(m_isVisiting == false);
                m_isVisiting = true;
            }
        }

        for (int i = 0; i < sensorEvents.endCount; ++i)
        {
            B2SensorEndTouchEvent @event = sensorEvents.endEvents[i];

            bool wasVisitorDestroyed = b2Shape_IsValid(@event.visitorShapeId) == false;
            if (B2_ID_EQUALS(@event.visitorShapeId, m_visitorShapeId) || wasVisitorDestroyed)
            {
                Debug.Assert(m_isVisiting == true);
                m_isVisiting = false;
            }
        }

        m_context.draw.DrawString(5, m_textLine, $"visiting == {m_isVisiting}");
        m_textLine += m_textIncrement;
    }
}