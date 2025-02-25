// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Events;

// Shows how to make a rigid body character mover and use the pre-solve callback. In this
// case the platform should get the pre-solve event, not the player.
public class Platformer : Sample
{
    
    bool m_jumping;
    float m_radius;
    float m_force;
    float m_impulse;
    float m_jumpDelay;
    b2BodyId m_playerId;
    b2ShapeId m_playerShapeId;
    b2BodyId m_movingPlatformId;

    static int samplePlatformer = RegisterSample( "Events", "Platformer", Create );
    static Sample Create( Settings settings )
    {
        return new Platformer( settings );
    }

public Platformer( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.5f, 7.5f };
        Draw.g_camera.m_zoom = 25.0f * 0.4f;
    }

    b2World_SetPreSolveCallback( m_worldId, PreSolveStatic, this );

    // Ground
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Segment segment = { { -20.0f, 0.0f }, { 20.0f, 0.0f } };
        b2CreateSegmentShape( groundId, &shapeDef, &segment );
    }

    // Static Platform
    // This tests pre-solve with continuous collision
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_staticBody;
        bodyDef.position = { -6.0f, 6.0f };
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        // Need to turn this on to get the callback
        shapeDef.enablePreSolveEvents = true;

        b2Polygon box = b2MakeBox( 2.0f, 0.5f );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );
    }

    // Moving Platform
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_kinematicBody;
        bodyDef.position = { 0.0f, 6.0f };
        bodyDef.linearVelocity = { 2.0f, 0.0f };
        m_movingPlatformId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        // Need to turn this on to get the callback
        shapeDef.enablePreSolveEvents = true;

        b2Polygon box = b2MakeBox( 3.0f, 0.5f );
        b2CreatePolygonShape( m_movingPlatformId, &shapeDef, &box );
    }

    // Player
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.fixedRotation = true;
        bodyDef.linearDamping = 0.5f;
        bodyDef.position = { 0.0f, 1.0f };
        m_playerId = b2CreateBody( m_worldId, &bodyDef );

        m_radius = 0.5f;
        b2Capsule capsule = { { 0.0f, 0.0f }, { 0.0f, 1.0f }, m_radius };
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.friction = 0.1f;

        m_playerShapeId = b2CreateCapsuleShape( m_playerId, &shapeDef, &capsule );
    }

    m_force = 25.0f;
    m_impulse = 25.0f;
    m_jumpDelay = 0.25f;
    m_jumping = false;
}

static bool PreSolveStatic( b2ShapeId shapeIdA, b2ShapeId shapeIdB, b2Manifold* manifold, void* context )
{
    Platformer* platformer = static_cast<Platformer*>( context );
    return platformer->PreSolve( shapeIdA, shapeIdB, manifold );
}

// This callback must be thread-safe. It may be called multiple times simultaneously.
// Notice how this method is constant and doesn't change any data. It also
// does not try to access any values in the world that may be changing, such as contact data.
bool PreSolve( b2ShapeId shapeIdA, b2ShapeId shapeIdB, b2Manifold* manifold ) const
{
    Debug.Assert( b2Shape_IsValid( shapeIdA ) );
    Debug.Assert( b2Shape_IsValid( shapeIdB ) );

    float sign = 0.0f;
    if ( B2_ID_EQUALS( shapeIdA, m_playerShapeId ) )
    {
        sign = -1.0f;
    }
    else if ( B2_ID_EQUALS( shapeIdB, m_playerShapeId ) )
    {
        sign = 1.0f;
    }
    else
    {
        // not colliding with the player, enable contact
        return true;
    }

    b2Vec2 normal = manifold->normal;
    if ( sign * normal.y > 0.95f )
    {
        return true;
    }

    float separation = 0.0f;
    for ( int i = 0; i < manifold->pointCount; ++i )
    {
        float s = manifold->points[i].separation;
        separation = separation < s ? separation : s;
    }

    if ( separation > 0.1f * m_radius )
    {
        // shallow overlap
        return true;
    }

    // normal points down, disable contact
    return false;
}

public override void UpdateUI()
{
    float height = 100.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 240.0f, height ) );

    ImGui.Begin( "One-Sided Platform", nullptr, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize );

    ImGui.SliderFloat( "force", &m_force, 0.0f, 50.0f, "%.1f" );
    ImGui.SliderFloat( "impulse", &m_impulse, 0.0f, 50.0f, "%.1f" );

    ImGui.End();
}

public override void Step(Settings settings)
{
    bool canJump = false;
    b2Vec2 velocity = b2Body_GetLinearVelocity( m_playerId );
    if ( m_jumpDelay == 0.0f && m_jumping == false && velocity.y < 0.01f )
    {
        int capacity = b2Body_GetContactCapacity( m_playerId );
        capacity = b2MinInt( capacity, 4 );
        b2ContactData contactData[4];
        int count = b2Body_GetContactData( m_playerId, contactData, capacity );
        for ( int i = 0; i < count; ++i )
        {
            b2BodyId bodyIdA = b2Shape_GetBody( contactData[i].shapeIdA );
            float sign = 0.0f;
            if ( B2_ID_EQUALS( bodyIdA, m_playerId ) )
            {
                // normal points from A to B
                sign = -1.0f;
            }
            else
            {
                sign = 1.0f;
            }

            if ( sign * contactData[i].manifold.normal.y > 0.9f )
            {
                canJump = true;
                break;
            }
        }
    }

    // A kinematic body is moved by setting its velocity. This
    // ensure friction works correctly.
    b2Vec2 platformPosition = b2Body_GetPosition( m_movingPlatformId );
    if ( platformPosition.x < -15.0f )
    {
        b2Body_SetLinearVelocity( m_movingPlatformId, { 2.0f, 0.0f } );
    }
    else if ( platformPosition.x > 15.0f )
    {
        b2Body_SetLinearVelocity( m_movingPlatformId, { -2.0f, 0.0f } );
    }

    if ( glfwGetKey( g_mainWindow, GLFW_KEY_A ) == GLFW_PRESS )
    {
        b2Body_ApplyForceToCenter( m_playerId, { -m_force, 0.0f }, true );
    }

    if ( glfwGetKey( g_mainWindow, GLFW_KEY_D ) == GLFW_PRESS )
    {
        b2Body_ApplyForceToCenter( m_playerId, { m_force, 0.0f }, true );
    }

    int keyState = glfwGetKey( g_mainWindow, GLFW_KEY_SPACE );
    if ( keyState == GLFW_PRESS )
    {
        if ( canJump )
        {
            b2Body_ApplyLinearImpulseToCenter( m_playerId, { 0.0f, m_impulse }, true );
            m_jumpDelay = 0.5f;
            m_jumping = true;
        }
    }
    else
    {
        m_jumping = false;
    }

    base.Step( settings );

    b2ContactData contactData = {};
    int contactCount = b2Body_GetContactData( m_movingPlatformId, &contactData, 1 );
    Draw.g_draw.DrawString( 5, m_textLine, "Platform contact count = %d, point count = %d", contactCount,
        contactData.manifold.pointCount );
    m_textLine += m_textIncrement;

    Draw.g_draw.DrawString( 5, m_textLine, "Movement: A/D/Space" );
    m_textLine += m_textIncrement;

    Draw.g_draw.DrawString( 5, m_textLine, "Can jump = %s", canJump ? "true" : "false" );
    m_textLine += m_textIncrement;

    if ( settings.hertz > 0.0f )
    {
        m_jumpDelay = b2MaxFloat( 0.0f, m_jumpDelay - 1.0f / settings.hertz );
    }
}



}


