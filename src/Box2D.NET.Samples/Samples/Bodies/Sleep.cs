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
    private static readonly int SampleSleep = SampleFactory.Shared.RegisterSample("Bodies", "Sleep", Create);

    private B2BodyId m_pendulumId;
    private B2BodyId m_staticBodyId;
    private B2ShapeId m_groundShapeId;
    private B2ShapeId[] m_sensorIds = new B2ShapeId[2];
    private bool[] m_sensorTouching = new bool[2];

    private static Sample Create(SampleContext context)
    {
        return new Sleep(context);
    }

    public Sleep(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(3.0f, 50.0f);
            m_camera.m_zoom = 25.0f * 2.2f;
        }

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.enableSensorEvents = true;
            m_groundShapeId = b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        // Sleeping body with sensors
        for (int i = 0; i < 2; ++i)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-4.0f, 3.0f + 2.0f * i);
            bodyDef.isAwake = false;
            bodyDef.enableSleep = true;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, 1.0f), new B2Vec2(1.0f, 1.0f), 0.75f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);

            shapeDef.isSensor = true;
            shapeDef.enableSensorEvents = true;
            capsule.radius = 1.0f;
            m_sensorIds[i] = b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);
            m_sensorTouching[i] = false;
        }

        // Sleeping body but sleep is disabled
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 3.0f);
            bodyDef.isAwake = false;
            bodyDef.enableSleep = false;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Circle circle = new B2Circle(new B2Vec2(1.0f, 1.0f), 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }

        // Awake body and sleep is disabled
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(5.0f, 3.0f);
            bodyDef.isAwake = true;
            bodyDef.enableSleep = false;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeOffsetBox(1.0f, 1.0f, new B2Vec2(0.0f, 1.0f), b2MakeRot(0.25f * B2_PI));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        // A sleeping body to test waking on collision
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(5.0f, 1.0f);
            bodyDef.isAwake = false;
            bodyDef.enableSleep = true;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeSquare(1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        // A long pendulum
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 100.0f);
            bodyDef.angularDamping = 0.5f;
            bodyDef.sleepThreshold = 0.05f;
            m_pendulumId = b2CreateBody(m_worldId, ref bodyDef);

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, 0.0f), new B2Vec2(90.0f, 0.0f), 0.25f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateCapsuleShape(m_pendulumId, ref shapeDef, ref capsule);

            B2Vec2 pivot = bodyDef.position;
            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = m_pendulumId;
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
            b2CreateRevoluteJoint(m_worldId, ref jointDef);
        }

        // A sleeping body to test waking on contact destroyed
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-10.0f, 1.0f);
            bodyDef.isAwake = false;
            bodyDef.enableSleep = true;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeSquare(1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        m_staticBodyId = b2_nullBodyId;
    }

    void ToggleInvoker()
    {
        if (B2_IS_NULL(m_staticBodyId))
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(-10.5f, 3.0f);
            m_staticBodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeOffsetBox(2.0f, 0.1f, new B2Vec2(0.0f, 0.0f), b2MakeRot(0.25f * B2_PI));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.invokeContactCreation = true;
            b2CreatePolygonShape(m_staticBodyId, ref shapeDef, ref box);
        }
        else
        {
            b2DestroyBody(m_staticBodyId);
            m_staticBodyId = b2_nullBodyId;
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();
        float height = 160.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));
        ImGui.Begin("Sleep", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

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

        ImGui.Separator();

        if (B2_IS_NULL(m_staticBodyId))
        {
            if (ImGui.Button("Create"))
            {
                ToggleInvoker();
            }
        }
        else
        {
            if (ImGui.Button("Destroy"))
            {
                ToggleInvoker();
            }
        }

        ImGui.End();
    }

    public override void Step()
    {
        base.Step();

        // Detect sensors touching the ground
        B2SensorEvents sensorEvents = b2World_GetSensorEvents(m_worldId);

        for (int i = 0; i < sensorEvents.beginCount; ++i)
        {
            ref B2SensorBeginTouchEvent @event = ref sensorEvents.beginEvents[i];
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
            ref B2SensorEndTouchEvent @event = ref sensorEvents.endEvents[i];
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
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        for (int i = 0; i < 2; ++i)
        {
            DrawTextLine($"sensor touch {i} = {m_sensorTouching[i]}");
        }
    }
}