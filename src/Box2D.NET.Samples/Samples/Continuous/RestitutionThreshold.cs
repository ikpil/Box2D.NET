﻿namespace Box2D.NET.Samples.Samples.Continuous;

// This shows that Box2D does not have pixel perfect collision.
class PixelImperfect : Sample
{
public:
explicit PixelImperfect( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 7.0f, 5.0f };
        Draw.g_camera.m_zoom = 6.0f;
    }

    float pixelsPerMeter = 30.f;

    {
        b2BodyDef block4BodyDef = b2DefaultBodyDef();
        block4BodyDef.type = b2BodyType.b2_staticBody;
        block4BodyDef.position = { 175.f / pixelsPerMeter, 150.f / pixelsPerMeter };
        b2BodyId block4BodyId = b2CreateBody( m_worldId, &block4BodyDef );
        b2Polygon block4Shape = b2MakeBox( 20.f / pixelsPerMeter, 10.f / pixelsPerMeter );
        b2ShapeDef block4ShapeDef = b2DefaultShapeDef();
        block4ShapeDef.friction = 0.f;
        b2CreatePolygonShape( block4BodyId, &block4ShapeDef, &block4Shape );
    }

    {
        b2BodyDef ballBodyDef = b2DefaultBodyDef();
        ballBodyDef.type = b2BodyType.b2_dynamicBody;
        ballBodyDef.position = { 200.0f / pixelsPerMeter, 275.f / pixelsPerMeter };
        ballBodyDef.gravityScale = 0.0f;

        m_ballId = b2CreateBody( m_worldId, &ballBodyDef );
        // Ball shape
        //b2Polygon ballShape = b2MakeBox( 5.f / pixelsPerMeter, 5.f / pixelsPerMeter );
        b2Polygon ballShape = b2MakeRoundedBox( 4.0f / pixelsPerMeter, 4.0f / pixelsPerMeter, 0.9f / pixelsPerMeter );
        b2ShapeDef ballShapeDef = b2DefaultShapeDef();
        ballShapeDef.friction = 0.f;
        //ballShapeDef.restitution = 1.f;
        b2CreatePolygonShape( m_ballId, &ballShapeDef, &ballShape );
        b2Body_SetLinearVelocity( m_ballId, { 0.f, -5.0f } );
        b2Body_SetFixedRotation( m_ballId, true );
    }
}

void Step( Settings& settings ) override
{
    b2ContactData data;
    b2Body_GetContactData( m_ballId, &data, 1 );

    b2Vec2 p = b2Body_GetPosition( m_ballId );
    b2Vec2 v = b2Body_GetLinearVelocity( m_ballId );
    Draw.g_draw.DrawString( 5, m_textLine, "p.x = %.9f, v.y = %.9f", p.x, v.y );
    m_textLine += m_textIncrement;

    Sample::Step( settings );
}

static Sample* Create( Settings& settings )
{
    return new PixelImperfect( settings );
}

b2BodyId m_ballId;
};

static int samplePixelImperfect = RegisterSample( "Continuous", "Pixel Imperfect", PixelImperfect::Create );

class RestitutionThreshold : Sample
{
public:
explicit RestitutionThreshold( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 7.0f, 5.0f };
        Draw.g_camera.m_zoom = 6.0f;
    }

    float pixelsPerMeter = 30.f;

    // With the default threshold the ball will not bounce.
    b2World_SetRestitutionThreshold( m_worldId, 0.1f );

    {
        b2BodyDef block0BodyDef = b2DefaultBodyDef();
        block0BodyDef.type = b2BodyType.b2_staticBody;
        block0BodyDef.position = { 205.f / pixelsPerMeter, 120.f / pixelsPerMeter };
        block0BodyDef.rotation = b2MakeRot( 70.f * 3.14f / 180.f );
        b2BodyId block0BodyId = b2CreateBody( m_worldId, &block0BodyDef );
        b2Polygon block0Shape = b2MakeBox( 50.f / pixelsPerMeter, 5.f / pixelsPerMeter );
        b2ShapeDef block0ShapeDef = b2DefaultShapeDef();
        block0ShapeDef.friction = 0.f;
        b2CreatePolygonShape( block0BodyId, &block0ShapeDef, &block0Shape );
    }

    {
        // Make a ball
        b2BodyDef ballBodyDef = b2DefaultBodyDef();
        ballBodyDef.type = b2BodyType.b2_dynamicBody;
        ballBodyDef.position = { 200.f / pixelsPerMeter, 250.f / pixelsPerMeter };
        m_ballId = b2CreateBody( m_worldId, &ballBodyDef );

        b2Circle ballShape = {};
        ballShape.radius = 5.f / pixelsPerMeter;
        b2ShapeDef ballShapeDef = b2DefaultShapeDef();
        ballShapeDef.friction = 0.f;
        ballShapeDef.restitution = 1.f;
        b2CreateCircleShape( m_ballId, &ballShapeDef, &ballShape );

        b2Body_SetLinearVelocity( m_ballId, { 0.f, -2.9f } ); // Initial velocity
        b2Body_SetFixedRotation( m_ballId, true );						// Do not rotate a ball
    }
}

void Step( Settings& settings ) override
{
    b2ContactData data;
    b2Body_GetContactData( m_ballId, &data, 1 );

    b2Vec2 p = b2Body_GetPosition( m_ballId );
    b2Vec2 v = b2Body_GetLinearVelocity( m_ballId );
    Draw.g_draw.DrawString( 5, m_textLine, "p.x = %.9f, v.y = %.9f", p.x, v.y );
    m_textLine += m_textIncrement;

    Sample::Step( settings );
}

static Sample* Create( Settings& settings )
{
    return new RestitutionThreshold( settings );
}

b2BodyId m_ballId;
};

static int sampleRestitutionThreshold = RegisterSample( "Continuous", "Restitution Threshold", RestitutionThreshold::Create );
