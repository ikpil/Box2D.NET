namespace Box2D.NET.Samples.Samples.Collisions;

// Tests manifolds and contact points
    class Manifold : Sample
    {
    public:
    explicit Manifold( Settings& settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            // Draw.g_camera.m_center = {1.8f, 15.0f};
            Draw.g_camera.m_center = { 1.8f, 0.0f };
            Draw.g_camera.m_zoom = 25.0f * 0.45f;
        }

        m_smgroxCache1 = b2_emptySimplexCache;
        m_smgroxCache2 = b2_emptySimplexCache;
        m_smgcapCache1 = b2_emptySimplexCache;
        m_smgcapCache2 = b2_emptySimplexCache;

        m_transform = b2Transform_identity;
        m_transform.p.x = 1.0f;
        m_transform.p.y = 0.0f;
        // m_transform.q = b2MakeRot( 0.5f * b2_pi );
        m_angle = 0.0f;
        m_round = 0.1f;

        m_startPoint = { 0.0f, 0.0f };
        m_basePosition = { 0.0f, 0.0f };
        m_baseAngle = 0.0f;

        m_dragging = false;
        m_rotating = false;
        m_showIds = false;
        m_showSeparation = false;
        m_showAnchors = false;
        m_enableCaching = true;

        b2Vec2 points[3] = { { -0.1f, -0.5f }, { 0.1f, -0.5f }, { 0.0f, 0.5f } };
        m_wedge = b2ComputeHull( points, 3 );
    }

    void UpdateUI() override
    {
        float height = 300.0f;
        ImGui.SetNextWindowPos( ImVec2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
        ImGui.SetNextWindowSize( ImVec2( 240.0f, height ) );

        ImGui.Begin( "Manifold", nullptr, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize );

        ImGui.SliderFloat( "x offset", &m_transform.p.x, -2.0f, 2.0f, "%.2f" );
        ImGui.SliderFloat( "y offset", &m_transform.p.y, -2.0f, 2.0f, "%.2f" );

        if ( ImGui.SliderFloat( "angle", &m_angle, -B2_PI, B2_PI, "%.2f" ) )
        {
            m_transform.q = b2MakeRot( m_angle );
        }

        ImGui.SliderFloat( "round", &m_round, 0.0f, 0.4f, "%.1f" );
        ImGui.Checkbox( "show ids", &m_showIds );
        ImGui.Checkbox( "show separation", &m_showSeparation );
        ImGui.Checkbox( "show anchors", &m_showAnchors );
        ImGui.Checkbox( "enable caching", &m_enableCaching );

        if ( ImGui.Button( "Reset" ) )
        {
            m_transform = b2Transform_identity;
            m_angle = 0.0f;
        }

        ImGui.Separator();

        ImGui.Text( "mouse button 1: drag" );
        ImGui.Text( "mouse button 1 + shift: rotate" );

        ImGui.End();
    }

    void MouseDown( b2Vec2 p, int button, int mods ) override
    {
        if ( button == GLFW_MOUSE_BUTTON_1 )
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
            m_transform.p.x = m_basePosition.x + 0.5f * ( p.x - m_startPoint.x );
            m_transform.p.y = m_basePosition.y + 0.5f * ( p.y - m_startPoint.y );
        }
        else if ( m_rotating )
        {
            float dx = p.x - m_startPoint.x;
            m_angle = b2ClampFloat( m_baseAngle + 1.0f * dx, -B2_PI, B2_PI );
            m_transform.q = b2MakeRot( m_angle );
        }
    }

    void DrawManifold( const b2Manifold* manifold, b2Vec2 origin1, b2Vec2 origin2 )
    {
        for ( int i = 0; i < manifold->pointCount; ++i )
        {
            const b2ManifoldPoint* mp = manifold->points + i;

            b2Vec2 p1 = mp->point;
            b2Vec2 p2 = b2MulAdd( p1, 0.5f, manifold->normal );
            Draw.g_draw.DrawSegment( p1, p2, b2_colorWhite );

            if ( m_showAnchors )
            {
                Draw.g_draw.DrawPoint( b2Add( origin1, mp->anchorA ), 5.0f, b2_colorRed );
                Draw.g_draw.DrawPoint( b2Add( origin2, mp->anchorB ), 5.0f, b2_colorGreen );
            }
            else
            {
                Draw.g_draw.DrawPoint( p1, 10.0f, b2_colorBlue );
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
        b2Vec2 offset = { -10.0f, -5.0f };
        b2Vec2 increment = { 4.0f, 0.0f };

        b2HexColor color1 = b2_colorAquamarine;
        b2HexColor color2 = b2_colorPaleGoldenRod;

        if ( m_enableCaching == false )
        {
            m_smgroxCache1 = b2_emptySimplexCache;
            m_smgroxCache2 = b2_emptySimplexCache;
            m_smgcapCache1 = b2_emptySimplexCache;
            m_smgcapCache2 = b2_emptySimplexCache;
        }

        // circle-circle
        {
            b2Circle circle1 = { { 0.0f, 0.0f }, 0.5f };
            b2Circle circle2 = { { 0.0f, 0.0f }, 1.0f };

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Manifold m = b2CollideCircles( &circle1, transform1, &circle2, transform2 );

            Draw.g_draw.DrawSolidCircle( transform1, circle1.center, circle1.radius, color1 );
            Draw.g_draw.DrawSolidCircle( transform2, circle2.center, circle2.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // capsule-circle
        {
            b2Capsule capsule = { { -0.5f, 0.0f }, { 0.5f, 0.0 }, 0.25f };
            b2Circle circle = { { 0.0f, 0.0f }, 0.5f };

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Manifold m = b2CollideCapsuleAndCircle( &capsule, transform1, &circle, transform2 );

            b2Vec2 v1 = b2TransformPoint( transform1, capsule.center1 );
            b2Vec2 v2 = b2TransformPoint( transform1, capsule.center2 );
            Draw.g_draw.DrawSolidCapsule( v1, v2, capsule.radius, color1 );

            Draw.g_draw.DrawSolidCircle( transform2, circle.center, circle.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // segment-circle
        {
            b2Segment segment = { { -1.0f, 0.0f }, { 1.0f, 0.0 } };
            b2Circle circle = { { 0.0f, 0.0f }, 0.5f };

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Manifold m = b2CollideSegmentAndCircle( &segment, transform1, &circle, transform2 );

            b2Vec2 p1 = b2TransformPoint( transform1, segment.point1 );
            b2Vec2 p2 = b2TransformPoint( transform1, segment.point2 );
            Draw.g_draw.DrawSegment( p1, p2, color1 );

            Draw.g_draw.DrawSolidCircle( transform2, circle.center, circle.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // box-circle
        {
            b2Circle circle = { { 0.0f, 0.0f }, 0.5f };
            b2Polygon box = b2MakeSquare( 0.5f );
            box.radius = m_round;

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Manifold m = b2CollidePolygonAndCircle( &box, transform1, &circle, transform2 );

            Draw.g_draw.DrawSolidPolygon( transform1, box.vertices, box.count, m_round, color1 );
            Draw.g_draw.DrawSolidCircle( transform2, circle.center, circle.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // capsule-capsule
        {
            b2Capsule capsule1 = { { -0.5f, 0.0f }, { 0.5f, 0.0 }, 0.25f };
            b2Capsule capsule2 = { { 0.25f, 0.0f }, { 1.0f, 0.0 }, 0.1f };

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Manifold m = b2CollideCapsules( &capsule1, transform1, &capsule2, transform2 );

            b2Vec2 v1 = b2TransformPoint( transform1, capsule1.center1 );
            b2Vec2 v2 = b2TransformPoint( transform1, capsule1.center2 );
            Draw.g_draw.DrawSolidCapsule( v1, v2, capsule1.radius, color1 );

            v1 = b2TransformPoint( transform2, capsule2.center1 );
            v2 = b2TransformPoint( transform2, capsule2.center2 );
            Draw.g_draw.DrawSolidCapsule( v1, v2, capsule2.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // box-capsule
        {
            b2Capsule capsule = { { -0.4f, 0.0f }, { -0.1f, 0.0f }, 0.1f };
            b2Polygon box = b2MakeOffsetBox( 0.25f, 1.0f, { 1.0f, -1.0f }, b2MakeRot( 0.25f * B2_PI ) );

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Manifold m = b2CollidePolygonAndCapsule( &box, transform1, &capsule, transform2 );

            Draw.g_draw.DrawSolidPolygon( transform1, box.vertices, box.count, box.radius, color1 );

            b2Vec2 v1 = b2TransformPoint( transform2, capsule.center1 );
            b2Vec2 v2 = b2TransformPoint( transform2, capsule.center2 );
            Draw.g_draw.DrawSolidCapsule( v1, v2, capsule.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // segment-capsule
        {
            b2Segment segment = { { -1.0f, 0.0f }, { 1.0f, 0.0 } };
            b2Capsule capsule = { { -0.5f, 0.0f }, { 0.5f, 0.0 }, 0.25f };

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Manifold m = b2CollideSegmentAndCapsule( &segment, transform1, &capsule, transform2 );

            b2Vec2 p1 = b2TransformPoint( transform1, segment.point1 );
            b2Vec2 p2 = b2TransformPoint( transform1, segment.point2 );
            Draw.g_draw.DrawSegment( p1, p2, color1 );

            p1 = b2TransformPoint( transform2, capsule.center1 );
            p2 = b2TransformPoint( transform2, capsule.center2 );
            Draw.g_draw.DrawSolidCapsule( p1, p2, capsule.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        offset = { -10.0f, 0.0f };

        // square-square
        {
            b2Polygon box1 = b2MakeSquare( 0.5f );
            b2Polygon box = b2MakeSquare( 0.5f );

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Manifold m = b2CollidePolygons( &box1, transform1, &box, transform2 );

            Draw.g_draw.DrawSolidPolygon( transform1, box1.vertices, box1.count, box1.radius, color1 );
            Draw.g_draw.DrawSolidPolygon( transform2, box.vertices, box.count, box.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // box-box
        {
            b2Polygon box1 = b2MakeBox( 2.0f, 0.1f );
            b2Polygon box = b2MakeSquare( 0.25f );

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };
            // b2Transform transform2 = {b2Add({0.0f, -0.1f}, offset), {0.0f, 1.0f}};

            b2Manifold m = b2CollidePolygons( &box1, transform1, &box, transform2 );

            Draw.g_draw.DrawSolidPolygon( transform1, box1.vertices, box1.count, box1.radius, color1 );
            Draw.g_draw.DrawSolidPolygon( transform2, box.vertices, box.count, box.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // box-rox
        {
            b2Polygon box = b2MakeSquare( 0.5f );
            float h = 0.5f - m_round;
            b2Polygon rox = b2MakeRoundedBox( h, h, m_round );

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };
            // b2Transform transform2 = {b2Add({0.0f, -0.1f}, offset), {0.0f, 1.0f}};

            b2Manifold m = b2CollidePolygons( &box, transform1, &rox, transform2 );

            Draw.g_draw.DrawSolidPolygon( transform1, box.vertices, box.count, box.radius, color1 );
            Draw.g_draw.DrawSolidPolygon( transform2, rox.vertices, rox.count, rox.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // rox-rox
        {
            float h = 0.5f - m_round;
            b2Polygon rox = b2MakeRoundedBox( h, h, m_round );

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };
            // b2Transform transform1 = {{6.48024225f, 2.07872653f}, {-0.938356698f, 0.345668465f}};
            // b2Transform transform2 = {{5.52862263f, 2.51146317f}, {-0.859374702f, -0.511346340f}};

            b2Manifold m = b2CollidePolygons( &rox, transform1, &rox, transform2 );

            Draw.g_draw.DrawSolidPolygon( transform1, rox.vertices, rox.count, rox.radius, color1 );
            Draw.g_draw.DrawSolidPolygon( transform2, rox.vertices, rox.count, rox.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // segment-rox
        {
            b2Segment segment = { { -1.0f, 0.0f }, { 1.0f, 0.0 } };
            float h = 0.5f - m_round;
            b2Polygon rox = b2MakeRoundedBox( h, h, m_round );

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };
            // b2Transform transform2 = {b2Add({-1.44583416f, 0.397352695f}, offset), m_transform.q};

            b2Manifold m = b2CollideSegmentAndPolygon( &segment, transform1, &rox, transform2 );

            b2Vec2 p1 = b2TransformPoint( transform1, segment.point1 );
            b2Vec2 p2 = b2TransformPoint( transform1, segment.point2 );
            Draw.g_draw.DrawSegment( p1, p2, color1 );
            Draw.g_draw.DrawSolidPolygon( transform2, rox.vertices, rox.count, rox.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // wox-wox
        {
            b2Polygon wox = b2MakePolygon( &m_wedge, m_round );

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };
            // b2Transform transform2 = {b2Add({0.0f, -0.1f}, offset), {0.0f, 1.0f}};

            b2Manifold m = b2CollidePolygons( &wox, transform1, &wox, transform2 );

            Draw.g_draw.DrawSolidPolygon( transform1, wox.vertices, wox.count, wox.radius, color1 );
            Draw.g_draw.DrawSolidPolygon( transform1, wox.vertices, wox.count, 0.0f, color1 );
            Draw.g_draw.DrawSolidPolygon( transform2, wox.vertices, wox.count, wox.radius, color2 );
            Draw.g_draw.DrawSolidPolygon( transform2, wox.vertices, wox.count, 0.0f, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // wox-wox
        {
            b2Vec2 p1s[3] = { { 0.175740838, 0.224936664 }, { -0.301293969, 0.194021404 }, { -0.105151534, -0.432157338 } };
            b2Vec2 p2s[3] = { { -0.427884758, -0.225028217 }, { 0.0566576123, -0.128772855 }, { 0.176625848, 0.338923335 } };

            b2Hull h1 = b2ComputeHull( p1s, 3 );
            b2Hull h2 = b2ComputeHull( p2s, 3 );
            b2Polygon w1 = b2MakePolygon( &h1, 0.158798501 );
            b2Polygon w2 = b2MakePolygon( &h2, 0.205900759 );

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };
            // b2Transform transform2 = {b2Add({0.0f, -0.1f}, offset), {0.0f, 1.0f}};

            b2Manifold m = b2CollidePolygons( &w1, transform1, &w2, transform2 );

            Draw.g_draw.DrawSolidPolygon( transform1, w1.vertices, w1.count, w1.radius, color1 );
            Draw.g_draw.DrawSolidPolygon( transform1, w1.vertices, w1.count, 0.0f, color1 );
            Draw.g_draw.DrawSolidPolygon( transform2, w2.vertices, w2.count, w2.radius, color2 );
            Draw.g_draw.DrawSolidPolygon( transform2, w2.vertices, w2.count, 0.0f, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        offset = { -10.0f, 5.0f };

        // box-triangle
        {
            b2Polygon box = b2MakeBox( 1.0f, 1.0f );
            b2Vec2 points[3] = { { -0.05f, 0.0f }, { 0.05f, 0.0f }, { 0.0f, 0.1f } };
            b2Hull hull = b2ComputeHull( points, 3 );
            b2Polygon tri = b2MakePolygon( &hull, 0.0f );

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };
            // b2Transform transform2 = {b2Add({0.0f, -0.1f}, offset), {0.0f, 1.0f}};

            b2Manifold m = b2CollidePolygons( &box, transform1, &tri, transform2 );

            Draw.g_draw.DrawSolidPolygon( transform1, box.vertices, box.count, 0.0f, color1 );
            Draw.g_draw.DrawSolidPolygon( transform2, tri.vertices, tri.count, 0.0f, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset = b2Add( offset, increment );
        }

        // chain-segment vs circle
        {
            b2ChainSegment segment = { { 2.0f, 1.0f }, { { 1.0f, 1.0f }, { -1.0f, 0.0f } }, { -2.0f, 0.0f }, -1 };
            b2Circle circle = { { 0.0f, 0.0f }, 0.5f };

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Manifold m = b2CollideChainSegmentAndCircle( &segment, transform1, &circle, transform2 );

            b2Vec2 g1 = b2TransformPoint( transform1, segment.ghost1 );
            b2Vec2 g2 = b2TransformPoint( transform1, segment.ghost2 );
            b2Vec2 p1 = b2TransformPoint( transform1, segment.segment.point1 );
            b2Vec2 p2 = b2TransformPoint( transform1, segment.segment.point2 );
            Draw.g_draw.DrawSegment( g1, p1, b2_colorLightGray );
            Draw.g_draw.DrawSegment( p1, p2, color1 );
            Draw.g_draw.DrawSegment( p2, g2, b2_colorLightGray );
            Draw.g_draw.DrawSolidCircle( transform2, circle.center, circle.radius, color2 );

            DrawManifold( &m, transform1.p, transform2.p );

            offset.x += 2.0f * increment.x;
        }

        // chain-segment vs rounded polygon
        {
            b2ChainSegment segment1 = { { 2.0f, 1.0f }, { { 1.0f, 1.0f }, { -1.0f, 0.0f } }, { -2.0f, 0.0f }, -1 };
            b2ChainSegment segment2 = { { 3.0f, 1.0f }, { { 2.0f, 1.0f }, { 1.0f, 1.0f } }, { -1.0f, 0.0f }, -1 };
            // b2ChainSegment segment1 = {{2.0f, 0.0f}, {{1.0f, 0.0f}, {-1.0f, 0.0f}}, {-2.0f, 0.0f}, -1};
            // b2ChainSegment segment2 = {{3.0f, 0.0f}, {{2.0f, 0.0f}, {1.0f, 0.0f}}, {-1.0f, 0.0f}, -1};
            // b2ChainSegment segment1 = {{0.5f, 1.0f}, {{0.0f, 2.0f}, {-0.5f, 1.0f}}, {-1.0f, 0.0f}, -1};
            // b2ChainSegment segment2 = {{1.0f, 0.0f}, {{0.5f, 1.0f}, {0.0f, 2.0f}}, {-0.5f, 1.0f}, -1};
            float h = 0.5f - m_round;
            b2Polygon rox = b2MakeRoundedBox( h, h, m_round );

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Manifold m1 = b2CollideChainSegmentAndPolygon( &segment1, transform1, &rox, transform2, &m_smgroxCache1 );
            b2Manifold m2 = b2CollideChainSegmentAndPolygon( &segment2, transform1, &rox, transform2, &m_smgroxCache2 );

            {
                b2Vec2 g2 = b2TransformPoint( transform1, segment1.ghost2 );
                b2Vec2 p1 = b2TransformPoint( transform1, segment1.segment.point1 );
                b2Vec2 p2 = b2TransformPoint( transform1, segment1.segment.point2 );
                Draw.g_draw.DrawSegment( p1, p2, color1 );
                Draw.g_draw.DrawPoint( p1, 4.0f, color1 );
                Draw.g_draw.DrawPoint( p2, 4.0f, color1 );
                Draw.g_draw.DrawSegment( p2, g2, b2_colorLightGray );
            }

            {
                b2Vec2 g1 = b2TransformPoint( transform1, segment2.ghost1 );
                b2Vec2 p1 = b2TransformPoint( transform1, segment2.segment.point1 );
                b2Vec2 p2 = b2TransformPoint( transform1, segment2.segment.point2 );
                Draw.g_draw.DrawSegment( g1, p1, b2_colorLightGray );
                Draw.g_draw.DrawSegment( p1, p2, color1 );
                Draw.g_draw.DrawPoint( p1, 4.0f, color1 );
                Draw.g_draw.DrawPoint( p2, 4.0f, color1 );
            }

            Draw.g_draw.DrawSolidPolygon( transform2, rox.vertices, rox.count, rox.radius, color2 );
            Draw.g_draw.DrawPoint( b2TransformPoint( transform2, rox.centroid ), 5.0f, b2_colorGainsboro );

            DrawManifold( &m1, transform1.p, transform2.p );
            DrawManifold( &m2, transform1.p, transform2.p );

            offset.x += 2.0f * increment.x;
        }

        // chain-segment vs capsule
        {
            b2ChainSegment segment1 = { { 2.0f, 1.0f }, { { 1.0f, 1.0f }, { -1.0f, 0.0f } }, { -2.0f, 0.0f }, -1 };
            b2ChainSegment segment2 = { { 3.0f, 1.0f }, { { 2.0f, 1.0f }, { 1.0f, 1.0f } }, { -1.0f, 0.0f }, -1 };
            b2Capsule capsule = { { -0.5f, 0.0f }, { 0.5f, 0.0 }, 0.25f };

            b2Transform transform1 = { offset, b2Rot_identity };
            b2Transform transform2 = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Manifold m1 = b2CollideChainSegmentAndCapsule( &segment1, transform1, &capsule, transform2, &m_smgcapCache1 );
            b2Manifold m2 = b2CollideChainSegmentAndCapsule( &segment2, transform1, &capsule, transform2, &m_smgcapCache2 );

            {
                b2Vec2 g2 = b2TransformPoint( transform1, segment1.ghost2 );
                b2Vec2 p1 = b2TransformPoint( transform1, segment1.segment.point1 );
                b2Vec2 p2 = b2TransformPoint( transform1, segment1.segment.point2 );
                // Draw.g_draw.DrawSegment(g1, p1, b2_colorLightGray);
                Draw.g_draw.DrawSegment( p1, p2, color1 );
                Draw.g_draw.DrawPoint( p1, 4.0f, color1 );
                Draw.g_draw.DrawPoint( p2, 4.0f, color1 );
                Draw.g_draw.DrawSegment( p2, g2, b2_colorLightGray );
            }

            {
                b2Vec2 g1 = b2TransformPoint( transform1, segment2.ghost1 );
                b2Vec2 p1 = b2TransformPoint( transform1, segment2.segment.point1 );
                b2Vec2 p2 = b2TransformPoint( transform1, segment2.segment.point2 );
                Draw.g_draw.DrawSegment( g1, p1, b2_colorLightGray );
                Draw.g_draw.DrawSegment( p1, p2, color1 );
                Draw.g_draw.DrawPoint( p1, 4.0f, color1 );
                Draw.g_draw.DrawPoint( p2, 4.0f, color1 );
                // Draw.g_draw.DrawSegment(p2, g2, b2_colorLightGray);
            }

            b2Vec2 p1 = b2TransformPoint( transform2, capsule.center1 );
            b2Vec2 p2 = b2TransformPoint( transform2, capsule.center2 );
            Draw.g_draw.DrawSolidCapsule( p1, p2, capsule.radius, color2 );

            Draw.g_draw.DrawPoint( b2Lerp( p1, p2, 0.5f ), 5.0f, b2_colorGainsboro );

            DrawManifold( &m1, transform1.p, transform2.p );
            DrawManifold( &m2, transform1.p, transform2.p );

            offset.x += 2.0f * increment.x;
        }
    }

    static Sample* Create( Settings& settings )
    {
        return new Manifold( settings );
    }

    b2SimplexCache m_smgroxCache1;
    b2SimplexCache m_smgroxCache2;
    b2SimplexCache m_smgcapCache1;
    b2SimplexCache m_smgcapCache2;

    b2Hull m_wedge;

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
    bool m_enableCaching;
    };

    static int sampleManifoldIndex = RegisterSample( "Collision", "Manifold", Manifold::Create );
