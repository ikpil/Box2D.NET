using System.Diagnostics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Stackings;

public class VerticalStack : Sample
{
    public const int e_maxColumns = 10;
    public const int e_maxRows = 15;
    public const int e_maxBullets = 8;

    enum ShapeType
    {
        e_circleShape = 0,
        e_boxShape
    };

    b2BodyId m_bullets[e_maxBullets];
    b2BodyId m_bodies[e_maxRows * e_maxColumns];
    int m_columnCount;
    int m_rowCount;
    int m_bulletCount;
    ShapeType m_shapeType;
    ShapeType m_bulletType;

    static int sampleVerticalStack = RegisterSample( "Stacking", "Vertical Stack", VerticalStack::Create );
    static Sample Create( Settings settings )
    {
        return new VerticalStack( settings );
    }


    public VerticalStack( Settings settings )
        : base( settings )
    {
        if ( settings.restart == false )
        {
            Draw.g_camera.m_center = { -7.0f, 9.0f };
            Draw.g_camera.m_zoom = 14.0f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = { 0.0f, -1.0f };
            b2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

            b2Polygon box = b2MakeBox( 100.0f, 1.0f );
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape( groundId, &shapeDef, &box );

            b2Segment segment = { { 10.0f, 1.0f }, { 10.0f, 21.0f } };
            b2CreateSegmentShape( groundId, &shapeDef, &segment );
        }

        for ( int i = 0; i < e_maxRows * e_maxColumns; ++i )
        {
            m_bodies[i] = b2_nullBodyId;
        }

        for ( int i = 0; i < e_maxBullets; ++i )
        {
            m_bullets[i] = b2_nullBodyId;
        }

        m_shapeType = e_boxShape;
        m_rowCount = e_maxRows;
        m_columnCount = 5;
        m_bulletCount = 1;
        m_bulletType = e_circleShape;

        CreateStacks();
    }

    void CreateStacks()
    {
        for ( int i = 0; i < e_maxRows * e_maxColumns; ++i )
        {
            if ( B2_IS_NON_NULL( m_bodies[i] ) )
            {
                b2DestroyBody( m_bodies[i] );
                m_bodies[i] = b2_nullBodyId;
            }
        }

        b2Circle circle = { };
        circle.radius = 0.5f;

        b2Polygon box = b2MakeBox( 0.5f, 0.5f );
        // b2Polygon box = b2MakeRoundedBox(0.45f, 0.45f, 0.05f);

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.friction = 0.3f;

        float offset;

        if ( m_shapeType == e_circleShape )
        {
            offset = 0.0f;
        }
        else
        {
            offset = 0.01f;
        }

        float dx = -3.0f;
        float xroot = 8.0f;

        for ( int j = 0; j < m_columnCount; ++j )
        {
            float x = xroot + j * dx;

            for ( int i = 0; i < m_rowCount; ++i )
            {
                b2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = b2BodyType.b2_dynamicBody;

                int n = j * m_rowCount + i;

                float shift = ( i % 2 == 0 ? -offset : offset );
                bodyDef.position = { x + shift, 0.5f + 1.0f * i };
                // bodyDef.position = {x + shift, 1.0f + 1.51f * i};
                b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

                m_bodies[n] = bodyId;

                if ( m_shapeType == e_circleShape )
                {
                    b2CreateCircleShape( bodyId, &shapeDef, &circle );
                }
                else
                {
                    b2CreatePolygonShape( bodyId, &shapeDef, &box );
                }
            }
        }
    }

    void DestroyBody()
    {
        for ( int j = 0; j < m_columnCount; ++j )
        {
            for ( int i = 0; i < m_rowCount; ++i )
            {
                int n = j * m_rowCount + i;

                if ( B2_IS_NON_NULL( m_bodies[n] ) )
                {
                    b2DestroyBody( m_bodies[n] );
                    m_bodies[n] = b2_nullBodyId;
                    break;
                }
            }
        }
    }

    void DestroyBullets()
    {
        for ( int i = 0; i < e_maxBullets; ++i )
        {
            b2BodyId bullet = m_bullets[i];

            if ( B2_IS_NON_NULL( bullet ) )
            {
                b2DestroyBody( bullet );
                m_bullets[i] = b2_nullBodyId;
            }
        }
    }

    void FireBullets()
    {
        b2Circle circle = { { 0.0f, 0.0f }, 0.25f };
        b2Polygon box = b2MakeBox( 0.25f, 0.25f );

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 4.0f;

        for ( int i = 0; i < m_bulletCount; ++i )
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = { -25.0f - i, 6.0f };
            float speed = RandomFloatRange( 200.0f, 300.0f );
            bodyDef.linearVelocity = { speed, 0.0f };
            bodyDef.isBullet = true;

            b2BodyId bullet = b2CreateBody( m_worldId, &bodyDef );

            if ( m_bulletType == e_boxShape )
            {
                b2CreatePolygonShape( bullet, &shapeDef, &box );
            }
            else
            {
                b2CreateCircleShape( bullet, &shapeDef, &circle );
            }
            Debug.Assert( B2_IS_NULL( m_bullets[i] ) );
            m_bullets[i] = bullet;
        }
    }

    public override void UpdateUI()
    {
        float height = 230.0f;
        ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
        ImGui.SetNextWindowSize( new Vector2( 240.0f, height ) );

        ImGui.Begin( "Vertical Stack", nullptr, ImGuiWindowFlags.NoResize );

        ImGui.PushItemWidth( 120.0f );

        bool changed = false;
        const char* shapeTypes[] = { "Circle", "Box" };

        int shapeType = int( m_shapeType );
        changed = changed || ImGui.Combo( "Shape", &shapeType, shapeTypes, IM_ARRAYSIZE( shapeTypes ) );
        m_shapeType = ShapeType( shapeType );

        changed = changed || ImGui.SliderInt( "Rows", &m_rowCount, 1, e_maxRows );
        changed = changed || ImGui.SliderInt( "Columns", &m_columnCount, 1, e_maxColumns );

        ImGui.SliderInt( "Bullets", &m_bulletCount, 1, e_maxBullets );

        int bulletType = int( m_bulletType );
        ImGui.Combo( "Bullet Shape", &bulletType, shapeTypes, IM_ARRAYSIZE( shapeTypes ) );
        m_bulletType = ShapeType( bulletType );

        ImGui.PopItemWidth();

        if ( ImGui.Button( "Fire Bullets" ) || glfwGetKey( g_mainWindow, GLFW_KEY_B ) == GLFW_PRESS )
        {
            DestroyBullets();
            FireBullets();
        }

        if ( ImGui.Button( "Destroy Body" ) )
        {
            DestroyBody();
        }

        changed = changed || ImGui.Button( "Reset Stack" );

        if ( changed )
        {
            DestroyBullets();
            CreateStacks();
        }

        ImGui.End();
    }
}
