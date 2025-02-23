using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;

class OffsetShapes : Sample
{
    public:
    explicit OffsetShapes( Settings settings )
        : base( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_zoom = 25.0f * 0.55f;
            Draw.g_camera.m_center = { 2.0f, 8.0f };
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = { -1.0f, 1.0f };
            b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Polygon box = b2MakeOffsetBox( 1.0f, 1.0f, { 10.0f, -2.0f }, b2MakeRot( 0.5f * B2_PI ) );
            b2CreatePolygonShape( groundId, &shapeDef, &box );
        }

        {
            b2Capsule capsule = { { -5.0f, 1.0f }, { -4.0f, 1.0f }, 0.25f };
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = { 13.5f, -0.75f };
            bodyDef.type = b2BodyType.b2_dynamicBody;
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateCapsuleShape( bodyId, &shapeDef, &capsule );
        }

        {
            b2Polygon box = b2MakeOffsetBox( 0.75f, 0.5f, { 9.0f, 2.0f }, b2MakeRot( 0.5f * B2_PI ) );
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = { 0.0f, 0.0f };
            bodyDef.type = b2BodyType.b2_dynamicBody;
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape( bodyId, &shapeDef, &box );
        }
    }

    public override void Step(Settings settings)
    {
        base.Step( settings );

        Draw.g_draw.DrawTransform( b2Transform_identity );
    }

    static Sample Create( Settings settings )
    {
        return new OffsetShapes( settings );
    }
};

static int sampleOffsetShapes = RegisterSample( "Shapes", "Offset", OffsetShapes::Create );
