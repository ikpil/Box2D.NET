using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;

// Restitution is approximate since Box2D uses speculative collision
    class Restitution : Sample
    {
    public:
    enum
    {
        e_count = 40
    };

    enum ShapeType
    {
        e_circleShape = 0,
        e_boxShape
    };

    explicit Restitution( Settings settings )
        : base( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 4.0f, 17.0f };
            Draw.g_camera.m_zoom = 27.5f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

            float h = 1.0f * e_count;
            b2Segment segment = { { -h, 0.0f }, { h, 0.0f } };
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape( groundId, &shapeDef, &segment );
        }

        for ( int i = 0; i < e_count; ++i )
        {
            m_bodyIds[i] = b2_nullBodyId;
        }

        m_shapeType = e_circleShape;

        CreateBodies();
    }

    void CreateBodies()
    {
        for ( int i = 0; i < e_count; ++i )
        {
            if ( B2_IS_NON_NULL( m_bodyIds[i] ) )
            {
                b2DestroyBody( m_bodyIds[i] );
                m_bodyIds[i] = b2_nullBodyId;
            }
        }

        b2Circle circle = {};
        circle.radius = 0.5f;

        b2Polygon box = b2MakeBox( 0.5f, 0.5f );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.restitution = 0.0f;

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;

        float dr = 1.0f / ( e_count > 1 ? e_count - 1 : 1 );
        float x = -1.0f * ( e_count - 1 );
        float dx = 2.0f;

        for ( int i = 0; i < e_count; ++i )
        {
            bodyDef.position = { x, 40.0f };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

            m_bodyIds[i] = bodyId;

            if ( m_shapeType == e_circleShape )
            {
                b2CreateCircleShape( bodyId, &shapeDef, &circle );
            }
            else
            {
                b2CreatePolygonShape( bodyId, &shapeDef, &box );
            }

            shapeDef.restitution += dr;
            x += dx;
        }
    }

    public override void UpdateUI()
    {
        float height = 100.0f;
        ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
        ImGui.SetNextWindowSize( new Vector2( 240.0f, height ) );

        ImGui.Begin( "Restitution", nullptr, ImGuiWindowFlags.NoResize );

        bool changed = false;
        const char* shapeTypes[] = { "Circle", "Box" };

        int shapeType = int( m_shapeType );
        changed = changed || ImGui.Combo( "Shape", &shapeType, shapeTypes, IM_ARRAYSIZE( shapeTypes ) );
        m_shapeType = ShapeType( shapeType );

        changed = changed || ImGui.Button( "Reset" );

        if ( changed )
        {
            CreateBodies();
        }

        ImGui.End();
    }

    static Sample Create( Settings settings )
    {
        return new Restitution( settings );
    }

    b2BodyId m_bodyIds[e_count];
    ShapeType m_shapeType;
    };

    static int sampleIndex = RegisterSample( "Shapes", "Restitution", Restitution::Create );
