using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.geometry;
using static Box2D.NET.math_function;
using static Box2D.NET.manifold;

namespace Box2D.NET.Samples.Samples.Collisions;

    class SmoothManifold : Sample
    {
    public:
    enum ShapeType
    {
        e_circleShape = 0,
        e_boxShape
    };

    explicit SmoothManifold( Settings& settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 2.0f, 20.0f };
            Draw.g_camera.m_zoom = 21.0f;
        }

        m_shapeType = e_boxShape;
        m_transform = { { 0.0f, 20.0f }, b2Rot_identity };
        m_angle = 0.0f;
        m_round = 0.0f;

        m_startPoint = { 0.0f, 00.0f };
        m_basePosition = { 0.0f, 0.0f };
        m_baseAngle = 0.0f;

        m_dragging = false;
        m_rotating = false;
        m_showIds = false;
        m_showAnchors = false;
        m_showSeparation = false;

        // https://betravis.github.io/shape-tools/path-to-polygon/
        m_count = 36;

        b2Vec2 points[36];
        points[0] = { -20.58325, 14.54175 };
        points[1] = { -21.90625, 15.8645 };
        points[2] = { -24.552, 17.1875 };
        points[3] = { -27.198, 11.89575 };
        points[4] = { -29.84375, 15.8645 };
        points[5] = { -29.84375, 21.15625 };
        points[6] = { -25.875, 23.802 };
        points[7] = { -20.58325, 25.125 };
        points[8] = { -25.875, 29.09375 };
        points[9] = { -20.58325, 31.7395 };
        points[10] = { -11.0089998, 23.2290001 };
        points[11] = { -8.67700005, 21.15625 };
        points[12] = { -6.03125, 21.15625 };
        points[13] = { -7.35424995, 29.09375 };
        points[14] = { -3.38549995, 29.09375 };
        points[15] = { 1.90625, 30.41675 };
        points[16] = { 5.875, 17.1875 };
        points[17] = { 11.16675, 25.125 };
        points[18] = { 9.84375, 29.09375 };
        points[19] = { 13.8125, 31.7395 };
        points[20] = { 21.75, 30.41675 };
        points[21] = { 28.3644981, 26.448 };
        points[22] = { 25.71875, 18.5105 };
        points[23] = { 24.3957481, 13.21875 };
        points[24] = { 17.78125, 11.89575 };
        points[25] = { 15.1355, 7.92700005 };
        points[26] = { 5.875, 9.25 };
        points[27] = { 1.90625, 11.89575 };
        points[28] = { -3.25, 11.89575 };
        points[29] = { -3.25, 9.9375 };
        points[30] = { -4.70825005, 9.25 };
        points[31] = { -8.67700005, 9.25 };
        points[32] = { -11.323, 11.89575 };
        points[33] = { -13.96875, 11.89575 };
        points[34] = { -15.29175, 14.54175 };
        points[35] = { -19.2605, 14.54175 };

        m_segments = (b2ChainSegment*)malloc( m_count * sizeof( b2ChainSegment ) );

        for ( int i = 0; i < m_count; ++i )
        {
            int i0 = i > 0 ? i - 1 : m_count - 1;
            int i1 = i;
            int i2 = i1 < m_count - 1 ? i1 + 1 : 0;
            int i3 = i2 < m_count - 1 ? i2 + 1 : 0;

            b2Vec2 g1 = points[i0];
            b2Vec2 p1 = points[i1];
            b2Vec2 p2 = points[i2];
            b2Vec2 g2 = points[i3];

            m_segments[i] = { g1, { p1, p2 }, g2, -1 };
        }
    }

    virtual ~SmoothManifold() override
    {
        free( m_segments );
    }

    void UpdateUI() override
    {
        float height = 290.0f;
        ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
        ImGui.SetNextWindowSize( new Vector2( 180.0f, height ) );

        ImGui.Begin( "Smooth Manifold", nullptr, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize );
        ImGui.PushItemWidth( 100.0f );

        {
            const char* shapeTypes[] = { "Circle", "Box" };
            int shapeType = int( m_shapeType );
            ImGui.Combo( "Shape", &shapeType, shapeTypes, IM_ARRAYSIZE( shapeTypes ) );
            m_shapeType = ShapeType( shapeType );
        }

        ImGui.SliderFloat( "x Offset", &m_transform.p.x, -2.0f, 2.0f, "%.2f" );
        ImGui.SliderFloat( "y Offset", &m_transform.p.y, -2.0f, 2.0f, "%.2f" );

        if ( ImGui.SliderFloat( "Angle", &m_angle, -B2_PI, B2_PI, "%.2f" ) )
        {
            m_transform.q = b2MakeRot( m_angle );
        }

        ImGui.SliderFloat( "Round", &m_round, 0.0f, 0.4f, "%.1f" );
        ImGui.Checkbox( "Show Ids", &m_showIds );
        ImGui.Checkbox( "Show Separation", &m_showSeparation );
        ImGui.Checkbox( "Show Anchors", &m_showAnchors );

        if ( ImGui.Button( "Reset" ) )
        {
            m_transform = b2Transform_identity;
            m_angle = 0.0f;
        }

        ImGui.Separator();

        ImGui.Text( "mouse button 1: drag" );
        ImGui.Text( "mouse button 1 + shift: rotate" );

        ImGui.PopItemWidth();
        ImGui.End();
    }

    void MouseDown( b2Vec2 p, int button, int mods ) override
    {
        if ( button == (int)MouseButton.Left )
        {
            if ( mods == 0 && m_rotating == false )
            {
                m_dragging = true;
                m_startPoint = p;
                m_basePosition = m_transform.p;
            }
            else if ( mods == GLFW_MOD_SHIFT && m_dragging == false )
            {
                m_rotating = true;
                m_startPoint = p;
                m_baseAngle = m_angle;
            }
        }
    }

    void MouseUp( b2Vec2, int button ) override
    {
        if ( button == (int)MouseButton.Left )
        {
            m_dragging = false;
            m_rotating = false;
        }
    }

    void MouseMove( b2Vec2 p ) override
    {
        if ( m_dragging )
        {
            m_transform.p.x = m_basePosition.x + ( p.x - m_startPoint.x );
            m_transform.p.y = m_basePosition.y + ( p.y - m_startPoint.y );
        }
        else if ( m_rotating )
        {
            float dx = p.x - m_startPoint.x;
            m_angle = b2ClampFloat( m_baseAngle + 1.0f * dx, -B2_PI, B2_PI );
            m_transform.q = b2MakeRot( m_angle );
        }
    }

    void DrawManifold( const b2Manifold* manifold )
    {
        for ( int i = 0; i < manifold->pointCount; ++i )
        {
            const b2ManifoldPoint* mp = manifold->points + i;

            b2Vec2 p1 = mp->point;
            b2Vec2 p2 = b2MulAdd( p1, 0.5f, manifold->normal );
            Draw.g_draw.DrawSegment( p1, p2, b2HexColor.b2_colorWhite );

            if ( m_showAnchors )
            {
                Draw.g_draw.DrawPoint( p1, 5.0f, b2HexColor.b2_colorGreen );
            }
            else
            {
                Draw.g_draw.DrawPoint( p1, 5.0f, b2HexColor.b2_colorGreen );
            }

            if ( m_showIds )
            {
                // uint indexA = mp->id >> 8;
                // uint indexB = 0xFF & mp->id;
                b2Vec2 p = { p1.x + 0.05f, p1.y - 0.02f };
                Draw.g_draw.DrawString( p, "0x%04x", mp->id );
            }

            if ( m_showSeparation )
            {
                b2Vec2 p = { p1.x + 0.05f, p1.y + 0.03f };
                Draw.g_draw.DrawString( p, "%.3f", mp->separation );
            }
        }
    }

    void Step( Settings& ) override
    {
        b2HexColor color1 = b2HexColor.b2_colorYellow;
        b2HexColor color2 = b2HexColor.b2_colorMagenta;

        b2Transform transform1 = b2Transform_identity;
        b2Transform transform2 = m_transform;

        for ( int i = 0; i < m_count; ++i )
        {
            const b2ChainSegment* segment = m_segments + i;
            b2Vec2 p1 = b2TransformPoint( transform1, segment->segment.point1 );
            b2Vec2 p2 = b2TransformPoint( transform1, segment->segment.point2 );
            Draw.g_draw.DrawSegment( p1, p2, color1 );
            Draw.g_draw.DrawPoint( p1, 4.0f, color1 );
        }

        // chain-segment vs circle
        if ( m_shapeType == e_circleShape )
        {
            b2Circle circle = { { 0.0f, 0.0f }, 0.5f };
            Draw.g_draw.DrawSolidCircle( transform2, circle.center, circle.radius, color2 );

            for ( int i = 0; i < m_count; ++i )
            {
                const b2ChainSegment* segment = m_segments + i;
                b2Manifold m = b2CollideChainSegmentAndCircle( segment, transform1, &circle, transform2 );
                DrawManifold( &m );
            }
        }
        else if ( m_shapeType == e_boxShape )
        {
            float h = 0.5f - m_round;
            b2Polygon rox = b2MakeRoundedBox( h, h, m_round );
            Draw.g_draw.DrawSolidPolygon( transform2, rox.vertices, rox.count, rox.radius, color2 );

            for ( int i = 0; i < m_count; ++i )
            {
                const b2ChainSegment* segment = m_segments + i;
                b2SimplexCache cache = {};
                b2Manifold m = b2CollideChainSegmentAndPolygon( segment, transform1, &rox, transform2, &cache );
                DrawManifold( &m );
            }
        }
    }

    static Sample* Create( Settings& settings )
    {
        return new SmoothManifold( settings );
    }

    ShapeType m_shapeType;

    b2ChainSegment* m_segments;
    int m_count;

    b2Transform m_transform;
    float m_angle;
    float m_round;

    b2Vec2 m_basePosition;
    b2Vec2 m_startPoint;
    float m_baseAngle;

    bool m_dragging;
    bool m_rotating;
    bool m_showIds;
    bool m_showAnchors;
    bool m_showSeparation;
    };

    static int sampleSmoothManifoldIndex = RegisterSample( "Collision", "Smooth Manifold", SmoothManifold::Create );
