using System.Diagnostics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.math_function;

namespace Box2D.NET.Samples.Samples.Collisions;

constexpr int SIMPLEX_CAPACITY = 20;

class ShapeDistance : Sample
{
    static int sampleShapeDistance = RegisterSample( "Collision", "Shape Distance", ShapeDistance::Create );
    enum ShapeType
    {
        e_point,
        e_segment,
        e_triangle,
        e_box
    };

    public ShapeDistance( Settings settings )
        : Sample( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.0f, 0.0f };
            Draw.g_camera.m_zoom = 3.0f;
        }

        m_point = b2Vec2_zero;
        m_segment = { { -0.5f, 0.0f }, { 0.5f, 0.0f } };

        {
            b2Vec2 points[3] = { { -0.5f, 0.0f }, { 0.5f, 0.0f }, { 0.0f, 1.0f } };
            b2Hull hull = b2ComputeHull( points, 3 );
            m_triangle = b2MakePolygon( &hull, 0.0f );
        }

        m_box = b2MakeBox( 0.5f, 0.5f );

        m_transform = { { 1.5f, -1.5f }, b2Rot_identity };
        m_angle = 0.0f;

        m_cache = b2_emptySimplexCache;
        m_simplexCount = 0;
        m_startPoint = { 0.0f, 0.0f };
        m_basePosition = { 0.0f, 0.0f };
        m_baseAngle = 0.0f;

        m_dragging = false;
        m_rotating = false;
        m_showIndices = false;
        m_useCache = false;
        m_drawSimplex = false;

        m_typeA = e_box;
        m_typeB = e_box;
        m_radiusA = 0.0f;
        m_radiusB = 0.0f;

        m_proxyA = MakeProxy( m_typeA, m_radiusA );
        m_proxyB = MakeProxy( m_typeB, m_radiusB );
    }

    b2ShapeProxy MakeProxy( ShapeType type, float radius )
    {
        b2ShapeProxy proxy = {};
        proxy.radius = radius;

        switch ( type )
        {
            case e_point:
                proxy.points[0] = b2Vec2_zero;
                proxy.count = 1;
                break;

            case e_segment:
                proxy.points[0] = m_segment.point1;
                proxy.points[1] = m_segment.point2;
                proxy.count = 2;
                break;

            case e_triangle:
                proxy.points[0] = m_triangle.vertices[0];
                proxy.points[1] = m_triangle.vertices[1];
                proxy.points[2] = m_triangle.vertices[2];
                proxy.count = 3;
                break;

            case e_box:
                proxy.points[0] = m_box.vertices[0];
                proxy.points[1] = m_box.vertices[1];
                proxy.points[2] = m_box.vertices[2];
                proxy.points[3] = m_box.vertices[3];
                proxy.count = 4;
                break;

            default:
                Debug.Assert( false );
        }

        return proxy;
    }

    void DrawShape( ShapeType type, b2Transform transform, float radius, b2HexColor color )
    {
        switch ( type )
        {
            case e_point:
            {
                b2Vec2 p = b2TransformPoint( transform, m_point );
                if ( radius > 0.0f )
                {
                    Draw.g_draw.DrawSolidCircle( transform, m_point, radius, color );
                }
                else
                {
                    Draw.g_draw.DrawPoint( p, 5.0f, color );
                }
            }
                break;

            case e_segment:
            {
                b2Vec2 p1 = b2TransformPoint( transform, m_segment.point1 );
                b2Vec2 p2 = b2TransformPoint( transform, m_segment.point2 );

                if ( radius > 0.0f )
                {
                    Draw.g_draw.DrawSolidCapsule( p1, p2, radius, color );
                }
                else
                {
                    Draw.g_draw.DrawSegment( p1, p2, color );
                }
            }
                break;

            case e_triangle:
                Draw.g_draw.DrawSolidPolygon( transform, m_triangle.vertices, 3, radius, color );
                break;

            case e_box:
                Draw.g_draw.DrawSolidPolygon( transform, m_box.vertices, 4, radius, color );
                break;

            default:
                Debug.Assert( false );
        }
    }

    void UpdateUI() override
    {
        float height = 310.0f;
        ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
        ImGui.SetNextWindowSize( new Vector2( 240.0f, height ) );

        ImGui.Begin( "Shape Distance", nullptr, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize );

        const char* shapeTypes[] = { "point", "segment", "triangle", "box" };
        int shapeType = int( m_typeA );
        if ( ImGui.Combo( "shape A", &shapeType, shapeTypes, IM_ARRAYSIZE( shapeTypes ) ) )
        {
            m_typeA = ShapeType( shapeType );
            m_proxyA = MakeProxy( m_typeA, m_radiusA );
        }

        if ( ImGui.SliderFloat( "radius A", &m_radiusA, 0.0f, 0.5f, "%.2f" ) )
        {
            m_proxyA.radius = m_radiusA;
        }

        shapeType = int( m_typeB );
        if ( ImGui.Combo( "shape B", &shapeType, shapeTypes, IM_ARRAYSIZE( shapeTypes ) ) )
        {
            m_typeB = ShapeType( shapeType );
            m_proxyB = MakeProxy( m_typeB, m_radiusB );
        }

        if ( ImGui.SliderFloat( "radius B", &m_radiusB, 0.0f, 0.5f, "%.2f" ) )
        {
            m_proxyB.radius = m_radiusB;
        }

        ImGui.Separator();

        ImGui.SliderFloat( "x offset", &m_transform.p.x, -2.0f, 2.0f, "%.2f" );
        ImGui.SliderFloat( "y offset", &m_transform.p.y, -2.0f, 2.0f, "%.2f" );

        if ( ImGui.SliderFloat( "angle", &m_angle, -B2_PI, B2_PI, "%.2f" ) )
        {
            m_transform.q = b2MakeRot( m_angle );
        }

        ImGui.Separator();

        ImGui.Checkbox( "show indices", &m_showIndices );
        ImGui.Checkbox( "use cache", &m_useCache );

        ImGui.Separator();

        if ( ImGui.Checkbox( "draw simplex", &m_drawSimplex ) )
        {
            m_simplexIndex = 0;
        }

        if ( m_drawSimplex )
        {
            ImGui.SliderInt( "index", &m_simplexIndex, 0, m_simplexCount - 1 );
            m_simplexIndex = b2ClampInt( m_simplexIndex, 0, m_simplexCount - 1 );
        }

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

    static b2Vec2 Weight2( float a1, b2Vec2 w1, float a2, b2Vec2 w2 )
    {
        return { a1 * w1.x + a2 * w2.x, a1 * w1.y + a2 * w2.y };
    }

    static b2Vec2 Weight3( float a1, b2Vec2 w1, float a2, b2Vec2 w2, float a3, b2Vec2 w3 )
    {
        return { a1 * w1.x + a2 * w2.x + a3 * w3.x, a1 * w1.y + a2 * w2.y + a3 * w3.y };
    }

    void ComputeSimplexWitnessPoints( b2Vec2* a, b2Vec2* b, const b2Simplex* s )
    {
        switch ( s->count )
        {
            case 0:
                Debug.Assert( false );
                break;

            case 1:
                *a = s->v1.wA;
                *b = s->v1.wB;
                break;

            case 2:
                *a = Weight2( s->v1.a, s->v1.wA, s->v2.a, s->v2.wA );
                *b = Weight2( s->v1.a, s->v1.wB, s->v2.a, s->v2.wB );
                break;

            case 3:
                *a = Weight3( s->v1.a, s->v1.wA, s->v2.a, s->v2.wA, s->v3.a, s->v3.wA );
                *b = *a;
                break;

            default:
                Debug.Assert( false );
                break;
        }
    }

    void Step( Settings& ) override
    {
        b2DistanceInput input;
        input.proxyA = m_proxyA;
        input.proxyB = m_proxyB;
        input.transformA = b2Transform_identity;
        input.transformB = m_transform;
        input.useRadii = m_radiusA > 0.0f || m_radiusB > 0.0f;

        if ( m_useCache == false )
        {
            m_cache.count = 0;
        }

        b2DistanceOutput output = b2ShapeDistance( &m_cache, &input, m_simplexes, SIMPLEX_CAPACITY );

        m_simplexCount = output.simplexCount;

        DrawShape( m_typeA, b2Transform_identity, m_radiusA, b2HexColor.b2_colorCyan );
        DrawShape( m_typeB, m_transform, m_radiusB, b2HexColor.b2_colorBisque );

        if ( m_drawSimplex )
        {
            b2Simplex* simplex = m_simplexes + m_simplexIndex;
            b2SimplexVertex* vertices[3] = { &simplex->v1, &simplex->v2, &simplex->v3 };

            if ( m_simplexIndex > 0 )
            {
                // The first recorded simplex does not have valid barycentric coordinates
                b2Vec2 pointA, pointB;
                ComputeSimplexWitnessPoints( &pointA, &pointB, simplex );

                Draw.g_draw.DrawSegment( pointA, pointB, b2HexColor.b2_colorWhite );
                Draw.g_draw.DrawPoint( pointA, 5.0f, b2HexColor.b2_colorWhite );
                Draw.g_draw.DrawPoint( pointB, 5.0f, b2HexColor.b2_colorWhite );
            }

            b2HexColor colors[3] = { b2_colorRed, b2HexColor.b2_colorGreen, b2HexColor.b2_colorBlue };

            for ( int i = 0; i < simplex->count; ++i )
            {
                b2SimplexVertex* vertex = vertices[i];
                Draw.g_draw.DrawPoint( vertex->wA, 5.0f, colors[i] );
                Draw.g_draw.DrawPoint( vertex->wB, 5.0f, colors[i] );
            }
        }
        else
        {
            Draw.g_draw.DrawSegment( output.pointA, output.pointB, b2HexColor.b2_colorWhite );
            Draw.g_draw.DrawPoint( output.pointA, 5.0f, b2HexColor.b2_colorWhite );
            Draw.g_draw.DrawPoint( output.pointB, 5.0f, b2HexColor.b2_colorWhite );
        }

        if ( m_showIndices )
        {
            for ( int i = 0; i < m_proxyA.count; ++i )
            {
                b2Vec2 p = m_proxyA.points[i];
                Draw.g_draw.DrawString( p, " %d", i );
            }

            for ( int i = 0; i < m_proxyB.count; ++i )
            {
                b2Vec2 p = b2TransformPoint( m_transform, m_proxyB.points[i] );
                Draw.g_draw.DrawString( p, " %d", i );
            }
        }

        Draw.g_draw.DrawString( 5, m_textLine, "mouse button 1: drag" );
        m_textLine += m_textIncrement;
        Draw.g_draw.DrawString( 5, m_textLine, "mouse button 1 + shift: rotate" );
        m_textLine += m_textIncrement;
        Draw.g_draw.DrawString( 5, m_textLine, "distance = %.2f, iterations = %d", output.distance, output.iterations );
        m_textLine += m_textIncrement;

        if ( m_cache.count == 1 )
        {
            Draw.g_draw.DrawString( 5, m_textLine, "cache = {%d}, {%d}", m_cache.indexA[0], m_cache.indexB[0] );
        }
        else if ( m_cache.count == 2 )
        {
            Draw.g_draw.DrawString( 5, m_textLine, "cache = {%d, %d}, {%d, %d}", m_cache.indexA[0], m_cache.indexA[1],
                m_cache.indexB[0], m_cache.indexB[1] );
        }
        else if ( m_cache.count == 3 )
        {
            Draw.g_draw.DrawString( 5, m_textLine, "cache = {%d, %d, %d}, {%d, %d, %d}", m_cache.indexA[0], m_cache.indexA[1],
                m_cache.indexA[2], m_cache.indexB[0], m_cache.indexB[1], m_cache.indexB[2] );
        }
        m_textLine += m_textIncrement;
    }

    static Sample* Create( Settings& settings )
    {
        return new ShapeDistance( settings );
    }

    b2Polygon m_box;
    b2Polygon m_triangle;
    b2Vec2 m_point;
    b2Segment m_segment;

    ShapeType m_typeA;
    ShapeType m_typeB;
    float m_radiusA;
    float m_radiusB;
    b2ShapeProxy m_proxyA;
    b2ShapeProxy m_proxyB;

    b2SimplexCache m_cache;
    b2Simplex m_simplexes[SIMPLEX_CAPACITY];
    int m_simplexCount;
    int m_simplexIndex;

    b2Transform m_transform;
    float m_angle;

    b2Vec2 m_basePosition;
    b2Vec2 m_startPoint;
    float m_baseAngle;

    bool m_dragging;
    bool m_rotating;
    bool m_showIndices;
    bool m_useCache;
    bool m_drawSimplex;
}

