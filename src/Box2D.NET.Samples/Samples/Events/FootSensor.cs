// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Events;

public class FootSensor : Sample
{
enum CollisionBits
{
    GROUND = 0x00000001,
    PLAYER = 0x00000002,
    FOOT = 0x00000004,

    ALL_BITS = ( ~0u )
};

B2BodyId m_playerId;
B2ShapeId m_sensorId;
List<B2ShapeId> m_overlaps;
int m_overlapCount;
static int sampleCharacterSensor = RegisterSample( "Events", "Foot Sensor", Create );
static Sample Create( Settings settings )
{
    return new FootSensor( settings );
}


public FootSensor( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 6.0f };
        Draw.g_camera.m_zoom = 7.5f;
    }

    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

        B2Vec2 points[20];
        float x = 10.0f;
        for ( int i = 0; i < 20; ++i )
        {
            points[i] = { x, 0.0f };
            x -= 1.0f;
        }

        B2ChainDef chainDef = b2DefaultChainDef();
        chainDef.points = points;
        chainDef.count = 20;
        chainDef.filter.categoryBits = GROUND;
        chainDef.filter.maskBits = FOOT | PLAYER;
        chainDef.isLoop = false;

        b2CreateChain( groundId, &chainDef );
    }

    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.fixedRotation = true;
        bodyDef.position = { 0.0f, 1.0f };
        m_playerId = b2CreateBody( m_worldId, &bodyDef );
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.filter.categoryBits = PLAYER;
        shapeDef.filter.maskBits = GROUND;
        shapeDef.friction = 0.3f;
        B2Capsule capsule = { { 0.0f, -0.5f }, { 0.0f, 0.5f }, 0.5f };
        b2CreateCapsuleShape( m_playerId, &shapeDef, &capsule );

        B2Polygon box = b2MakeOffsetBox( 0.5f, 0.25f, { 0.0f, -1.0f }, b2Rot_identity );
        shapeDef.filter.categoryBits = FOOT;
        shapeDef.filter.maskBits = GROUND;
        shapeDef.isSensor = true;
        m_sensorId = b2CreatePolygonShape( m_playerId, &shapeDef, &box );
    }

    m_overlapCount = 0;
}

public override void Step(Settings settings)
{
    if ( glfwGetKey( g_mainWindow, GLFW_KEY_A ) == GLFW_PRESS )
    {
        b2Body_ApplyForceToCenter( m_playerId, { -50.0f, 0.0f }, true );
    }

    if ( glfwGetKey( g_mainWindow, GLFW_KEY_D ) == GLFW_PRESS )
    {
        b2Body_ApplyForceToCenter( m_playerId, { 50.0f, 0.0f }, true );
    }

    base.Step( settings );

    B2SensorEvents sensorEvents = b2World_GetSensorEvents( m_worldId );
    for ( int i = 0; i < sensorEvents.beginCount; ++i )
    {
        B2SensorBeginTouchEvent event = sensorEvents.beginEvents[i];

        Debug.Assert( B2_ID_EQUALS( event.visitorShapeId, m_sensorId ) == false );

        if ( B2_ID_EQUALS( event.sensorShapeId, m_sensorId ) )
        {
            m_overlapCount += 1;
        }
    }

    for ( int i = 0; i < sensorEvents.endCount; ++i )
    {
        B2SensorEndTouchEvent event = sensorEvents.endEvents[i];

        Debug.Assert( B2_ID_EQUALS( event.visitorShapeId, m_sensorId ) == false );

        if ( B2_ID_EQUALS( event.sensorShapeId, m_sensorId ) )
        {
            m_overlapCount -= 1;
        }
    }

    Draw.g_draw.DrawString( 5, m_textLine, "count == %d", m_overlapCount );
    m_textLine += m_textIncrement;

    int capacity = b2Shape_GetSensorCapacity( m_sensorId );
    m_overlaps.clear();
    m_overlaps.resize( capacity );
    int count = b2Shape_GetSensorOverlaps( m_sensorId, m_overlaps.data(), capacity );
    for ( int i = 0; i < count; ++i )
    {
        B2ShapeId shapeId = m_overlaps[i];
        B2AABB aabb = b2Shape_GetAABB( shapeId );
        B2Vec2 point = b2AABB_Center( aabb );
        Draw.g_draw.DrawPoint( point, 10.0f, B2HexColor.b2_colorWhite );
    }
}



}


