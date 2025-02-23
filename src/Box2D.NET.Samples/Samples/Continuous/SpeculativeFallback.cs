﻿using Box2D.NET.Primitives;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Continuous;

// Speculative collision failure case suggested by Dirk Gregorius. This uses
// a simple fallback scheme to prevent tunneling.
class SpeculativeFallback : Sample
{
    public:
    explicit SpeculativeFallback( Settings settings )
        : base( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 1.0f, 5.0f };
            Draw.g_camera.m_zoom = 25.0f * 0.25f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment = { { -10.0f, 0.0f }, { 10.0f, 0.0f } };
            b2CreateSegmentShape( groundId, &shapeDef, &segment );

            b2Vec2 points[5] = { { -2.0f, 4.0f }, { 2.0f, 4.0f }, { 2.0f, 4.1f }, { -0.5f, 4.2f }, { -2.0f, 4.2f } };
            b2Hull hull = b2ComputeHull( points, 5 );
            b2Polygon poly = b2MakePolygon( &hull, 0.0f );
            b2CreatePolygonShape( groundId, &shapeDef, &poly );
        }

        // Fast moving skinny box. Also testing a large shape offset.
        {
            float offset = 8.0f;
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = { offset, 12.0f };
            bodyDef.linearVelocity = { 0.0f, -100.0f };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Polygon box = b2MakeOffsetBox( 2.0f, 0.05f, { -offset, 0.0f }, b2MakeRot( B2_PI ) );
            b2CreatePolygonShape( bodyId, &shapeDef, &box );
        }
    }

    static Sample Create( Settings settings )
    {
        return new SpeculativeFallback( settings );
    }
};

static int sampleSpeculativeFallback = RegisterSample( "Continuous", "Speculative Fallback", SpeculativeFallback::Create );
