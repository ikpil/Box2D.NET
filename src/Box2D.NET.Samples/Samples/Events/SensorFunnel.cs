// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Primitives;
using Box2D.NET.Shared.Primitives;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Events;

public class SensorFunnel : Sample
{
enum ea
{
    e_donut = 1,
    e_human = 2,
    e_count = 32
};

Human m_humans[e_count];
Donut m_donuts[e_count];
bool m_isSpawned[e_count];
int m_type;
float m_wait;
float m_side;

static int sampleSensorBeginEvent = RegisterSample( "Events", "Sensor Funnel", Create );
static Sample Create( Settings settings )
{
    return new SensorFunnel( settings );
}


public SensorFunnel( Settings settings ) : base( settings )
{
    if ( settings.restart == false )
    {
        Draw.g_camera.m_center = { 0.0f, 0.0f };
        Draw.g_camera.m_zoom = 25.0f * 1.333f;
    }

    settings.drawJoints = false;

    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody( m_worldId, &bodyDef );

        // b2Vec2 points[] = {
        //{42.333, 44.979},	{177.271, 44.979},	{177.271, 100.542}, {142.875, 121.708}, {177.271, 121.708},
        //{177.271, 171.979}, {142.875, 193.146}, {177.271, 193.146}, {177.271, 222.250}, {124.354, 261.938},
        //{124.354, 293.688}, {95.250, 293.688},	{95.250, 261.938},	{42.333, 222.250},	{42.333, 193.146},
        //{76.729, 193.146},	{42.333, 171.979},	{42.333, 121.708},	{76.729, 121.708},	{42.333, 100.542},
        //};

        B2Vec2 points[] = {
            { -16.8672504, 31.088623 },	 { 16.8672485, 31.088623 },		{ 16.8672485, 17.1978741 },
            { 8.26824951, 11.906374 },	 { 16.8672485, 11.906374 },		{ 16.8672485, -0.661376953 },
            { 8.26824951, -5.953125 },	 { 16.8672485, -5.953125 },		{ 16.8672485, -13.229126 },
            { 3.63799858, -23.151123 },	 { 3.63799858, -31.088623 },	{ -3.63800049, -31.088623 },
            { -3.63800049, -23.151123 }, { -16.8672504, -13.229126 },	{ -16.8672504, -5.953125 },
            { -8.26825142, -5.953125 },	 { -16.8672504, -0.661376953 }, { -16.8672504, 11.906374 },
            { -8.26825142, 11.906374 },	 { -16.8672504, 17.1978741 },
        };

        int count = std::size( points );

        // float scale = 0.25f;
        // b2Vec2 lower = {float.MaxValue, float.MaxValue};
        // b2Vec2 upper = {-float.MaxValue, -float.MaxValue};
        // for (int i = 0; i < count; ++i)
        //{
        //	points[i].x = scale * points[i].x;
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

        B2SurfaceMaterial material = {};
        material.friction = 0.2f;

        B2ChainDef chainDef = b2DefaultChainDef();
        chainDef.points = points;
        chainDef.count = count;
        chainDef.isLoop = true;
        chainDef.materials = &material;
        chainDef.materialCount = 1;
        b2CreateChain( groundId, &chainDef );

        float sign = 1.0f;
        float y = 14.0f;
        for ( int i = 0; i < 3; ++i )
        {
            bodyDef.position = { 0.0f, y };
            bodyDef.type = B2BodyType.b2_dynamicBody;

            B2BodyId bodyId = b2CreateBody( m_worldId, &bodyDef );

            B2Polygon box = b2MakeBox( 6.0f, 0.5f );
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.1f;
            shapeDef.restitution = 1.0f;
            shapeDef.density = 1.0f;

            b2CreatePolygonShape( bodyId, shapeDef, box );

            B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
            revoluteDef.bodyIdA = groundId;
            revoluteDef.bodyIdB = bodyId;
            revoluteDef.localAnchorA = bodyDef.position;
            revoluteDef.localAnchorB = b2Vec2_zero;
            revoluteDef.maxMotorTorque = 200.0f;
            revoluteDef.motorSpeed = 2.0f * sign;
            revoluteDef.enableMotor = true;

            b2CreateRevoluteJoint( m_worldId, &revoluteDef );

            y -= 14.0f;
            sign = -sign;
        }

        {
            B2Polygon box = b2MakeOffsetBox( 4.0f, 1.0f, { 0.0f, -30.5f }, b2Rot_identity );
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.isSensor = true;
            b2CreatePolygonShape( groundId, &shapeDef, &box );
        }
    }

    m_wait = 0.5f;
    m_side = -15.0f;
    m_type = e_human;

    for ( int i = 0; i < e_count; ++i )
    {
        m_isSpawned[i] = false;
    }

    memset( m_humans, 0, sizeof( m_humans ) );

    CreateElement();
}

void CreateElement()
{
    int index = -1;
    for ( int i = 0; i < e_count; ++i )
    {
        if ( m_isSpawned[i] == false )
        {
            index = i;
            break;
        }
    }

    if ( index == -1 )
    {
        return;
    }

    B2Vec2 center = { m_side, 29.5f };

    if ( m_type == e_donut )
    {
        Donut* donut = m_donuts + index;
        // donut->Spawn(m_worldId, center, index + 1, donut);
        donut->Spawn( m_worldId, center, 1.0f, 0, donut );
    }
    else
    {
        Human* human = m_humans + index;
        float scale = 2.0f;
        float jointFriction = 0.05f;
        float jointHertz = 6.0f;
        float jointDamping = 0.5f;
        bool colorize = true;
        CreateHuman( human, m_worldId, center, scale, jointFriction, jointHertz, jointDamping, index + 1, human, colorize );
    }

    m_isSpawned[index] = true;
    m_side = -m_side;
}

void DestroyElement( int index )
{
    if ( m_type == e_donut )
    {
        Donut* donut = m_donuts + index;
        donut->Despawn();
    }
    else
    {
        Human* human = m_humans + index;
        DestroyHuman( human );
    }

    m_isSpawned[index] = false;
}

void Clear()
{
    for ( int i = 0; i < e_count; ++i )
    {
        if ( m_isSpawned[i] == true )
        {
            if ( m_type == e_donut )
            {
                m_donuts[i].Despawn();
            }
            else
            {
                DestroyHuman( m_humans + i );
            }

            m_isSpawned[i] = false;
        }
    }
}

public override void UpdateUI()
{
    float height = 90.0f;
    ImGui.SetNextWindowPos( new Vector2( 10.0f, Draw.g_camera.m_height - height - 50.0f ), ImGuiCond.Once );
    ImGui.SetNextWindowSize( new Vector2( 140.0f, height ) );

    ImGui.Begin( "Sensor Event", nullptr, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize );

    if ( ImGui.RadioButton( "donut", m_type == e_donut ) )
    {
        Clear();
        m_type = e_donut;
    }

    if ( ImGui.RadioButton( "human", m_type == e_human ) )
    {
        Clear();
        m_type = e_human;
    }

    ImGui.End();
}

public override void Step(Settings settings)
{
    if ( m_stepCount == 832 )
    {
        m_stepCount += 0;
    }

    base.Step( settings );

    // Discover rings that touch the bottom sensor
    bool deferredDestructions[e_count] = {};
    B2SensorEvents sensorEvents = b2World_GetSensorEvents( m_worldId );
    for ( int i = 0; i < sensorEvents.beginCount; ++i )
    {
        B2SensorBeginTouchEvent event = sensorEvents.beginEvents[i];
        B2ShapeId visitorId = event.visitorShapeId;
        B2BodyId bodyId = b2Shape_GetBody( visitorId );

        if ( m_type == e_donut )
        {
            Donut* donut = (Donut*)b2Body_GetUserData( bodyId );
            if ( donut != nullptr )
            {
                int index = (int)( donut - m_donuts );
                Debug.Assert( 0 <= index && index < e_count );

                // Defer destruction to avoid double destruction and event invalidation (orphaned shape ids)
                deferredDestructions[index] = true;
            }
        }
        else
        {
            Human* human = (Human*)b2Body_GetUserData( bodyId );
            if ( human != nullptr )
            {
                int index = (int)( human - m_humans );
                Debug.Assert( 0 <= index && index < e_count );

                // Defer destruction to avoid double destruction and event invalidation (orphaned shape ids)
                deferredDestructions[index] = true;
            }
        }
    }

    // todo destroy mouse joint if necessary

    // Safely destroy rings that hit the bottom sensor
    for ( int i = 0; i < e_count; ++i )
    {
        if ( deferredDestructions[i] )
        {
            DestroyElement( i );
        }
    }

    if ( settings.hertz > 0.0f && settings.pause == false )
    {
        m_wait -= 1.0f / settings.hertz;
        if ( m_wait < 0.0f )
        {
            CreateElement();
            m_wait += 0.5f;
        }
    }
}



}


