using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;


namespace Box2D.NET.Samples.Samples.Benchmarks;

enum QueryType
{
    e_rayCast,
    e_circleCast,
    e_overlap,
};

public class BenchmarkCast : Sample
{
BenchmarkCast( Settings settings ) : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 500.0f, 500.0f };
        Draw.g_camera.m_zoom = 25.0f * 21.0f;
        // settings.drawShapes = g_sampleDebug;
    }

    m_queryType = e_circleCast;
    m_ratio = 5.0f;
    m_grid = 1.0f;
    m_fill = 0.1f;
    m_rowCount = g_sampleDebug ? 100 : 1000;
    m_columnCount = g_sampleDebug ? 100 : 1000;
    m_minTime = 1e6f;
    m_drawIndex = 0;
    m_topDown = false;
    m_buildTime = 0.0f;
    m_radius = 0.1f;

    g_seed = 1234;
    int sampleCount = g_sampleDebug ? 100 : 10000;
    m_origins.resize( sampleCount );
    m_translations.resize( sampleCount );
    float extent = m_rowCount * m_grid;

    // Pre-compute rays to avoid randomizer overhead
    for ( int i = 0; i < sampleCount; ++i )
    {
        b2Vec2 rayStart = RandomVec2( 0.0f, extent );
        b2Vec2 rayEnd = RandomVec2( 0.0f, extent );

        m_origins[i] = rayStart;
        m_translations[i] = rayEnd - rayStart;
    }

    BuildScene();
}

void BuildScene()
{
    g_seed = 1234;
    b2DestroyWorld( m_worldId );
    b2WorldDef worldDef = b2DefaultWorldDef();
    m_worldId = b2CreateWorld( &worldDef );

    uint64_t ticks = b2GetTicks();

    b2BodyDef bodyDef = b2DefaultBodyDef();
    b2ShapeDef shapeDef = b2DefaultShapeDef();

    float y = 0.0f;

    for ( int i = 0; i < m_rowCount; ++i )
    {
        float x = 0.0f;

        for ( int j = 0; j < m_columnCount; ++j )
        {
            float fillTest = RandomFloatRange( 0.0f, 1.0f );
            if ( fillTest <= m_fill )
            {
                bodyDef.position = { x, y };
                b2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

                float ratio = RandomFloatRange( 1.0f, m_ratio );
                float halfWidth = RandomFloatRange( 0.05f, 0.25f );

                b2Polygon box;
                if ( RandomFloat() > 0.0f )
                {
                    box = b2MakeBox( ratio * halfWidth, halfWidth );
                }
                else
                {
                    box = b2MakeBox( halfWidth, ratio * halfWidth );
                }

                int category = RandomIntRange( 0, 2 );
                shapeDef.filter.categoryBits = 1 << category;
                if ( category == 0 )
                {
                    shapeDef.customColor = b2_colorBox2DBlue;
                }
                else if ( category == 1 )
                {
                    shapeDef.customColor = b2_colorBox2DYellow;
                }
                else
                {
                    shapeDef.customColor = b2_colorBox2DGreen;
                }

                b2CreatePolygonShape( bodyId, &shapeDef, &box );
            }

            x += m_grid;
        }

        y += m_grid;
    }

    if ( m_topDown )
    {
        b2World_RebuildStaticTree( m_worldId );
    }

    m_buildTime = b2GetMilliseconds( ticks );
    m_minTime = 1e6f;
}

void UpdateUI() override
{
    float height = 240.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 200.0f, height ) );

    ImGui.Begin( "Cast", nullptr, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize );

    ImGui.PushItemWidth( 100.0f );

    bool changed = false;

    const char* queryTypes[] = { "Ray", "Circle", "Overlap" };
    int queryType = int( m_queryType );
    if ( ImGui.Combo( "Query", &queryType, queryTypes, IM_ARRAYSIZE( queryTypes ) ) )
    {
        m_queryType = QueryType( queryType );
        if ( m_queryType == e_overlap )
        {
            m_radius = 5.0f;
        }
        else
        {
            m_radius = 0.1f;
        }

        changed = true;
    }

    if ( ImGui.SliderInt( "rows", &m_rowCount, 0, 1000, "%d" ) )
    {
        changed = true;
    }

    if ( ImGui.SliderInt( "columns", &m_columnCount, 0, 1000, "%d" ) )
    {
        changed = true;
    }

    if ( ImGui.SliderFloat( "fill", &m_fill, 0.0f, 1.0f, "%.2f" ) )
    {
        changed = true;
    }

    if ( ImGui.SliderFloat( "grid", &m_grid, 0.5f, 2.0f, "%.2f" ) )
    {
        changed = true;
    }

    if ( ImGui.SliderFloat( "ratio", &m_ratio, 1.0f, 10.0f, "%.2f" ) )
    {
        changed = true;
    }

    if ( ImGui.Checkbox( "top down", &m_topDown ) )
    {
        changed = true;
    }

    if ( ImGui.Button( "Draw Next" ) )
    {
        m_drawIndex = ( m_drawIndex + 1 ) % m_origins.size();
    }

    ImGui.PopItemWidth();
    ImGui.End();

    if ( changed )
    {
        BuildScene();
    }
}

struct CastResult
{
    b2Vec2 point;
    float fraction;
    bool hit;
};

static float CastCallback( b2ShapeId shapeId, b2Vec2 point, b2Vec2 normal, float fraction, void* context )
{
    CastResult* result = (CastResult*)context;
    result->point = point;
    result->fraction = fraction;
    result->hit = true;
    return fraction;
}

struct OverlapResult
{
    b2Vec2 points[32];
    int count;
};

static bool OverlapCallback( b2ShapeId shapeId, void* context )
{
    OverlapResult* result = (OverlapResult*)context;
    if ( result->count < 32 )
    {
        b2AABB aabb = b2Shape_GetAABB( shapeId );
        result->points[result->count] = b2AABB_Center( aabb );
        result->count += 1;
    }

    return true;
}

void Step( Settings settings ) override
{
    Sample::Step( settings );

    b2QueryFilter filter = b2DefaultQueryFilter();
    filter.maskBits = 1;
    int hitCount = 0;
    int nodeVisits = 0;
    int leafVisits = 0;
    float ms = 0.0f;
    int sampleCount = (int)m_origins.size();

    if ( m_queryType == e_rayCast )
    {
        uint64_t ticks = b2GetTicks();

        b2RayResult drawResult = {};

        for ( int i = 0; i < sampleCount; ++i )
        {
            b2Vec2 origin = m_origins[i];
            b2Vec2 translation = m_translations[i];

            b2RayResult result = b2World_CastRayClosest( m_worldId, origin, translation, filter );

            if ( i == m_drawIndex )
            {
                drawResult = result;
            }

            nodeVisits += result.nodeVisits;
            leafVisits += result.leafVisits;
            hitCount += result.hit ? 1 : 0;
        }

        ms = b2GetMilliseconds( ticks );

        m_minTime = b2MinFloat( m_minTime, ms );

        b2Vec2 p1 = m_origins[m_drawIndex];
        b2Vec2 p2 = p1 + m_translations[m_drawIndex];
        Draw.g_draw.DrawSegment( p1, p2, b2_colorWhite );
        Draw.g_draw.DrawPoint( p1, 5.0f, b2_colorGreen );
        Draw.g_draw.DrawPoint( p2, 5.0f, b2_colorRed );
        if ( drawResult.hit )
        {
            Draw.g_draw.DrawPoint( drawResult.point, 5.0f, b2_colorWhite );
        }
    }
    else if ( m_queryType == e_circleCast )
    {
        uint64_t ticks = b2GetTicks();

        b2Circle circle = { { 0.0f, 0.0f }, m_radius };
        CastResult drawResult = {};

        for ( int i = 0; i < sampleCount; ++i )
        {
            b2Transform origin = { m_origins[i], { 1.0f, 0.0f } };
            b2Vec2 translation = m_translations[i];

            CastResult result;
            b2TreeStats traversalResult =
                b2World_CastCircle( m_worldId, &circle, origin, translation, filter, CastCallback, &result );

            if ( i == m_drawIndex )
            {
                drawResult = result;
            }

            nodeVisits += traversalResult.nodeVisits;
            leafVisits += traversalResult.leafVisits;
            hitCount += result.hit ? 1 : 0;
        }

        ms = b2GetMilliseconds( ticks );

        m_minTime = b2MinFloat( m_minTime, ms );

        b2Vec2 p1 = m_origins[m_drawIndex];
        b2Vec2 p2 = p1 + m_translations[m_drawIndex];
        Draw.g_draw.DrawSegment( p1, p2, b2_colorWhite );
        Draw.g_draw.DrawPoint( p1, 5.0f, b2_colorGreen );
        Draw.g_draw.DrawPoint( p2, 5.0f, b2_colorRed );
        if ( drawResult.hit )
        {
            b2Vec2 t = b2Lerp( p1, p2, drawResult.fraction );
            Draw.g_draw.DrawCircle( t, m_radius, b2_colorWhite );
            Draw.g_draw.DrawPoint( drawResult.point, 5.0f, b2_colorWhite );
        }
    }
    else if ( m_queryType == e_overlap )
    {
        uint64_t ticks = b2GetTicks();

        OverlapResult drawResult = {};
        b2Vec2 extent = { m_radius, m_radius };
        OverlapResult result = {};

        for ( int i = 0; i < sampleCount; ++i )
        {
            b2Vec2 origin = m_origins[i];
            b2AABB aabb = { origin - extent, origin + extent };

            result.count = 0;
            b2TreeStats traversalResult = b2World_OverlapAABB( m_worldId, aabb, filter, OverlapCallback, &result );

            if ( i == m_drawIndex )
            {
                drawResult = result;
            }

            nodeVisits += traversalResult.nodeVisits;
            leafVisits += traversalResult.leafVisits;
            hitCount += result.count;
        }

        ms = b2GetMilliseconds( ticks );

        m_minTime = b2MinFloat( m_minTime, ms );

        b2Vec2 origin = m_origins[m_drawIndex];
        b2AABB aabb = { origin - extent, origin + extent };

        Draw.g_draw.DrawAABB( aabb, b2_colorWhite );

        for ( int i = 0; i < drawResult.count; ++i )
        {
            Draw.g_draw.DrawPoint( drawResult.points[i], 5.0f, b2_colorHotPink );
        }
    }

    Draw.g_draw.DrawString( 5, m_textLine, "build time ms = %g", m_buildTime );
    m_textLine += m_textIncrement;

    Draw.g_draw.DrawString( 5, m_textLine, "hit count = %d, node visits = %d, leaf visits = %d", hitCount, nodeVisits,
        leafVisits );
    m_textLine += m_textIncrement;

    Draw.g_draw.DrawString( 5, m_textLine, "total ms = %.3f", ms );
    m_textLine += m_textIncrement;

    Draw.g_draw.DrawString( 5, m_textLine, "min total ms = %.3f", m_minTime );
    m_textLine += m_textIncrement;

    float aveRayCost = 1000.0f * m_minTime / float( sampleCount );
    Draw.g_draw.DrawString( 5, m_textLine, "average us = %.2f", aveRayCost );
    m_textLine += m_textIncrement;
}

static Sample* Create( Settings settings )
{
    return new BenchmarkCast( settings );
}

QueryType m_queryType;

std::vector<b2Vec2> m_origins;
std::vector<b2Vec2> m_translations;
float m_minTime;
float m_buildTime;

int m_rowCount, m_columnCount;
int m_updateType;
int m_drawIndex;
float m_radius;
float m_fill;
float m_ratio;
float m_grid;
bool m_topDown;
};

static int sampleCast = RegisterSample( "Benchmark", "Cast", BenchmarkCast::Create );
