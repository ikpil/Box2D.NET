using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;


    class Friction : Sample
    {
    public:
    explicit Friction( Settings& settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.0f, 14.0f };
            Draw.g_camera.m_zoom = 25.0f * 0.6f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.2f;

            b2Segment segment = { { -40.0f, 0.0f }, { 40.0f, 0.0f } };
            b2CreateSegmentShape( groundId, &shapeDef, &segment );

            b2Polygon box = b2MakeOffsetBox( 13.0f, 0.25f, { -4.0f, 22.0f }, b2MakeRot( -0.25f ) );
            b2CreatePolygonShape( groundId, &shapeDef, &box );

            box = b2MakeOffsetBox( 0.25f, 1.0f, { 10.5f, 19.0f }, b2Rot_identity );
            b2CreatePolygonShape( groundId, &shapeDef, &box );

            box = b2MakeOffsetBox( 13.0f, 0.25f, { 4.0f, 14.0f }, b2MakeRot( 0.25f ) );
            b2CreatePolygonShape( groundId, &shapeDef, &box );

            box = b2MakeOffsetBox( 0.25f, 1.0f, { -10.5f, 11.0f }, b2Rot_identity );
            b2CreatePolygonShape( groundId, &shapeDef, &box );

            box = b2MakeOffsetBox( 13.0f, 0.25f, { -4.0f, 6.0f }, b2MakeRot( -0.25f ) );
            b2CreatePolygonShape( groundId, &shapeDef, &box );
        }

        {
            b2Polygon box = b2MakeBox( 0.5f, 0.5f );

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 25.0f;

            float friction[5] = { 0.75f, 0.5f, 0.35f, 0.1f, 0.0f };

            for ( int i = 0; i < 5; ++i )
            {
                b2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = b2BodyType.b2_dynamicBody;
                bodyDef.position = { -15.0f + 4.0f * i, 28.0f };
                b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

                shapeDef.friction = friction[i];
                b2CreatePolygonShape( bodyId, &shapeDef, &box );
            }
        }
    }

    static Sample* Create( Settings& settings )
    {
        return new Friction( settings );
    }
    };

    static int sampleFriction = RegisterSample( "Shapes", "Friction", Friction::Create );
