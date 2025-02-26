// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Events;

public class SensorTypes : Sample
{
enum CollisionBits
{
    GROUND = 0x00000001,
    SENSOR = 0x00000002,
    DEFAULT = 0x00000004,

    ALL_BITS = ( ~0u )
};

B2ShapeId m_staticSensorId;
B2ShapeId m_kinematicSensorId;
B2ShapeId m_dynamicSensorId;

B2BodyId m_kinematicBodyId;

List<B2ShapeId> m_overlaps;

static int sampleSensorTypes = RegisterSample( "Events", "Sensor Types", Create );
static Sample Create( Settings settings )
{
    return new SensorTypes( settings );
}


public SensorTypes( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 3.0f };
        Draw.g_camera.m_zoom = 4.5f;
    }

    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.name = "ground";

        B2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.filter.categoryBits = GROUND;
        shapeDef.filter.maskBits = SENSOR | DEFAULT;

        B2Segment groundSegment = { { -6.0f, 0.0f }, { 6.0f, 0.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &groundSegment );

        groundSegment = { { -6.0f, 0.0f }, { -6.0f, 4.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &groundSegment );

        groundSegment = { { 6.0f, 0.0f }, { 6.0f, 4.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &groundSegment );
    }

    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.name = "static sensor";
        bodyDef.type = B2BodyType.b2_staticBody;
        bodyDef.position = { -3.0f, 0.8f };
        B2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.filter.categoryBits = SENSOR;
        shapeDef.isSensor = true;
        B2Polygon box = b2MakeSquare( 1.0f );
        m_staticSensorId = b2CreatePolygonShape( bodyId, &shapeDef, &box );
    }

    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.name = "kinematic sensor";
        bodyDef.type = B2BodyType.b2_kinematicBody;
        bodyDef.position = { 0.0f, 0.0f };
        bodyDef.linearVelocity = { 0.0f, 1.0f };
        m_kinematicBodyId = b2CreateBody( m_worldId, &bodyDef );

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.filter.categoryBits = SENSOR;
        shapeDef.isSensor = true;
        B2Polygon box = b2MakeSquare( 1.0f );
        m_kinematicSensorId = b2CreatePolygonShape( m_kinematicBodyId, &shapeDef, &box );
    }

    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.name = "dynamic sensor";
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = { 3.0f, 1.0f };
        B2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.filter.categoryBits = SENSOR;
        shapeDef.isSensor = true;
        B2Polygon box = b2MakeSquare( 1.0f );
        m_dynamicSensorId = b2CreatePolygonShape( bodyId, &shapeDef, &box );

        // Add some real collision so the dynamic body is valid
        shapeDef.filter.categoryBits = DEFAULT;
        shapeDef.isSensor = false;
        box = b2MakeSquare( 0.8f );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );
    }

    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.name = "ball_01";
        bodyDef.position = { -5.0f, 1.0f };
        bodyDef.type = B2BodyType.b2_dynamicBody;

        B2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.filter.categoryBits = DEFAULT;
        shapeDef.filter.maskBits = GROUND | DEFAULT | SENSOR;

        B2Circle circle = { { 0.0f, 0.0f }, 0.5f };
        b2CreateCircleShape( bodyId, &shapeDef, &circle );
    }
}

void PrintOverlaps( B2ShapeId sensorShapeId, string prefix )
{
    char buffer[256] = {};

    // Determine the necessary capacity
    int capacity = b2Shape_GetSensorCapacity( sensorShapeId );
    m_overlaps.resize( capacity );

    // Get all overlaps and record the actual count
    int count = b2Shape_GetSensorOverlaps( sensorShapeId, m_overlaps.data(), capacity );
    m_overlaps.resize( count );

    int start = snprintf( buffer, sizeof( buffer ), "%s: ", prefix );
    for ( int i = 0; i < count && start < sizeof( buffer ); ++i )
    {
        B2ShapeId visitorId = m_overlaps[i];
        if ( b2Shape_IsValid( visitorId ) == false )
        {
            continue;
        }

        B2BodyId bodyId = b2Shape_GetBody( visitorId );
        string name = b2Body_GetName( bodyId );
        if ( name == nullptr )
        {
            continue;
        }

        // todo fix this
        start += snprintf( buffer + start, sizeof( buffer ) - start, "%s, ", name );
    }

    DrawTextLine( buffer );
}

public override void Step(Settings settings)
{
    B2Vec2 position = b2Body_GetPosition( m_kinematicBodyId );
    if (position.y < 0.0f)
    {
        b2Body_SetLinearVelocity( m_kinematicBodyId, { 0.0f, 1.0f } );
        //b2Body_SetKinematicTarget( m_kinematicBodyId );
    }
    else if (position.y > 3.0f)
    {
        b2Body_SetLinearVelocity( m_kinematicBodyId, { 0.0f, -1.0f } );
    }

    base.Step( settings );

    PrintOverlaps( m_staticSensorId, "static" );
    PrintOverlaps( m_kinematicSensorId, "kinematic" );
    PrintOverlaps( m_dynamicSensorId, "dynamic" );

    B2Vec2 origin = { 5.0f, 1.0f };
    B2Vec2 translation = { -10.0f, 0.0f };
    B2RayResult result = b2World_CastRayClosest( m_worldId, origin, translation, b2DefaultQueryFilter() );
    Draw.g_draw.DrawSegment( origin, origin + translation, B2HexColor.b2_colorDimGray );

    if (result.hit)
    {
        Draw.g_draw.DrawPoint( result.point, 10.0f, B2HexColor.b2_colorCyan );
    }
}



}

