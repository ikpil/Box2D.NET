// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Bodies;

public class Sleep : Sample
{
    B2BodyId m_pendulumId;
    B2ShapeId m_groundShapeId;
    B2ShapeId[] m_sensorIds = new B2ShapeId[2];
    bool[] m_sensorTouching = new bool[2];

    private static readonly int SampleSleep = SampleFactory.Shared.RegisterSample("Bodies", "Sleep", Create);

    private static Sample Create(Settings settings)
    {
        return new Sleep(settings);
    }

    public Sleep(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(3.0f, 50.0f);
            B2.g_camera.m_zoom = 25.0f * 2.2f;
        }

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);

            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            m_groundShapeId = b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        // Sleeping body with sensors
        for (int i = 0; i < 2; ++i)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-4.0f, 3.0f + 2.0f * i);
            bodyDef.isAwake = false;
            bodyDef.enableSleep = true;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, 1.0f), new B2Vec2(1.0f, 1.0f), 0.75f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateCapsuleShape(bodyId, shapeDef, capsule);

            shapeDef.isSensor = true;
            capsule.radius = 1.0f;
            m_sensorIds[i] = b2CreateCapsuleShape(bodyId, shapeDef, capsule);
            m_sensorTouching[i] = false;
        }

        // Sleeping body but sleep is disabled
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 3.0f);
            bodyDef.isAwake = false;
            bodyDef.enableSleep = false;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2Circle circle = new B2Circle(new B2Vec2(1.0f, 1.0f), 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateCircleShape(bodyId, shapeDef, circle);
        }

        // Awake body and sleep is disabled
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(5.0f, 3.0f);
            bodyDef.isAwake = true;
            bodyDef.enableSleep = false;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeOffsetBox(1.0f, 1.0f, new B2Vec2(0.0f, 1.0f), b2MakeRot(0.25f * B2_PI));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(bodyId, shapeDef, box);
        }

        // A sleeping body to test waking on collision
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(5.0f, 1.0f);
            bodyDef.isAwake = false;
            bodyDef.enableSleep = true;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeSquare(1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(bodyId, shapeDef, box);
        }

        // A long pendulum
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 100.0f);
            bodyDef.angularDamping = 0.5f;
            bodyDef.sleepThreshold = 0.05f;
            m_pendulumId = b2CreateBody(m_worldId, bodyDef);

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, 0.0f), new B2Vec2(90.0f, 0.0f), 0.25f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateCapsuleShape(m_pendulumId, shapeDef, capsule);

            B2Vec2 pivot = bodyDef.position;
            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = m_pendulumId;
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            b2CreateRevoluteJoint(m_worldId, jointDef);
        }
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 100.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, B2.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));
        ImGui.Begin("Sleep", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        ImGui.PushItemWidth(120.0f);

        ImGui.Text("Pendulum Tuning");

        float sleepVelocity = b2Body_GetSleepThreshold(m_pendulumId);
        if (ImGui.SliderFloat("sleep velocity", ref sleepVelocity, 0.0f, 1.0f, "%.2f"))
        {
            b2Body_SetSleepThreshold(m_pendulumId, sleepVelocity);
            b2Body_SetAwake(m_pendulumId, true);
        }

        float angularDamping = b2Body_GetAngularDamping(m_pendulumId);
        if (ImGui.SliderFloat("angular damping", ref angularDamping, 0.0f, 2.0f, "%.2f"))
        {
            b2Body_SetAngularDamping(m_pendulumId, angularDamping);
        }

        ImGui.PopItemWidth();

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        // Detect sensors touching the ground
        B2SensorEvents sensorEvents = b2World_GetSensorEvents(m_worldId);

        for (int i = 0; i < sensorEvents.beginCount; ++i)
        {
            B2SensorBeginTouchEvent @event = sensorEvents.beginEvents[i];
            if (B2_ID_EQUALS(@event.visitorShapeId, m_groundShapeId))
            {
                if (B2_ID_EQUALS(@event.sensorShapeId, m_sensorIds[0]))
                {
                    m_sensorTouching[0] = true;
                }
                else if (B2_ID_EQUALS(@event.sensorShapeId, m_sensorIds[1]))
                {
                    m_sensorTouching[1] = true;
                }
            }
        }

        for (int i = 0; i < sensorEvents.endCount; ++i)
        {
            B2SensorEndTouchEvent @event = sensorEvents.endEvents[i];
            if (B2_ID_EQUALS(@event.visitorShapeId, m_groundShapeId))
            {
                if (B2_ID_EQUALS(@event.sensorShapeId, m_sensorIds[0]))
                {
                    m_sensorTouching[0] = false;
                }
                else if (B2_ID_EQUALS(@event.sensorShapeId, m_sensorIds[1]))
                {
                    m_sensorTouching[1] = false;
                }
            }
        }

        for (int i = 0; i < 2; ++i)
        {
            B2.g_draw.DrawString(5, m_textLine, "sensor touch %d = %s", i, m_sensorTouching[i] ? "true" : "false");
            m_textLine += m_textIncrement;
        }
    }
}
