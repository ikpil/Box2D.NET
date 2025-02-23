namespace Box2D.NET.Samples.Samples.Shapes;

// This sample tests a static shape being recreated every step.
class RecreateStatic : Sample
{
    public:
    explicit RecreateStatic( Settings& settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.0f, 2.5f };
            Draw.g_camera.m_zoom = 3.5f;
        }

        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = { 0.0f, 1.0f };
        b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

        b2Polygon box = b2MakeBox( 1.0f, 1.0f );
        b2CreatePolygonShape( bodyId, &shapeDef, &box );

        m_groundId = {};
    }

    void Step( Settings& settings ) override
    {
        if ( B2_IS_NON_NULL( m_groundId ) )
        {
            b2DestroyBody( m_groundId );
            m_groundId = {};
        }

        b2BodyDef bodyDef = b2DefaultBodyDef();
        m_groundId = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        // Invoke contact creation so that contact points are created immediately
        // on a static body.
        shapeDef.invokeContactCreation = true;

        b2Segment segment = { { -10.0f, 0.0f }, { 10.0f, 0.0f } };
        b2CreateSegmentShape( m_groundId, &shapeDef, &segment );

        Sample::Step( settings );
    }

    static Sample* Create( Settings& settings )
    {
        return new RecreateStatic( settings );
    }

    b2BodyId m_groundId;
}

static int sampleSingleBox = RegisterSample( "Shapes", "Recreate Static", RecreateStatic::Create );
