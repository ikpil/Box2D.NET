﻿using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Shapes;

// This shows how to use custom filtering
    class CustomFilter : Sample
    {
    public:
    enum
    {
        e_count = 10
    };

    explicit CustomFilter( Settings& settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.0f, 5.0f };
            Draw.g_camera.m_zoom = 10.0f;
        }

        // Register custom filter
        b2World_SetCustomFilterCallback( m_worldId, CustomFilterStatic, this );

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );
            b2Segment segment = { { -40.0f, 0.0f }, { 40.0f, 0.0f } };

            b2ShapeDef shapeDef = b2DefaultShapeDef();

            b2CreateSegmentShape( groundId, &shapeDef, &segment );
        }

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2Polygon box = b2MakeSquare( 1.0f );
        float x = -e_count;

        for ( int i = 0; i < e_count; ++i )
        {
            bodyDef.position = { x, 5.0f };
            m_bodyIds[i] = b2CreateBody( m_worldId, &bodyDef );

            shapeDef.userData = reinterpret_cast<void*>( intptr_t( i + 1 ) );
            m_shapeIds[i] = b2CreatePolygonShape( m_bodyIds[i], &shapeDef, &box );
            x += 2.0f;
        }
    }

    void Step( Settings& settings ) override
    {
        Draw.g_draw.DrawString( 5, m_textLine, "Custom filter disables collision between odd and even shapes" );
        m_textLine += m_textIncrement;

        Sample::Step( settings );

        for ( int i = 0; i < e_count; ++i )
        {
            b2Vec2 p = b2Body_GetPosition( m_bodyIds[i] );
            Draw.g_draw.DrawString( { p.x, p.y }, "%d", i );
        }
    }

    bool ShouldCollide( b2ShapeId shapeIdA, b2ShapeId shapeIdB )
    {
        void* userDataA = b2Shape_GetUserData( shapeIdA );
        void* userDataB = b2Shape_GetUserData( shapeIdB );

        if ( userDataA == NULL || userDataB == NULL )
        {
            return true;
        }

        int indexA = static_cast<int>( reinterpret_cast<intptr_t>( userDataA ) );
        int indexB = static_cast<int>( reinterpret_cast<intptr_t>( userDataB ) );

        return ( ( indexA & 1 ) + ( indexB & 1 ) ) != 1;
    }

    static bool CustomFilterStatic( b2ShapeId shapeIdA, b2ShapeId shapeIdB, void* context )
    {
        CustomFilter* customFilter = static_cast<CustomFilter*>( context );
        return customFilter->ShouldCollide( shapeIdA, shapeIdB );
    }

    static Sample* Create( Settings& settings )
    {
        return new CustomFilter( settings );
    }

    b2BodyId m_bodyIds[e_count];
    b2ShapeId m_shapeIds[e_count];
    };

    static int sampleCustomFilter = RegisterSample( "Shapes", "Custom Filter", CustomFilter::Create );
