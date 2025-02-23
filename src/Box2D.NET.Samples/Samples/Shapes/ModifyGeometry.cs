using System.Diagnostics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;

// This sample shows how to modify the geometry on an existing shape. This is only supported on
// dynamic and kinematic shapes because static shapes don't look for new collisions.
    class ModifyGeometry : Sample
    {
    public:
    explicit ModifyGeometry( Settings& settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_zoom = 25.0f * 0.25f;
            Draw.g_camera.m_center = { 0.0f, 5.0f };
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Polygon box = b2MakeOffsetBox( 10.0f, 1.0f, { 0.0f, -1.0f }, b2Rot_identity );
            b2CreatePolygonShape( groundId, &shapeDef, &box );
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = { 0.0f, 4.0f };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Polygon box = b2MakeBox( 1.0f, 1.0f );
            b2CreatePolygonShape( bodyId, &shapeDef, &box );
        }

        {
            m_shapeType = b2_circleShape;
            m_scale = 1.0f;
            m_circle = { { 0.0f, 0.0f }, 0.5f };
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_kinematicBody;
            bodyDef.position = { 0.0f, 1.0f };
            b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            m_shapeId = b2CreateCircleShape( bodyId, &shapeDef, &m_circle );
        }
    }

    void UpdateShape()
    {
        switch ( m_shapeType )
        {
            case b2_circleShape:
                m_circle = { { 0.0f, 0.0f }, 0.5f * m_scale };
                b2Shape_SetCircle( m_shapeId, &m_circle );
                break;

            case b2_capsuleShape:
                m_capsule = { { -0.5f * m_scale, 0.0f }, { 0.0f, 0.5f * m_scale }, 0.5f * m_scale };
                b2Shape_SetCapsule( m_shapeId, &m_capsule );
                break;

            case b2_segmentShape:
                m_segment = { { -0.5f * m_scale, 0.0f }, { 0.75f * m_scale, 0.0f } };
                b2Shape_SetSegment( m_shapeId, &m_segment );
                break;

            case b2_polygonShape:
                m_polygon = b2MakeBox( 0.5f * m_scale, 0.75f * m_scale );
                b2Shape_SetPolygon( m_shapeId, &m_polygon );
                break;

            default:
                Debug.Assert( false );
                break;
        }

        b2BodyId bodyId = b2Shape_GetBody( m_shapeId );
        b2Body_ApplyMassFromShapes( bodyId );
    }

    void UpdateUI() override
    {
        float height = 230.0f;
        ImGui.SetNextWindowPos( ImVec2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
        ImGui.SetNextWindowSize( ImVec2( 200.0f, height ) );

        ImGui.Begin( "Modify Geometry", nullptr, ImGuiWindowFlags.NoResize );

        if ( ImGui.RadioButton( "Circle", m_shapeType == b2_circleShape ) )
        {
            m_shapeType = b2_circleShape;
            UpdateShape();
        }

        if ( ImGui.RadioButton( "Capsule", m_shapeType == b2_capsuleShape ) )
        {
            m_shapeType = b2_capsuleShape;
            UpdateShape();
        }

        if ( ImGui.RadioButton( "Segment", m_shapeType == b2_segmentShape ) )
        {
            m_shapeType = b2_segmentShape;
            UpdateShape();
        }

        if ( ImGui.RadioButton( "Polygon", m_shapeType == b2_polygonShape ) )
        {
            m_shapeType = b2_polygonShape;
            UpdateShape();
        }

        if ( ImGui.SliderFloat( "Scale", &m_scale, 0.1f, 10.0f, "%.2f" ) )
        {
            UpdateShape();
        }

        b2BodyId bodyId = b2Shape_GetBody( m_shapeId );
        b2BodyType bodyType = b2Body_GetType( bodyId );

        if ( ImGui.RadioButton( "Static", bodyType == b2BodyType.b2_staticBody ) )
        {
            b2Body_SetType( bodyId, b2BodyType.b2_staticBody );
        }

        if ( ImGui.RadioButton( "Kinematic", bodyType == b2BodyType.b2_kinematicBody ) )
        {
            b2Body_SetType( bodyId, b2BodyType.b2_kinematicBody );
        }

        if ( ImGui.RadioButton( "Dynamic", bodyType == b2BodyType.b2_dynamicBody ) )
        {
            b2Body_SetType( bodyId, b2BodyType.b2_dynamicBody );
        }

        ImGui.End();
    }

    void Step( Settings& settings ) override
    {
        Sample::Step( settings );
    }

    static Sample* Create( Settings& settings )
    {
        return new ModifyGeometry( settings );
    }

    b2ShapeId m_shapeId;
    b2ShapeType m_shapeType;
    float m_scale;

    union
    {
        b2Circle m_circle;
        b2Capsule m_capsule;
        b2Segment m_segment;
        b2Polygon m_polygon;
    };
    };

    static int sampleModifyGeometry = RegisterSample( "Shapes", "Modify Geometry", ModifyGeometry::Create );

