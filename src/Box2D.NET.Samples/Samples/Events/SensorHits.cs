// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2PrismaticJoints;
using static Box2D.NET.Shared.RandomSupports;
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples.Samples.Events;

public class SensorHits : Sample
{
    private static readonly int SampleSensorHits = SampleFactory.Shared.RegisterSample("Events", "Sensor Hits", Create);

    private B2ShapeId m_staticSensorId;
    private B2ShapeId m_kinematicSensorId;
    private B2ShapeId m_dynamicSensorId;

    private B2BodyId m_kinematicBodyId;
    private B2BodyId m_dynamicBodyId;
    private B2JointId m_jointId;

    private B2BodyId m_bodyId;
    private B2ShapeId m_shapeId;

    private const int m_transformCapacity = 20;
    private int m_transformCount;
    private B2Transform[] m_transforms = new B2Transform[m_transformCapacity];

    private bool m_isBullet;
    private int m_beginCount;
    private int m_endCount;

    private static Sample Create(SampleContext context)
    {
        return new SensorHits(context);
    }

    public SensorHits(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 5.0f);
            m_context.camera.zoom = 7.5f;
        }

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.name = "ground";

            groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            B2Segment groundSegment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(10.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref groundSegment);

            groundSegment = new B2Segment(new B2Vec2(10.0f, 0.0f), new B2Vec2(10.0f, 10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref groundSegment);
        }

        // Static sensor
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.name = "static sensor";
            bodyDef.position = new B2Vec2(-4.0f, 1.0f);

            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.isSensor = true;
            shapeDef.enableSensorEvents = true;

            B2Segment segment = new B2Segment(new B2Vec2(0.0f, 0.0f), new B2Vec2(0.0f, 10.0f));
            m_staticSensorId = b2CreateSegmentShape(bodyId, ref shapeDef, ref segment);
        }

        // Kinematic sensor
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.name = "kinematic sensor";
            bodyDef.type = B2BodyType.b2_kinematicBody;
            bodyDef.position = new B2Vec2(0.0f, 1.0f);
            bodyDef.linearVelocity = new B2Vec2(0.5f, 0.0f);

            m_kinematicBodyId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.isSensor = true;
            shapeDef.enableSensorEvents = true;

            B2Segment segment = new B2Segment(new B2Vec2(0.0f, 0.0f), new B2Vec2(0.0f, 10.0f));
            m_kinematicSensorId = b2CreateSegmentShape(m_kinematicBodyId, ref shapeDef, ref segment);
        }

        // Dynamic sensor
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.name = "dynamic sensor";
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(4.0f, 1.0f);

            m_dynamicBodyId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.isSensor = true;
            shapeDef.enableSensorEvents = true;

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, 1.0f), new B2Vec2(0.0f, 9.0f), 0.1f);
            m_dynamicSensorId = b2CreateCapsuleShape(m_dynamicBodyId, ref shapeDef, ref capsule);

            B2Vec2 pivot = bodyDef.position + new B2Vec2(0.0f, 6.0f);
            B2Vec2 axis = new B2Vec2(1.0f, 0.0f);
            B2PrismaticJointDef jointDef = b2DefaultPrismaticJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = m_dynamicBodyId;
            jointDef.@base.localFrameA.q = b2MakeRotFromUnitVector(axis);
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(groundId, pivot);
            jointDef.@base.localFrameB.q = b2MakeRotFromUnitVector(axis);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(m_dynamicBodyId, pivot);
            jointDef.enableMotor = true;
            jointDef.maxMotorForce = 1000.0f;
            jointDef.motorSpeed = 0.5f;

            m_jointId = b2CreatePrismaticJoint(m_worldId, ref jointDef);
        }

        m_beginCount = 0;
        m_endCount = 0;
        m_bodyId = new B2BodyId();
        m_shapeId = new B2ShapeId();
        m_transformCount = 0;
        m_isBullet = true;

        Launch();
    }

    void Launch()
    {
        if (B2_IS_NON_NULL(m_bodyId))
        {
            b2DestroyBody(m_bodyId);
        }

        m_transformCount = 0;
        m_beginCount = 0;
        m_endCount = 0;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(-26.7f, 6.0f);
        float speed = RandomFloatRange(200.0f, 300.0f);
        bodyDef.linearVelocity = new B2Vec2(speed, 0.0f);
        bodyDef.isBullet = m_isBullet;
        m_bodyId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.enableSensorEvents = true;
        shapeDef.material.friction = 0.8f;
        shapeDef.material.rollingResistance = 0.01f;
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.25f);
        m_shapeId = b2CreateCircleShape(m_bodyId, ref shapeDef, ref circle);
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 120.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(120.0f, height));

        ImGui.Begin("Sensor Hit", ImGuiWindowFlags.NoResize);

        ImGui.Checkbox("Bullet", ref m_isBullet);

        if (ImGui.Button("Launch") || GetKey(Keys.B) == InputAction.Press)
        {
            Launch();
        }

        ImGui.End();
    }

    void CollectTransforms(B2ShapeId sensorShapeId)
    {
        const int capacity = 5;
        Span<B2ShapeId> visitorIds = stackalloc B2ShapeId[capacity];
        int count = b2Shape_GetSensorData(sensorShapeId, visitorIds, capacity);

        for (int i = 0; i < count && m_transformCount < m_transformCapacity; ++i)
        {
            B2BodyId sensorBodyId = b2Shape_GetBody(sensorShapeId);
            m_transforms[m_transformCount] = b2Body_GetTransform(sensorBodyId);
            m_transformCount += 1;
        }
    }

    public override void Step()
    {
        B2Vec2 p = b2Body_GetPosition(m_kinematicBodyId);
        if (p.X > 1.0f)
        {
            b2Body_SetLinearVelocity(m_kinematicBodyId, new B2Vec2(-0.5f, 0.0f));
        }
        else if (p.X < -1.0f)
        {
            b2Body_SetLinearVelocity(m_kinematicBodyId, new B2Vec2(0.5f, 0.0f));
        }

        float x = b2PrismaticJoint_GetTranslation(m_jointId);
        if (x > 1.0f)
        {
            b2PrismaticJoint_SetMotorSpeed(m_jointId, -0.5f);
        }
        else if (x < -1.0f)
        {
            b2PrismaticJoint_SetMotorSpeed(m_jointId, 0.5f);
        }

        base.Step();


        B2SensorEvents sensorEvents = b2World_GetSensorEvents(m_worldId);
        m_beginCount += sensorEvents.beginCount;
        m_endCount += sensorEvents.endCount;

        for (int i = 0; i < sensorEvents.beginCount; ++i)
        {
            ref readonly B2SensorBeginTouchEvent @event = ref sensorEvents.beginEvents[i];
            if (b2Shape_IsValid(@event.sensorShapeId) == true)
            {
                CollectTransforms(@event.sensorShapeId);
            }
        }
    }

    public override void Draw()
    {
        base.Draw();

        for (int i = 0; i < m_transformCount; ++i)
        {
            DrawTransform(m_draw, m_transforms[i], 1.0f);
        }

        DrawTextLine($"begin touch count = {m_beginCount}");
        DrawTextLine($"end touch count = {m_endCount}");
    }
}