using Box2D.NET.Primitives;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Continuous;

// This shows the importance of secondary collisions in continuous physics.
// This also shows a difficult setup for the solver with an acute angle.
class Wedge : Sample
{
    public:
    explicit Wedge( Settings settings )
        : base( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.0f, 5.5f };
            Draw.g_camera.m_zoom = 6.0f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment = { { -4.0f, 8.0f }, { 0.0f, 0.0f } };
            b2CreateSegmentShape( groundId, &shapeDef, &segment );
            segment = { { 0.0f, 0.0f }, { 0.0f, 8.0 } };
            b2CreateSegmentShape( groundId, &shapeDef, &segment );
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = { -0.45f, 10.75f };
            bodyDef.linearVelocity = { 0.0f, -200.0f };

            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

            b2Circle circle = {};
            circle.radius = 0.3f;
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.2f;
            b2CreateCircleShape( bodyId, &shapeDef, &circle );
        }
    }

    static Sample Create( Settings settings )
    {
        return new Wedge( settings );
    }
};

static int sampleWedge = RegisterSample( "Continuous", "Wedge", Wedge::Create );

