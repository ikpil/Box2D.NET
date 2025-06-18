// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Samples.Primitives;
using Box2D.NET.Shared;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.Shared.Humans;

namespace Box2D.NET.Samples.Samples.Events;

public class SensorFunnel : Sample
{
    private static readonly int SampleSensorBeginEvent = SampleFactory.Shared.RegisterSample("Events", "Sensor Funnel", Create);

    private enum ea
    {
        e_donut = 1,
        e_human = 2,
        e_count = 32
    };

    private Human[] m_humans = new Human[(int)ea.e_count];
    private Donut[] m_donuts = new Donut[(int)ea.e_count];
    private bool[] m_isSpawned = new bool[(int)ea.e_count];
    private int m_type;
    private float m_wait;
    private float m_side;

    private static Sample Create(SampleContext context)
    {
        return new SensorFunnel(context);
    }


    public SensorFunnel(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 0.0f);
            m_camera.m_zoom = 25.0f * 1.333f;
        }

        m_context.settings.drawJoints = false;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            // B2Vec2 points[] = {
            //{42.333, 44.979},	{177.271, 44.979},	{177.271, 100.542}, {142.875, 121.708}, {177.271, 121.708},
            //{177.271, 171.979}, {142.875, 193.146}, {177.271, 193.146}, {177.271, 222.250}, {124.354, 261.938},
            //{124.354, 293.688}, {95.250, 293.688},	{95.250, 261.938},	{42.333, 222.250},	{42.333, 193.146},
            //{76.729, 193.146},	{42.333, 171.979},	{42.333, 121.708},	{76.729, 121.708},	{42.333, 100.542},
            //};

            B2Vec2[] points = new B2Vec2[]
            {
                new B2Vec2(-16.8672504f, 31.088623f), new B2Vec2(16.8672485f, 31.088623f), new B2Vec2(16.8672485f, 17.1978741f),
                new B2Vec2(8.26824951f, 11.906374f), new B2Vec2(16.8672485f, 11.906374f), new B2Vec2(16.8672485f, -0.661376953f),
                new B2Vec2(8.26824951f, -5.953125f), new B2Vec2(16.8672485f, -5.953125f), new B2Vec2(16.8672485f, -13.229126f),
                new B2Vec2(3.63799858f, -23.151123f), new B2Vec2(3.63799858f, -31.088623f), new B2Vec2(-3.63800049f, -31.088623f),
                new B2Vec2(-3.63800049f, -23.151123f), new B2Vec2(-16.8672504f, -13.229126f), new B2Vec2(-16.8672504f, -5.953125f),
                new B2Vec2(-8.26825142f, -5.953125f), new B2Vec2(-16.8672504f, -0.661376953f), new B2Vec2(-16.8672504f, 11.906374f),
                new B2Vec2(-8.26825142f, 11.906374f), new B2Vec2(-16.8672504f, 17.1978741f),
            };

            int count = points.Length;

            // float scale = 0.25f;
            // B2Vec2 lower = {float.MaxValue, float.MaxValue};
            // B2Vec2 upper = {-float.MaxValue, -float.MaxValue};
            // for (int i = 0; i < count; ++i)
            //{
            //	points[i].x = scale * points[i].x;
            //	points[i].y = -scale * points[i].y;

            //	lower = b2Min(lower, points[i]);
            //	upper = b2Max(upper, points[i]);
            //}

            // B2Vec2 center = b2MulSV(0.5f, b2Add(lower, upper));
            // for (int i = 0; i < count; ++i)
            //{
            //	points[i] = b2Sub(points[i], center);
            // }

            // for (int i = 0; i < count / 2; ++i)
            //{
            //	B2Vec2 temp = points[i];
            //	points[i] = points[count - 1 - i];
            //	points[count - 1 - i] = temp;
            // }

            // Logger.Information("{");
            // for (int i = 0; i < count; ++i)
            //{
            //	Logger.Information("{%.9g, %.9g},", points[i].x, points[i].y);
            // }
            // Logger.Information("};\n");

            B2SurfaceMaterial material = new B2SurfaceMaterial();
            material.friction = 0.2f;

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = count;
            chainDef.isLoop = true;
            chainDef.materials = [material];
            chainDef.materialCount = 1;
            b2CreateChain(groundId, ref chainDef);

            float sign = 1.0f;
            float y = 14.0f;
            for (int i = 0; i < 3; ++i)
            {
                bodyDef.position = new B2Vec2(0.0f, y);
                bodyDef.type = B2BodyType.b2_dynamicBody;

                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

                B2Polygon box = b2MakeBox(6.0f, 0.5f);
                B2ShapeDef shapeDef = b2DefaultShapeDef();
                shapeDef.material.friction = 0.1f;
                shapeDef.material.restitution = 1.0f;
                shapeDef.density = 1.0f;

                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

                B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
                revoluteDef.@base.bodyIdA = groundId;
                revoluteDef.@base.bodyIdB = bodyId;
                revoluteDef.@base.localFrameA.p = bodyDef.position;
                revoluteDef.@base.localFrameB.p = b2Vec2_zero;
                revoluteDef.maxMotorTorque = 200.0f;
                revoluteDef.motorSpeed = 2.0f * sign;
                revoluteDef.enableMotor = true;

                b2CreateRevoluteJoint(m_worldId, ref revoluteDef);

                y -= 14.0f;
                sign = -sign;
            }

            {
                B2Polygon box = b2MakeOffsetBox(4.0f, 1.0f, new B2Vec2(0.0f, -30.5f), b2Rot_identity);
                B2ShapeDef shapeDef = b2DefaultShapeDef();
                shapeDef.isSensor = true;
                shapeDef.enableSensorEvents = true;

                b2CreatePolygonShape(groundId, ref shapeDef, ref box);
            }
        }

        m_wait = 0.5f;
        m_side = -15.0f;
        m_type = (int)ea.e_human;

        for (int i = 0; i < (int)ea.e_count; ++i)
        {
            m_isSpawned[i] = false;
        }

        //memset( m_humans, 0, sizeof( m_humans ) );
        for (int i = 0; i < m_humans.Length; ++i)
        {
            m_humans[i].Clear();
        }

        CreateElement();
    }

    void CreateElement()
    {
        int index = -1;
        for (int i = 0; i < (int)ea.e_count; ++i)
        {
            if (m_isSpawned[i] == false)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            return;
        }

        B2Vec2 center = new B2Vec2(m_side, 29.5f);

        if (m_type == (int)ea.e_donut)
        {
            ref Donut donut = ref m_donuts[index];
            // donut->Spawn(m_worldId, center, index + 1, donut);
            donut.Create(m_worldId, center, 1.0f, 0, true, CustomUserData.Create(index));
        }
        else
        {
            ref Human human = ref m_humans[index];
            float scale = 2.0f;
            float jointFriction = 0.05f;
            float jointHertz = 6.0f;
            float jointDamping = 0.5f;
            bool colorize = true;
            CreateHuman(ref human, m_worldId, center, scale, jointFriction, jointHertz, jointDamping, index + 1, CustomUserData.Create(index), colorize);
            Human_EnableSensorEvents(ref human, true);
        }

        m_isSpawned[index] = true;
        m_side = -m_side;
    }

    void DestroyElement(int index)
    {
        if (m_type == (int)ea.e_donut)
        {
            ref Donut donut = ref m_donuts[index];
            donut.Destroy();
        }
        else
        {
            ref Human human = ref m_humans[index];
            DestroyHuman(ref human);
        }

        m_isSpawned[index] = false;
    }

    void Clear()
    {
        for (int i = 0; i < (int)ea.e_count; ++i)
        {
            if (m_isSpawned[i] == true)
            {
                if (m_type == (int)ea.e_donut)
                {
                    m_donuts[i].Destroy();
                }
                else
                {
                    DestroyHuman(ref m_humans[i]);
                }

                m_isSpawned[i] = false;
            }
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float height = 90.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(140.0f, height));

        ImGui.Begin("Sensor Event", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (ImGui.RadioButton("donut", m_type == (int)ea.e_donut))
        {
            Clear();
            m_type = (int)ea.e_donut;
        }

        if (ImGui.RadioButton("human", m_type == (int)ea.e_human))
        {
            Clear();
            m_type = (int)ea.e_human;
        }

        ImGui.End();
    }

    public override void Step()
    {
        if (m_stepCount == 832)
        {
            m_stepCount += 0;
        }

        base.Step();

        // Discover rings that touch the bottom sensor
        bool[] deferredDestruction = new bool[(int)ea.e_count];
        B2SensorEvents sensorEvents = b2World_GetSensorEvents(m_worldId);
        for (int i = 0; i < sensorEvents.beginCount; ++i)
        {
            ref B2SensorBeginTouchEvent @event = ref sensorEvents.beginEvents[i];
            B2ShapeId visitorId = @event.visitorShapeId;
            B2BodyId bodyId = b2Shape_GetBody(visitorId);

            if (m_type == (int)ea.e_donut)
            {
                CustomUserData<int> donut = b2Body_GetUserData(bodyId) as CustomUserData<int>;
                if (donut != null)
                {
                    int index = donut.Value;
                    B2_ASSERT(0 <= index && index < (int)ea.e_count);

                    // Defer destruction to avoid double destruction and event invalidation (orphaned shape ids)
                    deferredDestruction[index] = true;
                }
            }
            else
            {
                CustomUserData<int> human = b2Body_GetUserData(bodyId) as CustomUserData<int>;
                if (human != null)
                {
                    int index = human.Value;
                    B2_ASSERT(0 <= index && index < (int)ea.e_count);

                    // Defer destruction to avoid double destruction and event invalidation (orphaned shape ids)
                    deferredDestruction[index] = true;
                }
            }
        }

        // todo destroy mouse joint if necessary

        // Safely destroy rings that hit the bottom sensor
        for (int i = 0; i < (int)ea.e_count; ++i)
        {
            if (deferredDestruction[i])
            {
                DestroyElement(i);
            }
        }

        if (m_context.settings.hertz > 0.0f && m_context.settings.pause == false)
        {
            m_wait -= 1.0f / m_context.settings.hertz;
            if (m_wait < 0.0f)
            {
                CreateElement();
                m_wait += 0.5f;
            }
        }
    }
}