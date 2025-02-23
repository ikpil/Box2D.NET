using System.Diagnostics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Collisions;

    class OverlapWorld : Sample
    {
    public:
    enum
    {
        e_circleShape = 0,
        e_capsuleShape = 1,
        e_boxShape = 2
    };

    enum
    {
        e_maxCount = 64,
        e_maxDoomed = 16,
    };

    static bool OverlapResultFcn( b2ShapeId shapeId, void* context )
    {
        ShapeUserData* userData = (ShapeUserData*)b2Shape_GetUserData( shapeId );
        if ( userData != nullptr && userData->ignore )
        {
            // continue the query
            return true;
        }

        OverlapWorld* sample = (OverlapWorld*)context;

        if ( sample->m_doomCount < e_maxDoomed )
        {
            int index = sample->m_doomCount;
            sample->m_doomIds[index] = shapeId;
            sample->m_doomCount += 1;
        }

        // continue the query
        return true;
    }

    explicit OverlapWorld( Settings& settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.0f, 10.0f };
            Draw.g_camera.m_zoom = 25.0f * 0.7f;
        }

        {
            b2Vec2 vertices[3] = { { -0.5f, 0.0f }, { 0.5f, 0.0f }, { 0.0f, 1.5f } };
            b2Hull hull = b2ComputeHull( vertices, 3 );
            m_polygons[0] = b2MakePolygon( &hull, 0.0f );
        }

        {
            b2Vec2 vertices[3] = { { -0.1f, 0.0f }, { 0.1f, 0.0f }, { 0.0f, 1.5f } };
            b2Hull hull = b2ComputeHull( vertices, 3 );
            m_polygons[1] = b2MakePolygon( &hull, 0.0f );
        }

        {
            float w = 1.0f;
            float b = w / ( 2.0f + sqrtf( 2.0f ) );
            float s = sqrtf( 2.0f ) * b;

            b2Vec2 vertices[8] = { { 0.5f * s, 0.0f }, { 0.5f * w, b },		 { 0.5f * w, b + s }, { 0.5f * s, w },
                { -0.5f * s, w },   { -0.5f * w, b + s }, { -0.5f * w, b },	  { -0.5f * s, 0.0f } };

            b2Hull hull = b2ComputeHull( vertices, 8 );
            m_polygons[2] = b2MakePolygon( &hull, 0.0f );
        }

        m_polygons[3] = b2MakeBox( 0.5f, 0.5f );
        m_capsule = { { -0.5f, 0.0f }, { 0.5f, 0.0f }, 0.25f };
        m_circle = { { 0.0f, 0.0f }, 0.5f };
        m_segment = { { -1.0f, 0.0f }, { 1.0f, 0.0f } };

        m_bodyIndex = 0;

        for ( int i = 0; i < e_maxCount; ++i )
        {
            m_bodyIds[i] = b2_nullBodyId;
        }

        m_ignoreIndex = 7;

        m_shapeType = e_circleShape;

        m_queryCircle = { { 0.0f, 0.0f }, 1.0f };
        m_queryCapsule = { { -1.0f, 0.0f }, { 1.0f, 0.0f }, 0.5f };
        m_queryBox = b2MakeBox( 2.0f, 0.5f );

        m_position = { 0.0f, 10.0f };
        m_angle = 0.0f;
        m_dragging = false;
        m_rotating = false;

        m_doomCount = 0;

        CreateN( 0, 10 );
    }

    void Create( int index )
    {
        if ( B2_IS_NON_NULL( m_bodyIds[m_bodyIndex] ) )
        {
            b2DestroyBody( m_bodyIds[m_bodyIndex] );
            m_bodyIds[m_bodyIndex] = b2_nullBodyId;
        }

        float x = RandomFloatRange( -20.0f, 20.0f );
        float y = RandomFloatRange( 0.0f, 20.0f );

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = { x, y };
        bodyDef.rotation = b2MakeRot( RandomFloatRange( -B2_PI, B2_PI ) );

        m_bodyIds[m_bodyIndex] = b2CreateBody( m_worldId, &bodyDef );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.userData = m_userData + m_bodyIndex;
        m_userData[m_bodyIndex].index = m_bodyIndex;
        m_userData[m_bodyIndex].ignore = false;
        if ( m_bodyIndex == m_ignoreIndex )
        {
            m_userData[m_bodyIndex].ignore = true;
        }

        if ( index < 4 )
        {
            b2CreatePolygonShape( m_bodyIds[m_bodyIndex], &shapeDef, m_polygons + index );
        }
        else if ( index == 4 )
        {
            b2CreateCircleShape( m_bodyIds[m_bodyIndex], &shapeDef, &m_circle );
        }
        else if ( index == 5 )
        {
            b2CreateCapsuleShape( m_bodyIds[m_bodyIndex], &shapeDef, &m_capsule );
        }
        else
        {
            b2CreateSegmentShape( m_bodyIds[m_bodyIndex], &shapeDef, &m_segment );
        }

        m_bodyIndex = ( m_bodyIndex + 1 ) % e_maxCount;
    }

    void CreateN( int index, int count )
    {
        for ( int i = 0; i < count; ++i )
        {
            Create( index );
        }
    }

    void DestroyBody()
    {
        for ( int i = 0; i < e_maxCount; ++i )
        {
            if ( B2_IS_NON_NULL( m_bodyIds[i] ) )
            {
                b2DestroyBody( m_bodyIds[i] );
                m_bodyIds[i] = b2_nullBodyId;
                return;
            }
        }
    }

    void MouseDown( b2Vec2 p, int button, int mods ) override
    {
        if ( button == GLFW_MOUSE_BUTTON_1 )
        {
            if ( mods == 0 && m_rotating == false )
            {
                m_dragging = true;
                m_position = p;
            }
            else if ( mods == GLFW_MOD_SHIFT && m_dragging == false )
            {
                m_rotating = true;
                m_startPosition = p;
                m_baseAngle = m_angle;
            }
        }
    }

    void MouseUp( b2Vec2, int button ) override
    {
        if ( button == GLFW_MOUSE_BUTTON_1 )
        {
            m_dragging = false;
            m_rotating = false;
        }
    }

    void MouseMove( b2Vec2 p ) override
    {
        if ( m_dragging )
        {
            m_position = p;
        }
        else if ( m_rotating )
        {
            float dx = p.x - m_startPosition.x;
            m_angle = m_baseAngle + 1.0f * dx;
        }
    }

    void UpdateUI() override
    {
        float height = 330.0f;
        ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
        ImGui.SetNextWindowSize( new Vector2( 140.0f, height ) );

        ImGui.Begin( "Overlap World", nullptr, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize );

        if ( ImGui.Button( "Polygon 1" ) )
            Create( 0 );
        ImGui.SameLine();
        if ( ImGui.Button( "10x##Poly1" ) )
            CreateN( 0, 10 );

        if ( ImGui.Button( "Polygon 2" ) )
            Create( 1 );
        ImGui.SameLine();
        if ( ImGui.Button( "10x##Poly2" ) )
            CreateN( 1, 10 );

        if ( ImGui.Button( "Polygon 3" ) )
            Create( 2 );
        ImGui.SameLine();
        if ( ImGui.Button( "10x##Poly3" ) )
            CreateN( 2, 10 );

        if ( ImGui.Button( "Box" ) )
            Create( 3 );
        ImGui.SameLine();
        if ( ImGui.Button( "10x##Box" ) )
            CreateN( 3, 10 );

        if ( ImGui.Button( "Circle" ) )
            Create( 4 );
        ImGui.SameLine();
        if ( ImGui.Button( "10x##Circle" ) )
            CreateN( 4, 10 );

        if ( ImGui.Button( "Capsule" ) )
            Create( 5 );
        ImGui.SameLine();
        if ( ImGui.Button( "10x##Capsule" ) )
            CreateN( 5, 10 );

        if ( ImGui.Button( "Segment" ) )
            Create( 6 );
        ImGui.SameLine();
        if ( ImGui.Button( "10x##Segment" ) )
            CreateN( 6, 10 );

        if ( ImGui.Button( "Destroy Shape" ) )
        {
            DestroyBody();
        }

        ImGui.Separator();
        ImGui.Text( "Overlap Shape" );
        ImGui.RadioButton( "Circle##Overlap", &m_shapeType, e_circleShape );
        ImGui.RadioButton( "Capsule##Overlap", &m_shapeType, e_capsuleShape );
        ImGui.RadioButton( "Box##Overlap", &m_shapeType, e_boxShape );

        ImGui.End();
    }

    void Step( Settings& settings ) override
    {
        Sample::Step( settings );

        Draw.g_draw.DrawString( 5, m_textLine, "left mouse button: drag query shape" );
        m_textLine += m_textIncrement;
        Draw.g_draw.DrawString( 5, m_textLine, "left mouse button + shift: rotate query shape" );
        m_textLine += m_textIncrement;

        m_doomCount = 0;

        b2Transform transform = { m_position, b2MakeRot( m_angle ) };

        if ( m_shapeType == e_circleShape )
        {
            b2World_OverlapCircle( m_worldId, &m_queryCircle, transform, b2DefaultQueryFilter(), OverlapWorld::OverlapResultFcn,
                this );
            Draw.g_draw.DrawSolidCircle( transform, b2Vec2_zero, m_queryCircle.radius, b2HexColor.b2_colorWhite );
        }
        else if ( m_shapeType == e_capsuleShape )
        {
            b2World_OverlapCapsule( m_worldId, &m_queryCapsule, transform, b2DefaultQueryFilter(), OverlapWorld::OverlapResultFcn,
                this );
            b2Vec2 p1 = b2TransformPoint( transform, m_queryCapsule.center1 );
            b2Vec2 p2 = b2TransformPoint( transform, m_queryCapsule.center2 );
            Draw.g_draw.DrawSolidCapsule( p1, p2, m_queryCapsule.radius, b2HexColor.b2_colorWhite );
        }
        else if ( m_shapeType == e_boxShape )
        {
            b2World_OverlapPolygon( m_worldId, &m_queryBox, transform, b2DefaultQueryFilter(), OverlapWorld::OverlapResultFcn,
                this );
            b2Vec2 points[B2_MAX_POLYGON_VERTICES] = { };
            for ( int i = 0; i < m_queryBox.count; ++i )
            {
                points[i] = b2TransformPoint( transform, m_queryBox.vertices[i] );
            }
            Draw.g_draw.DrawPolygon( points, m_queryBox.count, b2HexColor.b2_colorWhite );
        }

        if ( B2_IS_NON_NULL( m_bodyIds[m_ignoreIndex] ) )
        {
            b2Vec2 p = b2Body_GetPosition( m_bodyIds[m_ignoreIndex] );
            p.x -= 0.2f;
            Draw.g_draw.DrawString( p, "skip" );
        }

        for ( int i = 0; i < m_doomCount; ++i )
        {
            b2ShapeId shapeId = m_doomIds[i];
            ShapeUserData* userData = (ShapeUserData*)b2Shape_GetUserData( shapeId );
            if ( userData == nullptr )
            {
                continue;
            }

            int index = userData->index;
            Debug.Assert( 0 <= index && index < e_maxCount );
            Debug.Assert( B2_IS_NON_NULL( m_bodyIds[index] ) );

            b2DestroyBody( m_bodyIds[index] );
            m_bodyIds[index] = b2_nullBodyId;
        }
    }

    static Sample* Create( Settings& settings )
    {
        return new OverlapWorld( settings );
    }

    int m_bodyIndex;
    b2BodyId m_bodyIds[e_maxCount];
    ShapeUserData m_userData[e_maxCount];
    b2Polygon m_polygons[4];
    b2Capsule m_capsule;
    b2Circle m_circle;
    b2Segment m_segment;
    int m_ignoreIndex;

    b2ShapeId m_doomIds[e_maxDoomed];
    int m_doomCount;

    b2Circle m_queryCircle;
    b2Capsule m_queryCapsule;
    b2Polygon m_queryBox;

    int m_shapeType;
    b2Transform m_transform;

    b2Vec2 m_startPosition;

    b2Vec2 m_position;
    b2Vec2 m_basePosition;
    float m_angle;
    float m_baseAngle;

    bool m_dragging;
    bool m_rotating;
    };

    static int sampleOverlapWorld = RegisterSample( "Collision", "Overlap World", OverlapWorld::Create );
