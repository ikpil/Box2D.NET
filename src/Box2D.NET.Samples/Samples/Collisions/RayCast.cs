using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.math_function;

namespace Box2D.NET.Samples.Samples.Collisions;

    public class RayCast : Sample
    {
    public RayCast( Settings settings )
        : base ( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { 0.0f, 20.0f };
            Draw.g_camera.m_zoom = 17.5f;
        }

        m_circle = { { 0.0f, 0.0f }, 2.0f };
        m_capsule = { { -1.0f, 1.0f }, { 1.0f, -1.0f }, 1.5f };
        m_box = b2MakeBox( 2.0f, 2.0f );

        b2Vec2 vertices[3] = { { -2.0f, 0.0f }, { 2.0f, 0.0f }, { 2.0f, 3.0f } };
        b2Hull hull = b2ComputeHull( vertices, 3 );
        m_triangle = b2MakePolygon( &hull, 0.0f );

        m_segment = { { -3.0f, 0.0f }, { 3.0f, 0.0 } };

        m_transform = b2Transform_identity;
        m_angle = 0.0f;

        m_basePosition = { 0.0f, 0.0f };
        m_baseAngle = 0.0f;
        m_startPosition = { 0.0f, 0.0f };

        m_rayStart = { 0.0f, 30.0f };
        m_rayEnd = { 0.0f, 0.0f };

        m_rayDrag = false;
        m_translating = false;
        m_rotating = false;

        m_showFraction = false;
    }

    public override void UpdateUI()
    {
        float height = 230.0f;
        ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
        ImGui.SetNextWindowSize( new Vector2( 200.0f, height ) );

        ImGui.Begin( "Ray-cast", nullptr, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize );

        ImGui.PushItemWidth( 100.0f );

        ImGui.SliderFloat( "x offset", &m_transform.p.x, -2.0f, 2.0f, "%.2f" );
        ImGui.SliderFloat( "y offset", &m_transform.p.y, -2.0f, 2.0f, "%.2f" );

        if ( ImGui.SliderFloat( "angle", &m_angle, -B2_PI, B2_PI, "%.2f" ) )
        {
            m_transform.q = b2MakeRot( m_angle );
        }

        // if (ImGui.SliderFloat("ray radius", &m_rayRadius, 0.0f, 1.0f, "%.1f"))
        //{
        // }

        ImGui.Checkbox( "show fraction", &m_showFraction );

        if ( ImGui.Button( "Reset" ) )
        {
            m_transform = b2Transform_identity;
            m_angle = 0.0f;
        }

        ImGui.Separator();

        ImGui.Text( "mouse btn 1: ray cast" );
        ImGui.Text( "mouse btn 1 + shft: translate" );
        ImGui.Text( "mouse btn 1 + ctrl: rotate" );

        ImGui.PopItemWidth();

        ImGui.End();
    }

    public override void MouseDown( b2Vec2 p, int button, int mods )
    {
        if ( button == (int)MouseButton.Left )
        {
            m_startPosition = p;

            if ( mods == 0 )
            {
                m_rayStart = p;
                m_rayDrag = true;
            }
            else if ( mods == GLFW_MOD_SHIFT )
            {
                m_translating = true;
                m_basePosition = m_transform.p;
            }
            else if ( mods == GLFW_MOD_CONTROL )
            {
                m_rotating = true;
                m_baseAngle = m_angle;
            }
        }
    }

    public override void MouseUp( b2Vec2 _, int button )
    {
        if ( button == (int)MouseButton.Left )
        {
            m_rayDrag = false;
            m_rotating = false;
            m_translating = false;
        }
    }

    public override void MouseMove( b2Vec2 p )
    {
        if ( m_rayDrag )
        {
            m_rayEnd = p;
        }
        else if ( m_translating )
        {
            m_transform.p.x = m_basePosition.x + 0.5f * ( p.x - m_startPosition.x );
            m_transform.p.y = m_basePosition.y + 0.5f * ( p.y - m_startPosition.y );
        }
        else if ( m_rotating )
        {
            float dx = p.x - m_startPosition.x;
            m_angle = b2ClampFloat( m_baseAngle + 0.5f * dx, -B2_PI, B2_PI );
            m_transform.q = b2MakeRot( m_angle );
        }
    }

    void DrawRay( const b2CastOutput* output )
    {
        b2Vec2 p1 = m_rayStart;
        b2Vec2 p2 = m_rayEnd;
        b2Vec2 d = b2Sub( p2, p1 );

        if ( output->hit )
        {
            b2Vec2 p = b2MulAdd( p1, output->fraction, d );
            Draw.g_draw.DrawSegment( p1, p, b2HexColor.b2_colorWhite );
            Draw.g_draw.DrawPoint( p1, 5.0f, b2HexColor.b2_colorGreen );
            Draw.g_draw.DrawPoint( output->point, 5.0f, b2HexColor.b2_colorWhite );

            b2Vec2 n = b2MulAdd( p, 1.0f, output->normal );
            Draw.g_draw.DrawSegment( p, n, b2HexColor.b2_colorViolet );

            // if (m_rayRadius > 0.0f)
            //{
            //	Draw.g_draw.DrawCircle(p1, m_rayRadius, b2HexColor.b2_colorGreen);
            //	Draw.g_draw.DrawCircle(p, m_rayRadius, b2HexColor.b2_colorRed);
            // }

            if ( m_showFraction )
            {
                b2Vec2 ps = { p.x + 0.05f, p.y - 0.02f };
                Draw.g_draw.DrawString( ps, "%.2f", output->fraction );
            }
        }
        else
        {
            Draw.g_draw.DrawSegment( p1, p2, b2HexColor.b2_colorWhite );
            Draw.g_draw.DrawPoint( p1, 5.0f, b2HexColor.b2_colorGreen );
            Draw.g_draw.DrawPoint( p2, 5.0f, b2HexColor.b2_colorRed );

            // if (m_rayRadius > 0.0f)
            //{
            //	Draw.g_draw.DrawCircle(p1, m_rayRadius, b2HexColor.b2_colorGreen);
            //	Draw.g_draw.DrawCircle(p2, m_rayRadius, b2HexColor.b2_colorRed);
            // }
        }
    }

    public override void Step( Settings _ )
    {
        b2Vec2 offset = { -20.0f, 20.0f };
        b2Vec2 increment = { 10.0f, 0.0f };

        b2HexColor color1 = b2HexColor.b2_colorYellow;

        b2CastOutput output = { };
        float maxFraction = 1.0f;

        // circle
        {
            b2Transform transform = { b2Add( m_transform.p, offset ), m_transform.q };
            Draw.g_draw.DrawSolidCircle( transform, m_circle.center, m_circle.radius, color1 );

            b2Vec2 start = b2InvTransformPoint( transform, m_rayStart );
            b2Vec2 translation = b2InvRotateVector( transform.q, b2Sub( m_rayEnd, m_rayStart ) );
            b2RayCastInput input = { start, translation, maxFraction };

            b2CastOutput localOutput = b2RayCastCircle( &input, &m_circle );
            if ( localOutput.hit )
            {
                output = localOutput;
                output.point = b2TransformPoint( transform, localOutput.point );
                output.normal = b2RotateVector( transform.q, localOutput.normal );
                maxFraction = localOutput.fraction;
            }

            offset = b2Add( offset, increment );
        }

        // capsule
        {
            b2Transform transform = { b2Add( m_transform.p, offset ), m_transform.q };
            b2Vec2 v1 = b2TransformPoint( transform, m_capsule.center1 );
            b2Vec2 v2 = b2TransformPoint( transform, m_capsule.center2 );
            Draw.g_draw.DrawSolidCapsule( v1, v2, m_capsule.radius, color1 );

            b2Vec2 start = b2InvTransformPoint( transform, m_rayStart );
            b2Vec2 translation = b2InvRotateVector( transform.q, b2Sub( m_rayEnd, m_rayStart ) );
            b2RayCastInput input = { start, translation, maxFraction };

            b2CastOutput localOutput = b2RayCastCapsule( &input, &m_capsule );
            if ( localOutput.hit )
            {
                output = localOutput;
                output.point = b2TransformPoint( transform, localOutput.point );
                output.normal = b2RotateVector( transform.q, localOutput.normal );
                maxFraction = localOutput.fraction;
            }

            offset = b2Add( offset, increment );
        }

        // box
        {
            b2Transform transform = { b2Add( m_transform.p, offset ), m_transform.q };
            Draw.g_draw.DrawSolidPolygon( transform, m_box.vertices, m_box.count, 0.0f, color1 );

            b2Vec2 start = b2InvTransformPoint( transform, m_rayStart );
            b2Vec2 translation = b2InvRotateVector( transform.q, b2Sub( m_rayEnd, m_rayStart ) );
            b2RayCastInput input = { start, translation, maxFraction };

            b2CastOutput localOutput = b2RayCastPolygon( &input, &m_box );
            if ( localOutput.hit )
            {
                output = localOutput;
                output.point = b2TransformPoint( transform, localOutput.point );
                output.normal = b2RotateVector( transform.q, localOutput.normal );
                maxFraction = localOutput.fraction;
            }

            offset = b2Add( offset, increment );
        }

        // triangle
        {
            b2Transform transform = { b2Add( m_transform.p, offset ), m_transform.q };
            Draw.g_draw.DrawSolidPolygon( transform, m_triangle.vertices, m_triangle.count, 0.0f, color1 );

            b2Vec2 start = b2InvTransformPoint( transform, m_rayStart );
            b2Vec2 translation = b2InvRotateVector( transform.q, b2Sub( m_rayEnd, m_rayStart ) );
            b2RayCastInput input = { start, translation, maxFraction };

            b2CastOutput localOutput = b2RayCastPolygon( &input, &m_triangle );
            if ( localOutput.hit )
            {
                output = localOutput;
                output.point = b2TransformPoint( transform, localOutput.point );
                output.normal = b2RotateVector( transform.q, localOutput.normal );
                maxFraction = localOutput.fraction;
            }

            offset = b2Add( offset, increment );
        }

        // segment
        {
            b2Transform transform = { b2Add( m_transform.p, offset ), m_transform.q };

            b2Vec2 p1 = b2TransformPoint( transform, m_segment.point1 );
            b2Vec2 p2 = b2TransformPoint( transform, m_segment.point2 );
            Draw.g_draw.DrawSegment( p1, p2, color1 );

            b2Vec2 start = b2InvTransformPoint( transform, m_rayStart );
            b2Vec2 translation = b2InvRotateVector( transform.q, b2Sub( m_rayEnd, m_rayStart ) );
            b2RayCastInput input = { start, translation, maxFraction };

            b2CastOutput localOutput = b2RayCastSegment( &input, &m_segment, false );
            if ( localOutput.hit )
            {
                output = localOutput;
                output.point = b2TransformPoint( transform, localOutput.point );
                output.normal = b2RotateVector( transform.q, localOutput.normal );
                maxFraction = localOutput.fraction;
            }

            offset = b2Add( offset, increment );
        }

        DrawRay( &output );
    }

    static Sample Create( Settings settings )
    {
        return new RayCast( settings );
    }

    b2Polygon m_box;
    b2Polygon m_triangle;
    b2Circle m_circle;
    b2Capsule m_capsule;
    b2Segment m_segment;

    b2Transform m_transform;
    float m_angle;

    b2Vec2 m_rayStart;
    b2Vec2 m_rayEnd;

    b2Vec2 m_basePosition;
    float m_baseAngle;

    b2Vec2 m_startPosition;

    bool m_rayDrag;
    bool m_translating;
    bool m_rotating;
    bool m_showFraction;
    };

    static int sampleIndex = RegisterSample( "Collision", "Ray Cast", RayCast::Create );
