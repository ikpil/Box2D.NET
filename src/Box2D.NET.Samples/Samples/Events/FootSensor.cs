// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Box2D.NET.Samples.Extensions;
using Silk.NET.GLFW;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples.Events;

public class FootSensor : Sample
{
    private static readonly int SampleFootSensor = SampleFactory.Shared.RegisterSample("Events", "Foot Sensor", Create);

    public const uint GROUND = 0x00000001;
    public const uint PLAYER = 0x00000002;
    public const uint FOOT = 0x00000004;
    public const uint ALL_BITS = (~0u);


    private B2BodyId m_playerId;
    private B2ShapeId m_sensorId;
    private List<B2ShapeId> m_visitorIds = new List<B2ShapeId>();
    private int m_overlapCount;

    private static Sample Create(SampleContext context)
    {
        return new FootSensor(context);
    }


    public FootSensor(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 6.0f);
            m_camera.m_zoom = 7.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Vec2[] points = new B2Vec2[20];
            float x = 10.0f;
            for (int i = 0; i < 20; ++i)
            {
                points[i] = new B2Vec2(x, 0.0f);
                x -= 1.0f;
            }

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = 20;
            chainDef.filter.categoryBits = GROUND;
            chainDef.filter.maskBits = FOOT | PLAYER;
            chainDef.isLoop = false;
            chainDef.enableSensorEvents = true;

            b2CreateChain(groundId, ref chainDef);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.motionLocks.angularZ = true;
            bodyDef.position = new B2Vec2(0.0f, 1.0f);
            m_playerId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter.categoryBits = PLAYER;
            shapeDef.filter.maskBits = GROUND;
            shapeDef.material.friction = 0.3f;
            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.5f), new B2Vec2(0.0f, 0.5f), 0.5f);
            b2CreateCapsuleShape(m_playerId, ref shapeDef, ref capsule);

            B2Polygon box = b2MakeOffsetBox(0.5f, 0.25f, new B2Vec2(0.0f, -1.0f), b2Rot_identity);
            shapeDef.filter.categoryBits = FOOT;
            shapeDef.filter.maskBits = GROUND;
            shapeDef.isSensor = true;
            shapeDef.enableSensorEvents = true;
            m_sensorId = b2CreatePolygonShape(m_playerId, ref shapeDef, ref box);
        }

        m_overlapCount = 0;
    }

    public override void Step()
    {
        if (GetKey(Keys.A) == InputAction.Press)
        {
            b2Body_ApplyForceToCenter(m_playerId, new B2Vec2(-50.0f, 0.0f), true);
        }

        if (GetKey(Keys.D) == InputAction.Press)
        {
            b2Body_ApplyForceToCenter(m_playerId, new B2Vec2(50.0f, 0.0f), true);
        }

        base.Step();

        B2SensorEvents sensorEvents = b2World_GetSensorEvents(m_worldId);
        for (int i = 0; i < sensorEvents.beginCount; ++i)
        {
            ref B2SensorBeginTouchEvent @event = ref sensorEvents.beginEvents[i];

            B2_ASSERT(B2_ID_EQUALS(@event.visitorShapeId, m_sensorId) == false);

            if (B2_ID_EQUALS(@event.sensorShapeId, m_sensorId))
            {
                m_overlapCount += 1;
            }
        }

        for (int i = 0; i < sensorEvents.endCount; ++i)
        {
            ref B2SensorEndTouchEvent @event = ref sensorEvents.endEvents[i];

            B2_ASSERT(B2_ID_EQUALS(@event.visitorShapeId, m_sensorId) == false);

            if (B2_ID_EQUALS(@event.sensorShapeId, m_sensorId))
            {
                m_overlapCount -= 1;
            }
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        int capacity = b2Shape_GetSensorCapacity(m_sensorId);
        m_visitorIds.Clear();
        m_visitorIds.Resize(capacity);

        int count = b2Shape_GetSensorData(m_sensorId, CollectionsMarshal.AsSpan(m_visitorIds), capacity);
        for (int i = 0; i < count; ++i)
        {
            B2ShapeId shapeId = m_visitorIds[i];
            B2AABB aabb = b2Shape_GetAABB(shapeId);
            B2Vec2 point = b2AABB_Center(aabb);
            m_draw.DrawPoint(point, 10.0f, B2HexColor.b2_colorWhite);
        }

        DrawTextLine($"count == {m_overlapCount}");
        
    }
}