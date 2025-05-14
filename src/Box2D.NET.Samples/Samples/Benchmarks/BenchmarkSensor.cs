// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.Shared.RandomSupports;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkSensor : Sample
{
    private static readonly int benchmarkSensor = SampleFactory.Shared.RegisterSample("Benchmark", "Sensor", Create);

    public class ShapeUserData
    {
        public bool shouldDestroyVisitors;
    };

    private const int m_columnCount = 40;
    private const int m_rowCount = 40;
    private int m_maxBeginCount;
    private int m_maxEndCount;
    private ShapeUserData m_passiveSensor = new ShapeUserData();
    private ShapeUserData m_activeSensor = new ShapeUserData();
    private int m_lastStepCount;

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkSensor(context);
    }

    public BenchmarkSensor(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 105.0f);
            m_context.camera.m_zoom = 125.0f;
        }

        m_passiveSensor.shouldDestroyVisitors = false;
        m_activeSensor.shouldDestroyVisitors = true;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

        {
            float gridSize = 3.0f;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.isSensor = true;
            shapeDef.enableSensorEvents = true;
            shapeDef.userData = m_activeSensor;

            float y = 0.0f;
            float x = -40.0f * gridSize;
            for (int i = 0; i < 81; ++i)
            {
                B2Polygon box = b2MakeOffsetBox(0.5f * gridSize, 0.5f * gridSize, new B2Vec2(x, y), b2Rot_identity);
                b2CreatePolygonShape(groundId, ref shapeDef, ref box);
                x += gridSize;
            }
        }

        g_randomSeed = 42;

        {
            float shift = 5.0f;
            float xCenter = 0.5f * shift * m_columnCount;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.isSensor = true;
            shapeDef.enableSensorEvents = true;
            shapeDef.userData = m_passiveSensor;

            float yStart = 10.0f;

            for (int j = 0; j < m_rowCount; ++j)
            {
                float y = j * shift + yStart;
                for (int i = 0; i < m_columnCount; ++i)
                {
                    float x = i * shift - xCenter;
                    float yOffset = RandomFloatRange(-1.0f, 1.0f);
                    B2Polygon box = b2MakeOffsetRoundedBox(0.5f, 0.5f, new B2Vec2(x, y + yOffset), RandomRot(), 0.1f);
                    b2CreatePolygonShape(groundId, ref shapeDef, ref box);
                }
            }
        }

        m_maxBeginCount = 0;
        m_maxEndCount = 0;
        m_lastStepCount = 0;
    }

    void CreateRow(float y)
    {
        float shift = 5.0f;
        float xCenter = 0.5f * shift * m_columnCount;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.gravityScale = 0.0f;
        bodyDef.linearVelocity = new B2Vec2(0.0f, -5.0f);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.enableSensorEvents = true;

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        for (int i = 0; i < m_columnCount; ++i)
        {
            bodyDef.position = new B2Vec2(shift * i - xCenter, y);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }
    }

    public override void Step()
    {
        base.Step();

        if (m_stepCount == m_lastStepCount)
        {
            return;
        }

        HashSet<B2BodyId> zombies = new HashSet<B2BodyId>();

        B2SensorEvents events = b2World_GetSensorEvents(m_worldId);
        for (int i = 0; i < events.beginCount; ++i)
        {
            ref B2SensorBeginTouchEvent @event = ref events.beginEvents[i];

            // shapes on begin touch are always valid
            ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(@event.sensorShapeId);
            if (userData.shouldDestroyVisitors)
            {
                zombies.Add(b2Shape_GetBody(@event.visitorShapeId));
            }
            else
            {
                // Modify color while overlapped with a sensor
                B2SurfaceMaterial surfaceMaterial = b2Shape_GetSurfaceMaterial(@event.visitorShapeId);
                surfaceMaterial.customColor = (uint)B2HexColor.b2_colorLime;
                b2Shape_SetSurfaceMaterial(@event.visitorShapeId, surfaceMaterial);
            }
        }

        for (int i = 0; i < events.endCount; ++i)
        {
            ref B2SensorEndTouchEvent @event = ref events.endEvents[i];

            if (b2Shape_IsValid(@event.visitorShapeId) == false)
            {
                continue;
            }

            // Restore color to default
            B2SurfaceMaterial surfaceMaterial = b2Shape_GetSurfaceMaterial(@event.visitorShapeId);
            surfaceMaterial.customColor = 0;
            b2Shape_SetSurfaceMaterial(@event.visitorShapeId, surfaceMaterial);
        }

        foreach (B2BodyId bodyId in zombies)
        {
            b2DestroyBody(bodyId);
        }

        int delay = 0x1F;

        if ((m_stepCount & delay) == 0)
        {
            CreateRow(10.0f + m_rowCount * 5.0f);
        }

        m_lastStepCount = m_stepCount;

        m_maxBeginCount = b2MaxInt(events.beginCount, m_maxBeginCount);
        m_maxEndCount = b2MaxInt(events.endCount, m_maxEndCount);
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        DrawTextLine($"max begin touch events = {m_maxBeginCount}");
        DrawTextLine($"max end touch events = {m_maxEndCount}");
    }
}