using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;

    class ShapeFilter : Sample
    {
    public:
    enum CollisionBits
    {
        GROUND = 0x00000001,
        TEAM1 = 0x00000002,
        TEAM2 = 0x00000004,
        TEAM3 = 0x00000008,

        ALL_BITS = ( ~0u )
    };

    explicit ShapeFilter( Settings settings )
        : base( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_zoom = 25.0f * 0.5f;
            Draw.g_camera.m_center = { 0.0f, 5.0f };
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );
            b2Segment segment = { { -20.0f, 0.0f }, { 20.0f, 0.0f } };

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter.categoryBits = GROUND;
            shapeDef.filter.maskBits = ALL_BITS;

            b2CreateSegmentShape( groundId, &shapeDef, &segment );
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;

            bodyDef.position = { 0.0f, 2.0f };
            m_player1Id = b2CreateBody( m_worldId, &bodyDef );

            bodyDef.position = { 0.0f, 5.0f };
            m_player2Id = b2CreateBody( m_worldId, &bodyDef );

            bodyDef.position = { 0.0f, 8.0f };
            m_player3Id = b2CreateBody( m_worldId, &bodyDef );

            b2Polygon box = b2MakeBox( 2.0f, 1.0f );

            b2ShapeDef shapeDef = b2DefaultShapeDef();

            shapeDef.filter.categoryBits = TEAM1;
            shapeDef.filter.maskBits = GROUND | TEAM2 | TEAM3;
            m_shape1Id = b2CreatePolygonShape( m_player1Id, &shapeDef, &box );

            shapeDef.filter.categoryBits = TEAM2;
            shapeDef.filter.maskBits = GROUND | TEAM1 | TEAM3;
            m_shape2Id = b2CreatePolygonShape( m_player2Id, &shapeDef, &box );

            shapeDef.filter.categoryBits = TEAM3;
            shapeDef.filter.maskBits = GROUND | TEAM1 | TEAM2;
            m_shape3Id = b2CreatePolygonShape( m_player3Id, &shapeDef, &box );
        }
    }

    public override void UpdateUI()
    {
        float height = 240.0f;
        ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
        ImGui.SetNextWindowSize( new Vector2( 240.0f, height ) );

        ImGui.Begin( "Shape Filter", nullptr, ImGuiWindowFlags.NoResize );

        ImGui.Text( "Player 1 Collides With" );
        {
            b2Filter filter1 = b2Shape_GetFilter( m_shape1Id );
            bool team2 = ( filter1.maskBits & TEAM2 ) == TEAM2;
            if ( ImGui.Checkbox( "Team 2##1", &team2 ) )
            {
                if ( team2 )
                {
                    filter1.maskBits |= TEAM2;
                }
                else
                {
                    filter1.maskBits &= ~TEAM2;
                }

                b2Shape_SetFilter( m_shape1Id, filter1 );
            }

            bool team3 = ( filter1.maskBits & TEAM3 ) == TEAM3;
            if ( ImGui.Checkbox( "Team 3##1", &team3 ) )
            {
                if ( team3 )
                {
                    filter1.maskBits |= TEAM3;
                }
                else
                {
                    filter1.maskBits &= ~TEAM3;
                }

                b2Shape_SetFilter( m_shape1Id, filter1 );
            }
        }

        ImGui.Separator();

        ImGui.Text( "Player 2 Collides With" );
        {
            b2Filter filter2 = b2Shape_GetFilter( m_shape2Id );
            bool team1 = ( filter2.maskBits & TEAM1 ) == TEAM1;
            if ( ImGui.Checkbox( "Team 1##2", &team1 ) )
            {
                if ( team1 )
                {
                    filter2.maskBits |= TEAM1;
                }
                else
                {
                    filter2.maskBits &= ~TEAM1;
                }

                b2Shape_SetFilter( m_shape2Id, filter2 );
            }

            bool team3 = ( filter2.maskBits & TEAM3 ) == TEAM3;
            if ( ImGui.Checkbox( "Team 3##2", &team3 ) )
            {
                if ( team3 )
                {
                    filter2.maskBits |= TEAM3;
                }
                else
                {
                    filter2.maskBits &= ~TEAM3;
                }

                b2Shape_SetFilter( m_shape2Id, filter2 );
            }
        }

        ImGui.Separator();

        ImGui.Text( "Player 3 Collides With" );
        {
            b2Filter filter3 = b2Shape_GetFilter( m_shape3Id );
            bool team1 = ( filter3.maskBits & TEAM1 ) == TEAM1;
            if ( ImGui.Checkbox( "Team 1##3", &team1 ) )
            {
                if ( team1 )
                {
                    filter3.maskBits |= TEAM1;
                }
                else
                {
                    filter3.maskBits &= ~TEAM1;
                }

                b2Shape_SetFilter( m_shape3Id, filter3 );
            }

            bool team2 = ( filter3.maskBits & TEAM2 ) == TEAM2;
            if ( ImGui.Checkbox( "Team 2##3", &team2 ) )
            {
                if ( team2 )
                {
                    filter3.maskBits |= TEAM2;
                }
                else
                {
                    filter3.maskBits &= ~TEAM2;
                }

                b2Shape_SetFilter( m_shape3Id, filter3 );
            }
        }

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        base.Step( settings );

        b2Vec2 p1 = b2Body_GetPosition( m_player1Id );
        Draw.g_draw.DrawString( { p1.x - 0.5f, p1.y }, "player 1" );

        b2Vec2 p2 = b2Body_GetPosition( m_player2Id );
        Draw.g_draw.DrawString( { p2.x - 0.5f, p2.y }, "player 2" );

        b2Vec2 p3 = b2Body_GetPosition( m_player3Id );
        Draw.g_draw.DrawString( { p3.x - 0.5f, p3.y }, "player 3" );
    }

    static Sample Create( Settings settings )
    {
        return new ShapeFilter( settings );
    }

    b2BodyId m_player1Id;
    b2BodyId m_player2Id;
    b2BodyId m_player3Id;

    b2ShapeId m_shape1Id;
    b2ShapeId m_shape2Id;
    b2ShapeId m_shape3Id;
    };

    static int sampleShapeFilter = RegisterSample( "Shapes", "Filter", ShapeFilter::Create );
