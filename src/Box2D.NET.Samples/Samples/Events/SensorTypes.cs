// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Box2D.NET.Samples.Extensions;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Events;

public class SensorTypes : Sample
{
    public const uint GROUND = 0x00000001;
    public const uint SENSOR = 0x00000002;
    public const uint DEFAULT = 0x00000004;
    public const uint ALL_BITS = (~0u);

    B2ShapeId m_staticSensorId;
    B2ShapeId m_kinematicSensorId;
    B2ShapeId m_dynamicSensorId;

    B2BodyId m_kinematicBodyId;

    List<B2ShapeId> m_overlaps = new List<B2ShapeId>();

    private static readonly int SampleSensorTypes = SampleFactory.Shared.RegisterSample("Events", "Sensor Types", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new SensorTypes(ctx, settings);
    }


    public SensorTypes(SampleAppContext ctx, Settings settings)
        : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.g_camera.m_center = new B2Vec2(0.0f, 3.0f);
            m_context.g_camera.m_zoom = 4.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.name = "ground";

            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter.categoryBits = GROUND;
            shapeDef.filter.maskBits = SENSOR | DEFAULT;

            B2Segment groundSegment = new B2Segment(new B2Vec2(-6.0f, 0.0f), new B2Vec2(6.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref groundSegment);

            groundSegment = new B2Segment(new B2Vec2(-6.0f, 0.0f), new B2Vec2(-6.0f, 4.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref groundSegment);

            groundSegment = new B2Segment(new B2Vec2(6.0f, 0.0f), new B2Vec2(6.0f, 4.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref groundSegment);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.name = "static sensor";
            bodyDef.type = B2BodyType.b2_staticBody;
            bodyDef.position = new B2Vec2(-3.0f, 0.8f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter.categoryBits = SENSOR;
            shapeDef.isSensor = true;
            B2Polygon box = b2MakeSquare(1.0f);
            m_staticSensorId = b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.name = "kinematic sensor";
            bodyDef.type = B2BodyType.b2_kinematicBody;
            bodyDef.position = new B2Vec2(0.0f, 0.0f);
            bodyDef.linearVelocity = new B2Vec2(0.0f, 1.0f);
            m_kinematicBodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter.categoryBits = SENSOR;
            shapeDef.isSensor = true;
            B2Polygon box = b2MakeSquare(1.0f);
            m_kinematicSensorId = b2CreatePolygonShape(m_kinematicBodyId, ref shapeDef, ref box);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.name = "dynamic sensor";
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(3.0f, 1.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter.categoryBits = SENSOR;
            shapeDef.isSensor = true;
            B2Polygon box = b2MakeSquare(1.0f);
            m_dynamicSensorId = b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            // Add some real collision so the dynamic body is valid
            shapeDef.filter.categoryBits = DEFAULT;
            shapeDef.isSensor = false;
            box = b2MakeSquare(0.8f);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.name = "ball_01";
            bodyDef.position = new B2Vec2(-5.0f, 1.0f);
            bodyDef.type = B2BodyType.b2_dynamicBody;

            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter.categoryBits = DEFAULT;
            shapeDef.filter.maskBits = GROUND | DEFAULT | SENSOR;

            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }
    }

    void PrintOverlaps(B2ShapeId sensorShapeId, string prefix)
    {
        // Determine the necessary capacity
        int capacity = b2Shape_GetSensorCapacity(sensorShapeId);
        m_overlaps.Resize(capacity);

        // Get all overlaps and record the actual count
        int count = b2Shape_GetSensorOverlaps(sensorShapeId, CollectionsMarshal.AsSpan(m_overlaps), capacity);
        m_overlaps.Resize(count);

        var builder = new StringBuilder();
        for (int i = 0; i < count; ++i)
        {
            B2ShapeId visitorId = m_overlaps[i];
            if (b2Shape_IsValid(visitorId) == false)
            {
                continue;
            }

            B2BodyId bodyId = b2Shape_GetBody(visitorId);
            string name = b2Body_GetName(bodyId);
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }


            // todo fix this
            builder.Append($"{prefix}: {name}, ");
        }

        DrawTextLine(builder.ToString());
    }

    public override void Step(Settings settings)
    {
        B2Vec2 position = b2Body_GetPosition(m_kinematicBodyId);
        if (position.y < 0.0f)
        {
            b2Body_SetLinearVelocity(m_kinematicBodyId, new B2Vec2(0.0f, 1.0f));
            //b2Body_SetKinematicTarget( m_kinematicBodyId );
        }
        else if (position.y > 3.0f)
        {
            b2Body_SetLinearVelocity(m_kinematicBodyId, new B2Vec2(0.0f, -1.0f));
        }

        base.Step(settings);

        PrintOverlaps(m_staticSensorId, "static");
        PrintOverlaps(m_kinematicSensorId, "kinematic");
        PrintOverlaps(m_dynamicSensorId, "dynamic");

        B2Vec2 origin = new B2Vec2(5.0f, 1.0f);
        B2Vec2 translation = new B2Vec2(-10.0f, 0.0f);
        B2RayResult result = b2World_CastRayClosest(m_worldId, origin, translation, b2DefaultQueryFilter());
        m_context.g_draw.DrawSegment(origin, origin + translation, B2HexColor.b2_colorDimGray);

        if (result.hit)
        {
            m_context.g_draw.DrawPoint(result.point, 10.0f, B2HexColor.b2_colorCyan);
        }
    }
}