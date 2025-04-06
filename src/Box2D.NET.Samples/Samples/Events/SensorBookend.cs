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
    private static readonly int sampleSensorBookendEvent = SampleFactory.Shared.RegisterSample("Events", "Sensor Bookend", Create);

    private B2BodyId m_sensorBodyId1;
    private B2ShapeId m_sensorShapeId1;

    private B2BodyId m_sensorBodyId2;
    private B2ShapeId m_sensorShapeId2;

    private B2BodyId m_visitorBodyId;
    private B2ShapeId m_visitorShapeId;

    private bool m_isVisiting1;
    private bool m_isVisiting2;
    private int m_sensorsOverlapCount;


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

            m_isVisiting1 = false;
            m_isVisiting2 = false;
            m_sensorsOverlapCount = 0;
        }

        CreateSensor1();
        CreateSensor2();
        CreateVisitor();
    }

    void CreateSensor1()
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();

        bodyDef.position = new B2Vec2(-2.0f, 1.0f);
        m_sensorBodyId1 = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.isSensor = true;
        shapeDef.enableSensorEvents = true;

        B2Polygon box = b2MakeSquare(1.0f);
        m_sensorShapeId1 = b2CreatePolygonShape(m_sensorBodyId1, ref shapeDef, ref box);
    }

    void CreateSensor2()
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(2.0f, 1.0f);
        m_sensorBodyId2 = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.isSensor = true;
        shapeDef.enableSensorEvents = true;

        B2Polygon box = b2MakeRoundedBox(0.5f, 0.5f, 0.5f);
        m_sensorShapeId2 = b2CreatePolygonShape(m_sensorBodyId2, ref shapeDef, ref box);

        // Solid middle
        shapeDef.isSensor = false;
        shapeDef.enableSensorEvents = false;
        box = b2MakeSquare(0.5f);
        b2CreatePolygonShape(m_sensorBodyId2, ref shapeDef, ref box);
    }

    void CreateVisitor()
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = new B2Vec2(-4.0f, 1.0f);
        bodyDef.type = B2BodyType.b2_dynamicBody;

        m_visitorBodyId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.enableSensorEvents = true;

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        m_visitorShapeId = b2CreateCircleShape(m_visitorBodyId, ref shapeDef, ref circle);
    }

    public override void UpdateGui()
    {
        float height = 260.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));

        ImGui.Begin("Sensor Bookend", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

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
                // Retain m_visitorShapeId for end events.
            }
            else
            {
                bool enabledEvents = b2Shape_AreSensorEventsEnabled(m_visitorShapeId);
                if (ImGui.Checkbox("visitor events", ref enabledEvents))
                {
                    b2Shape_EnableSensorEvents(m_visitorShapeId, enabledEvents);
                }

                bool enabledBody = b2Body_IsEnabled(m_visitorBodyId);
                if (ImGui.Checkbox("enable visitor body", ref enabledBody))
                {
                    if (enabledBody)
                    {
                        b2Body_Enable(m_visitorBodyId);
                    }
                    else
                    {
                        b2Body_Disable(m_visitorBodyId);
                    }
                }
            }
        }

        ImGui.Separator();

        if (B2_IS_NULL(m_sensorBodyId1))
        {
            if (ImGui.Button("create sensor1"))
            {
                CreateSensor1();
            }
        }
        else
        {
            if (ImGui.Button("destroy sensor1"))
            {
                b2DestroyBody(m_sensorBodyId1);
                m_sensorBodyId1 = b2_nullBodyId;
                // Retain m_sensorShapeId1 for end events.
            }
            else
            {
                bool enabledEvents = b2Shape_AreSensorEventsEnabled(m_sensorShapeId1);
                if (ImGui.Checkbox("sensor 1 events", ref enabledEvents))
                {
                    b2Shape_EnableSensorEvents(m_sensorShapeId1, enabledEvents);
                }

                bool enabledBody = b2Body_IsEnabled(m_sensorBodyId1);
                if (ImGui.Checkbox("enable sensor1 body", ref enabledBody))
                {
                    if (enabledBody)
                    {
                        b2Body_Enable(m_sensorBodyId1);
                    }
                    else
                    {
                        b2Body_Disable(m_sensorBodyId1);
                    }
                }
            }
        }

        ImGui.Separator();

        if (B2_IS_NULL(m_sensorBodyId2))
        {
            if (ImGui.Button("create sensor2"))
            {
                CreateSensor2();
            }
        }
        else
        {
            if (ImGui.Button("destroy sensor2"))
            {
                b2DestroyBody(m_sensorBodyId2);
                m_sensorBodyId2 = b2_nullBodyId;
                // Retain m_sensorShapeId2 for end events.
            }
            else
            {
                bool enabledEvents = b2Shape_AreSensorEventsEnabled(m_sensorShapeId2);
                if (ImGui.Checkbox("sensor2 events", ref enabledEvents))
                {
                    b2Shape_EnableSensorEvents(m_sensorShapeId2, enabledEvents);
                }

                bool enabledBody = b2Body_IsEnabled(m_sensorBodyId2);
                if (ImGui.Checkbox("enable sensor2 body", ref enabledBody))
                {
                    if (enabledBody)
                    {
                        b2Body_Enable(m_sensorBodyId2);
                    }
                    else
                    {
                        b2Body_Disable(m_sensorBodyId2);
                    }
                }
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

            if (B2_ID_EQUALS(@event.sensorShapeId, m_sensorShapeId1))
            {
                if (B2_ID_EQUALS(@event.visitorShapeId, m_visitorShapeId))
                {
                    Debug.Assert(m_isVisiting1 == false);
                    m_isVisiting1 = true;
                }
                else
                {
                    Debug.Assert(B2_ID_EQUALS(@event.visitorShapeId, m_sensorShapeId2));
                    m_sensorsOverlapCount += 1;
                }
            }
            else
            {
                Debug.Assert(B2_ID_EQUALS(@event.sensorShapeId, m_sensorShapeId2));

                if (B2_ID_EQUALS(@event.visitorShapeId, m_visitorShapeId))
                {
                    Debug.Assert(m_isVisiting2 == false);
                    m_isVisiting2 = true;
                }
                else
                {
                    Debug.Assert(B2_ID_EQUALS(@event.visitorShapeId, m_sensorShapeId1));
                    m_sensorsOverlapCount += 1;
                }
            }
        }

        Debug.Assert(m_sensorsOverlapCount == 0 || m_sensorsOverlapCount == 2);

        for (int i = 0; i < sensorEvents.endCount; ++i)
        {
            B2SensorEndTouchEvent @event = sensorEvents.endEvents[i];

            if (B2_ID_EQUALS(@event.sensorShapeId, m_sensorShapeId1))
            {
                if (B2_ID_EQUALS(@event.visitorShapeId, m_visitorShapeId))
                {
                    Debug.Assert(m_isVisiting1 == true);
                    m_isVisiting1 = false;
                }
                else
                {
                    Debug.Assert(B2_ID_EQUALS(@event.visitorShapeId, m_sensorShapeId2));
                    m_sensorsOverlapCount -= 1;
                }
            }
            else
            {
                Debug.Assert(B2_ID_EQUALS(@event.sensorShapeId, m_sensorShapeId2));

                if (B2_ID_EQUALS(@event.visitorShapeId, m_visitorShapeId))
                {
                    Debug.Assert(m_isVisiting2 == true);
                    m_isVisiting2 = false;
                }
                else
                {
                    Debug.Assert(B2_ID_EQUALS(@event.visitorShapeId, m_sensorShapeId1));
                    m_sensorsOverlapCount -= 1;
                }
            }
        }

        Debug.Assert(m_sensorsOverlapCount == 0 || m_sensorsOverlapCount == 2);

        // Nullify invalid shape ids after end events are processed.
        if (b2Shape_IsValid(m_visitorShapeId) == false)
        {
            m_visitorShapeId = b2_nullShapeId;
        }

        if (b2Shape_IsValid(m_sensorShapeId1) == false)
        {
            m_sensorShapeId1 = b2_nullShapeId;
        }

        if (b2Shape_IsValid(m_sensorShapeId2) == false)
        {
            m_sensorShapeId2 = b2_nullShapeId;
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        DrawTextLine($"visiting 1 == {m_isVisiting1}");
        DrawTextLine($"visiting 2 == {m_isVisiting2}");
        DrawTextLine($"sensors overlap count == {m_sensorsOverlapCount}");
    }
}