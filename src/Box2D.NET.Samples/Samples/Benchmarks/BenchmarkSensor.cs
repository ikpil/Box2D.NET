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
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkSensor : Sample
{
    private static readonly int benchmarkSensor = SampleFactory.Shared.RegisterSample("Benchmark", "Sensor", Create);

    public class ShapeUserData
    {
        public int row;
        public bool active;
    };

    private const int m_columnCount = 40;
    private const int m_rowCount = 40;
    private int m_maxBeginCount;
    private int m_maxEndCount;
    private ShapeUserData[] m_passiveSensors = new ShapeUserData[m_rowCount];
    private ShapeUserData m_activeSensor = new ShapeUserData();
    private int m_lastStepCount;
    private int m_filterRow;

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkSensor(context);
    }

    public BenchmarkSensor(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 105.0f);
            m_camera.zoom = 125.0f;
        }

        b2World_SetCustomFilterCallback(m_worldId, FilterFcn, this);

        m_activeSensor.row = 0;
        m_activeSensor.active = true;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        {
            float gridSize = 3.0f;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.isSensor = true;
            shapeDef.enableSensorEvents = true;
            shapeDef.userData = B2UserData.Ref(m_activeSensor);

            float y = 0.0f;
            float x = -40.0f * gridSize;
            for (int i = 0; i < 81; ++i)
            {
                B2Polygon box = b2MakeOffsetBox(0.5f * gridSize, 0.5f * gridSize, new B2Vec2(x, y), b2Rot_identity);
                b2CreatePolygonShape(groundId, shapeDef, box);
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

            float yStart = 10.0f;
            m_filterRow = m_rowCount >> 1;

            for (int j = 0; j < m_rowCount; ++j)
            {
                m_passiveSensors[j] = new ShapeUserData();
                m_passiveSensors[j].row = j;
                m_passiveSensors[j].active = false;
                shapeDef.userData = B2UserData.Ref(m_passiveSensors[j]);

                if (j == m_filterRow)
                {
                    shapeDef.enableCustomFiltering = true;
                    shapeDef.material.customColor = (uint)B2HexColor.b2_colorFuchsia;
                }
                else
                {
                    shapeDef.enableCustomFiltering = false;
                    shapeDef.material.customColor = 0;
                }

                float y = j * shift + yStart;
                for (int i = 0; i < m_columnCount; ++i)
                {
                    float x = i * shift - xCenter;
                    B2Polygon box = b2MakeOffsetRoundedBox(0.5f, 0.5f, new B2Vec2(x, y), b2Rot_identity, 0.1f);
                    b2CreatePolygonShape(groundId, shapeDef, box);
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
            // stagger bodies to avoid bunching up events into a single update
            float yOffset = RandomFloatRange(-1.0f, 1.0f);
            bodyDef.position = new B2Vec2(shift * i - xCenter, y + yOffset);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            b2CreateCircleShape(bodyId, shapeDef, circle);
        }
    }

    private bool Filter(in B2ShapeId idA, in B2ShapeId idB)
    {
        ShapeUserData userData = null;
        if (b2Shape_IsSensor(idA))
        {
            userData = b2Shape_GetUserData(idA).GetRef<ShapeUserData>();
        }
        else if (b2Shape_IsSensor(idB))
        {
            userData = b2Shape_GetUserData(idB).GetRef<ShapeUserData>();
        }

        if (userData != null)
        {
            return userData.active == true || userData.row != m_filterRow;
        }

        return true;
    }

    private static bool FilterFcn(in B2ShapeId idA, in B2ShapeId idB, object context)
    {
        BenchmarkSensor self = context as BenchmarkSensor;
        return self.Filter(idA, idB);
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
            ShapeUserData userData = b2Shape_GetUserData(@event.sensorShapeId).GetRef<ShapeUserData>();
            if (userData.active)
            {
                zombies.Add(b2Shape_GetBody(@event.visitorShapeId));
            }
            else
            {
                // Check custom filter correctness
                B2_ASSERT(userData.row != m_filterRow);

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

    public override void Draw()
    {
        base.Draw();

        DrawTextLine($"max begin touch events = {m_maxBeginCount}");
        DrawTextLine($"max end touch events = {m_maxEndCount}");
    }
}