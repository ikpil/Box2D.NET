namespace Box2D.NET.Samples.Samples.Shapes;

    class TangentSpeed : Sample
    {
    public:
    explicit TangentSpeed( Settings& settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 60.0f, -15.0f };
            Draw.g_camera.m_zoom = 38.0f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

            //const char* path = "M 613.8334,185.20833 H 500.06255 L 470.95838,182.5625 444.50004,174.625 418.04171,161.39583 "
            //				   "394.2292,140.22917 h "
            //				   "-13.22916 v 44.97916 H 68.791712 V 0 h -21.16671 v 206.375 l 566.208398,-1e-5 z";

            const char* path = "m 613.8334,185.20833 -42.33338,0 h -37.04166 l -34.39581,0 -29.10417,-2.64583 -26.45834,-7.9375 "
            "-26.45833,-13.22917 -23.81251,-21.16666 h -13.22916 v 44.97916 H 68.791712 V 0 h -21.16671 v "
            "206.375 l 566.208398,-1e-5 z";

            b2Vec2 offset = { -47.375002f, 0.25f };

            float scale = 0.2f;
            b2Vec2 points[20] = {};
            int count = ParsePath( path, offset, points, 20, scale, true );

            b2SurfaceMaterial materials[20] = {};
            for ( int i = 0; i < 20; ++i )
            {
                materials[i].friction = 0.6f;
            }

            materials[0].tangentSpeed = -10.0;
            materials[0].customColor = b2_colorDarkBlue;
            materials[1].tangentSpeed = -20.0;
            materials[1].customColor = b2_colorDarkCyan;
            materials[2].tangentSpeed = -30.0;
            materials[2].customColor = b2_colorDarkGoldenRod;
            materials[3].tangentSpeed = -40.0;
            materials[3].customColor = b2_colorDarkGray;
            materials[4].tangentSpeed = -50.0;
            materials[4].customColor = b2_colorDarkGreen;
            materials[5].tangentSpeed = -60.0;
            materials[5].customColor = b2_colorDarkKhaki;
            materials[6].tangentSpeed = -70.0;
            materials[6].customColor = b2_colorDarkMagenta;

            b2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = count;
            chainDef.isLoop = true;
            chainDef.materials = materials;
            chainDef.materialCount = count;

            b2CreateChain( groundId, &chainDef );
        }
    }

    b2BodyId DropBall()
    {
        b2Circle circle = { { 0.0f, 0.0f }, 0.5f };

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { 110.0f, -30.0f };
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.rollingResistance = 0.3f;
        b2CreateCircleShape( bodyId, &shapeDef, &circle );
        return bodyId;
    }

    void Step( Settings& settings ) override
    {
        if ( m_stepCount % 25 == 0 && m_count < m_totalCount && settings.pause == false)
        {
            DropBall();
            m_count += 1;
        }

        Sample::Step( settings );

    }

    static Sample* Create( Settings& settings )
    {
        return new TangentSpeed( settings );
    }

    static constexpr int m_totalCount = 200;
    int m_count = 0;
    };

    static int sampleTangentSpeed = RegisterSample( "Shapes", "Tangent Speed", TangentSpeed::Create );
