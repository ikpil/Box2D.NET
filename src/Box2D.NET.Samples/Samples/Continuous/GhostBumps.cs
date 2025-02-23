﻿using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Continuous;

// This sample shows ghost bumps
class GhostBumps : Sample
{
public:
enum ShapeType
{
    e_circleShape = 0,
    e_capsuleShape,
    e_boxShape
};

explicit GhostBumps( Settings& settings )
    : Sample( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 1.5f, 16.0f };
        Draw.g_camera.m_zoom = 25.0f * 0.8f;
    }

    m_groundId = b2_nullBodyId;
    m_bodyId = b2_nullBodyId;
    m_shapeId = b2_nullShapeId;
    m_shapeType = e_circleShape;
    m_round = 0.0f;
    m_friction = 0.2f;
    m_bevel = 0.0f;
    m_useChain = true;

    CreateScene();
    Launch();
}

void CreateScene()
{
    if ( B2_IS_NON_NULL( m_groundId ) )
    {
        b2DestroyBody( m_groundId );
    }

    m_shapeId = b2_nullShapeId;

    b2BodyDef bodyDef = b2DefaultBodyDef();
    m_groundId = b2CreateBody( m_worldId, &bodyDef );

    float m = 1.0f / sqrt( 2.0f );
    float mm = 2.0f * ( sqrt( 2.0f ) - 1.0f );
    float hx = 4.0f, hy = 0.25f;

    if ( m_useChain )
    {
        b2Vec2 points[20];
        points[0] = { -3.0f * hx, hy };
        points[1] = b2Add( points[0], { -2.0f * hx * m, 2.0f * hx * m } );
        points[2] = b2Add( points[1], { -2.0f * hx * m, 2.0f * hx * m } );
        points[3] = b2Add( points[2], { -2.0f * hx * m, 2.0f * hx * m } );
        points[4] = b2Add( points[3], { -2.0f * hy * m, -2.0f * hy * m } );
        points[5] = b2Add( points[4], { 2.0f * hx * m, -2.0f * hx * m } );
        points[6] = b2Add( points[5], { 2.0f * hx * m, -2.0f * hx * m } );
        points[7] =
            b2Add( points[6], { 2.0f * hx * m + 2.0f * hy * ( 1.0f - m ), -2.0f * hx * m - 2.0f * hy * ( 1.0f - m ) } );
        points[8] = b2Add( points[7], { 2.0f * hx + hy * mm, 0.0f } );
        points[9] = b2Add( points[8], { 2.0f * hx, 0.0f } );
        points[10] = b2Add( points[9], { 2.0f * hx + hy * mm, 0.0f } );
        points[11] =
            b2Add( points[10], { 2.0f * hx * m + 2.0f * hy * ( 1.0f - m ), 2.0f * hx * m + 2.0f * hy * ( 1.0f - m ) } );
        points[12] = b2Add( points[11], { 2.0f * hx * m, 2.0f * hx * m } );
        points[13] = b2Add( points[12], { 2.0f * hx * m, 2.0f * hx * m } );
        points[14] = b2Add( points[13], { -2.0f * hy * m, 2.0f * hy * m } );
        points[15] = b2Add( points[14], { -2.0f * hx * m, -2.0f * hx * m } );
        points[16] = b2Add( points[15], { -2.0f * hx * m, -2.0f * hx * m } );
        points[17] = b2Add( points[16], { -2.0f * hx * m, -2.0f * hx * m } );
        points[18] = b2Add( points[17], { -2.0f * hx, 0.0f } );
        points[19] = b2Add( points[18], { -2.0f * hx, 0.0f } );

        b2SurfaceMaterial material = {};
        material.friction = m_friction;

        b2ChainDef chainDef = b2DefaultChainDef();
        chainDef.points = points;
        chainDef.count = 20;
        chainDef.isLoop = true;
        chainDef.materials = &material;
        chainDef.materialCount = 1;

        b2CreateChain( m_groundId, &chainDef );
    }
    else
    {
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.friction = m_friction;

        b2Hull hull = { };

        if ( m_bevel > 0.0f )
        {
            float hb = m_bevel;
            b2Vec2 vs[8] = { { hx + hb, hy - 0.05f },	{ hx, hy },	  { -hx, hy }, { -hx - hb, hy - 0.05f },
                { -hx - hb, -hy + 0.05f }, { -hx, -hy }, { hx, -hy }, { hx + hb, -hy + 0.05f } };
            hull = b2ComputeHull( vs, 8 );
        }
        else
        {
            b2Vec2 vs[4] = { { hx, hy }, { -hx, hy }, { -hx, -hy }, { hx, -hy } };
            hull = b2ComputeHull( vs, 4 );
        }

        b2Transform transform;
        float x, y;

        // Left slope
        x = -3.0f * hx - m * hx - m * hy;
        y = hy + m * hx - m * hy;
        transform.q = b2MakeRot( -0.25f * B2_PI );

        {
            transform.p = { x, y };
            b2Polygon polygon = b2MakeOffsetPolygon( &hull, transform.p, transform.q );
            b2CreatePolygonShape( m_groundId, &shapeDef, &polygon );
            x -= 2.0f * m * hx;
            y += 2.0f * m * hx;
        }
        {
            transform.p = { x, y };
            b2Polygon polygon = b2MakeOffsetPolygon( &hull, transform.p, transform.q );
            b2CreatePolygonShape( m_groundId, &shapeDef, &polygon );
            x -= 2.0f * m * hx;
            y += 2.0f * m * hx;
        }
        {
            transform.p = { x, y };
            b2Polygon polygon = b2MakeOffsetPolygon( &hull, transform.p, transform.q );
            b2CreatePolygonShape( m_groundId, &shapeDef, &polygon );
            x -= 2.0f * m * hx;
            y += 2.0f * m * hx;
        }

        x = -2.0f * hx;
        y = 0.0f;
        transform.q = b2MakeRot( 0.0f );

        {
            transform.p = { x, y };
            b2Polygon polygon = b2MakeOffsetPolygon( &hull, transform.p, transform.q );
            b2CreatePolygonShape( m_groundId, &shapeDef, &polygon );
            x += 2.0f * hx;
        }
        {
            transform.p = { x, y };
            b2Polygon polygon = b2MakeOffsetPolygon( &hull, transform.p, transform.q );
            b2CreatePolygonShape( m_groundId, &shapeDef, &polygon );
            x += 2.0f * hx;
        }
        {
            transform.p = { x, y };
            b2Polygon polygon = b2MakeOffsetPolygon( &hull, transform.p, transform.q );
            b2CreatePolygonShape( m_groundId, &shapeDef, &polygon );
            x += 2.0f * hx;
        }

        x = 3.0f * hx + m * hx + m * hy;
        y = hy + m * hx - m * hy;
        transform.q = b2MakeRot( 0.25f * B2_PI );

        {
            transform.p = { x, y };
            b2Polygon polygon = b2MakeOffsetPolygon( &hull, transform.p, transform.q );
            b2CreatePolygonShape( m_groundId, &shapeDef, &polygon );
            x += 2.0f * m * hx;
            y += 2.0f * m * hx;
        }
        {
            transform.p = { x, y };
            b2Polygon polygon = b2MakeOffsetPolygon( &hull, transform.p, transform.q );
            b2CreatePolygonShape( m_groundId, &shapeDef, &polygon );
            x += 2.0f * m * hx;
            y += 2.0f * m * hx;
        }
        {
            transform.p = { x, y };
            b2Polygon polygon = b2MakeOffsetPolygon( &hull, transform.p, transform.q );
            b2CreatePolygonShape( m_groundId, &shapeDef, &polygon );
            x += 2.0f * m * hx;
            y += 2.0f * m * hx;
        }
    }
}

void Launch()
{
    if ( B2_IS_NON_NULL( m_bodyId ) )
    {
        b2DestroyBody( m_bodyId );
        m_shapeId = b2_nullShapeId;
    }

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;
    bodyDef.position = { -28.0f, 18.0f };
    bodyDef.linearVelocity = { 0.0f, 0.0f };
    m_bodyId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    shapeDef.density = 1.0f;
    shapeDef.friction = m_friction;

    if ( m_shapeType == e_circleShape )
    {
        b2Circle circle = { { 0.0f, 0.0f }, 0.5f };
        m_shapeId = b2CreateCircleShape( m_bodyId, &shapeDef, &circle );
    }
    else if ( m_shapeType == e_capsuleShape )
    {
        b2Capsule capsule = { { -0.5f, 0.0f }, { 0.5f, 0.0 }, 0.25f };
        m_shapeId = b2CreateCapsuleShape( m_bodyId, &shapeDef, &capsule );
    }
    else
    {
        float h = 0.5f - m_round;
        b2Polygon box = b2MakeRoundedBox( h, 2.0f * h, m_round );
        m_shapeId = b2CreatePolygonShape( m_bodyId, &shapeDef, &box );
    }
}

void UpdateUI() override
{
    float height = 140.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 180.0f, height ) );

    ImGui.Begin( "Ghost Bumps", nullptr, ImGuiWindowFlags.NoResize );
    ImGui.PushItemWidth( 100.0f );

    if ( ImGui.Checkbox( "Chain", &m_useChain ) )
    {
        CreateScene();
    }

    if ( m_useChain == false )
    {
        if ( ImGui.SliderFloat( "Bevel", &m_bevel, 0.0f, 1.0f, "%.2f" ) )
        {
            CreateScene();
        }
    }

    {
        const char* shapeTypes[] = { "Circle", "Capsule", "Box" };
        int shapeType = int( m_shapeType );
        ImGui.Combo( "Shape", &shapeType, shapeTypes, IM_ARRAYSIZE( shapeTypes ) );
        m_shapeType = ShapeType( shapeType );
    }

    if ( m_shapeType == e_boxShape )
    {
        ImGui.SliderFloat( "Round", &m_round, 0.0f, 0.4f, "%.1f" );
    }

    if ( ImGui.SliderFloat( "Friction", &m_friction, 0.0f, 1.0f, "%.1f" ) )
    {
        if ( B2_IS_NON_NULL( m_shapeId ) )
        {
            b2Shape_SetFriction( m_shapeId, m_friction );
        }

        CreateScene();
    }

    if ( ImGui.Button( "Launch" ) )
    {
        Launch();
    }

    ImGui.PopItemWidth();
    ImGui.End();
}

static Sample* Create( Settings& settings )
{
    return new GhostBumps( settings );
}

b2BodyId m_groundId;
b2BodyId m_bodyId;
b2ShapeId m_shapeId;
ShapeType m_shapeType;
float m_round;
float m_friction;
float m_bevel;
bool m_useChain;
};

static int sampleGhostCollision = RegisterSample( "Continuous", "Ghost Bumps", GhostBumps::Create );
