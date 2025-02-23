using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;

public class ChainShape : Sample
{
    
    
enum ShapeType
{
    e_circleShape = 0,
    e_capsuleShape,
    e_boxShape
};

b2BodyId m_groundId;
b2BodyId m_bodyId;
b2ChainId m_chainId;
ShapeType m_shapeType;
b2ShapeId m_shapeId;
float m_restitution;
float m_friction;

static int sampleChainShape = RegisterSample( "Shapes", "Chain Shape", Create );
static Sample Create( Settings settings )
{
    return new ChainShape( settings );
}


public ChainShape( Settings settings )
    : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 0.0f };
        Draw.g_camera.m_zoom = 25.0f * 1.75f;
    }

    m_groundId = b2_nullBodyId;
    m_bodyId = b2_nullBodyId;
    m_chainId = b2_nullChainId;
    m_shapeId = b2_nullShapeId;
    m_shapeType = e_circleShape;
    m_restitution = 0.0f;
    m_friction = 0.2f;

    CreateScene();
    Launch();
}

void CreateScene()
{
    if ( B2_IS_NON_NULL( m_groundId ) )
    {
        b2DestroyBody( m_groundId );
    }

    // https://betravis.github.io/shape-tools/path-to-polygon/
    // b2Vec2 points[] = {{-20.58325, 14.54175}, {-21.90625, 15.8645},		 {-24.552, 17.1875},
    //				   {-27.198, 11.89575},	  {-29.84375, 15.8645},		 {-29.84375, 21.15625},
    //				   {-25.875, 23.802},	  {-20.58325, 25.125},		 {-25.875, 29.09375},
    //				   {-20.58325, 31.7395},  {-11.0089998, 23.2290001}, {-8.67700005, 21.15625},
    //				   {-6.03125, 21.15625},  {-7.35424995, 29.09375},	 {-3.38549995, 29.09375},
    //				   {1.90625, 30.41675},	  {5.875, 17.1875},			 {11.16675, 25.125},
    //				   {9.84375, 29.09375},	  {13.8125, 31.7395},		 {21.75, 30.41675},
    //				   {28.3644981, 26.448},  {25.71875, 18.5105},		 {24.3957481, 13.21875},
    //				   {17.78125, 11.89575},  {15.1355, 7.92700005},	 {5.875, 9.25},
    //				   {1.90625, 11.89575},	  {-3.25, 11.89575},		 {-3.25, 9.9375},
    //				   {-4.70825005, 9.25},	  {-8.67700005, 9.25},		 {-11.323, 11.89575},
    //				   {-13.96875, 11.89575}, {-15.29175, 14.54175},	 {-19.2605, 14.54175}};

    b2Vec2 points[] = {
        { -56.885498, 12.8985004 },	  { -56.885498, 16.2057495 },	{ 56.885498, 16.2057495 },	 { 56.885498, -16.2057514 },
        { 51.5935059, -16.2057514 },  { 43.6559982, -10.9139996 },	{ 35.7184982, -10.9139996 }, { 27.7809982, -10.9139996 },
        { 21.1664963, -14.2212505 },  { 11.9059982, -16.2057514 },	{ 0, -16.2057514 },			 { -10.5835037, -14.8827496 },
        { -17.1980019, -13.5597477 }, { -21.1665001, -12.2370014 }, { -25.1355019, -9.5909977 }, { -31.75, -3.63799858 },
        { -38.3644981, 6.2840004 },	  { -42.3334999, 9.59125137 },	{ -47.625, 11.5755005 },	 { -56.885498, 12.8985004 },
    };

    int count = sizeof( points ) / sizeof( points[0] );

    // float scale = 0.25f;
    // b2Vec2 lower = {float.MaxValue, float.MaxValue};
    // b2Vec2 upper = {-float.MaxValue, -float.MaxValue};
    // for (int i = 0; i < count; ++i)
    //{
    //	points[i].x = 2.0f * scale * points[i].x;
    //	points[i].y = -scale * points[i].y;

    //	lower = b2Min(lower, points[i]);
    //	upper = b2Max(upper, points[i]);
    //}

    // b2Vec2 center = b2MulSV(0.5f, b2Add(lower, upper));
    // for (int i = 0; i < count; ++i)
    //{
    //	points[i] = b2Sub(points[i], center);
    // }

    // for (int i = 0; i < count / 2; ++i)
    //{
    //	b2Vec2 temp = points[i];
    //	points[i] = points[count - 1 - i];
    //	points[count - 1 - i] = temp;
    // }

    // printf("{");
    // for (int i = 0; i < count; ++i)
    //{
    //	printf("{%.9g, %.9g},", points[i].x, points[i].y);
    // }
    // printf("};\n");

    b2SurfaceMaterial material = {};
    material.friction = 0.2f;
    material.customColor = b2HexColor.b2_colorSteelBlue;
    material.material = 42;

    b2ChainDef chainDef = b2DefaultChainDef();
    chainDef.points = points;
    chainDef.count = count;
    chainDef.materials = &material;
    chainDef.materialCount = 1;
    chainDef.isLoop = true;

    b2BodyDef bodyDef = b2DefaultBodyDef();
    m_groundId = b2CreateBody( m_worldId, &bodyDef );

    m_chainId = b2CreateChain( m_groundId, &chainDef );
}

void Launch()
{
    if ( B2_IS_NON_NULL( m_bodyId ) )
    {
        b2DestroyBody( m_bodyId );
    }

    b2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = b2BodyType.b2_dynamicBody;
    bodyDef.position = { -55.0f, 13.5f };
    m_bodyId = b2CreateBody( m_worldId, &bodyDef );

    b2ShapeDef shapeDef = b2DefaultShapeDef();
    shapeDef.density = 1.0f;
    shapeDef.friction = m_friction;
    shapeDef.restitution = m_restitution;

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
        float h = 0.5f;
        b2Polygon box = b2MakeBox( h, h );
        m_shapeId = b2CreatePolygonShape( m_bodyId, &shapeDef, &box );
    }

#if DEBUG
    b2_toiCalls = 0;
    b2_toiHitCount = 0;
#endif

    m_stepCount = 0;
}

public override void UpdateUI()
{
    float height = 155.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 240.0f, height ) );

    ImGui.Begin( "Chain Shape", nullptr, ImGuiWindowFlags.NoResize );

    const char* shapeTypes[] = { "Circle", "Capsule", "Box" };
    int shapeType = int( m_shapeType );
    if ( ImGui.Combo( "Shape", &shapeType, shapeTypes, IM_ARRAYSIZE( shapeTypes ) ) )
    {
        m_shapeType = ShapeType( shapeType );
        Launch();
    }

    if ( ImGui.SliderFloat( "Friction", &m_friction, 0.0f, 1.0f, "%.2f" ) )
    {
        b2Shape_SetFriction( m_shapeId, m_friction );
        b2Chain_SetFriction( m_chainId, m_friction );
    }

    if ( ImGui.SliderFloat( "Restitution", &m_restitution, 0.0f, 2.0f, "%.1f" ) )
    {
        b2Shape_SetRestitution( m_shapeId, m_restitution );
    }

    if ( ImGui.Button( "Launch" ) )
    {
        Launch();
    }

    ImGui.End();
}

public override void Step(Settings settings)
{
    base.Step( settings );

    Draw.g_draw.DrawSegment( b2Vec2_zero, { 0.5f, 0.0f }, b2HexColor.b2_colorRed );
    Draw.g_draw.DrawSegment( b2Vec2_zero, { 0.0f, 0.5f }, b2HexColor.b2_colorGreen );

#if DEBUG
    DrawTextLine( "toi calls, hits = %d, %d", b2_toiCalls, b2_toiHitCount );
#endif
}



}

