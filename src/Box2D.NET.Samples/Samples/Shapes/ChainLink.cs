namespace Box2D.NET.Samples.Samples.Shapes;

// Shows how to link to chain shapes together. This is a useful technique for building large game levels with smooth collision.
    class ChainLink : Sample
    {
    public:
    explicit ChainLink( Settings& settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.0f, 5.0f };
            Draw.g_camera.m_zoom = 25.0f * 0.5f;
        }

        b2Vec2 points1[] = { { 40.0f, 1.0f },	{ 0.0f, 0.0f },	 { -40.0f, 0.0f },
            { -40.0f, -1.0f }, { 0.0f, -1.0f }, { 40.0f, -1.0f } };
        b2Vec2 points2[] = { { -40.0f, -1.0f }, { 0.0f, -1.0f }, { 40.0f, -1.0f },
            { 40.0f, 0.0f },	{ 0.0f, 0.0f },	 { -40.0f, 0.0f } };

        int count1 = std::size( points1 );
        int count2 = std::size( points2 );

        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

        {
            b2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points1;
            chainDef.count = count1;
            chainDef.isLoop = false;
            b2CreateChain( groundId, &chainDef );
        }

        {
            b2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points2;
            chainDef.count = count2;
            chainDef.isLoop = false;
            b2CreateChain( groundId, &chainDef );
        }

        bodyDef.type = b2BodyType.b2_dynamicBody;
        b2ShapeDef shapeDef = b2DefaultShapeDef();

        {
            bodyDef.position = { -5.0f, 2.0f };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2Circle circle = { { 0.0f, 0.0f }, 0.5f };
            b2CreateCircleShape( bodyId, &shapeDef, &circle );
        }

        {
            bodyDef.position = { 0.0f, 2.0f };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2Capsule capsule = { { -0.5f, 0.0f }, { 0.5f, 0.0 }, 0.25f };
            b2CreateCapsuleShape( bodyId, &shapeDef, &capsule );
        }

        {
            bodyDef.position = { 5.0f, 2.0f };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            float h = 0.5f;
            b2Polygon box = b2MakeBox( h, h );
            b2CreatePolygonShape( bodyId, &shapeDef, &box );
        }
    }

    void Step( Settings& settings ) override
    {
        Sample::Step( settings );

        Draw.g_draw.DrawString( 5, m_textLine, "This shows how to link together two chain shapes" );
        m_textLine += m_textIncrement;
    }

    static Sample* Create( Settings& settings )
    {
        return new ChainLink( settings );
    }
    };

    static int sampleChainLink = RegisterSample( "Shapes", "Chain Link", ChainLink::Create );
