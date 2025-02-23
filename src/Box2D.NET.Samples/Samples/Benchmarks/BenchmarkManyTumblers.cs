using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Benchmarks;

// todo try removing kinematics from graph coloring
public class BenchmarkManyTumblers : Sample
{
    b2BodyId m_groundId;

    int m_rowCount;
    int m_columnCount;

    b2BodyId[] m_tumblerIds;
    b2Vec2[] m_positions;
    int m_tumblerCount;

    b2BodyId[] m_bodyIds;
    int m_bodyCount;
    int m_bodyIndex;

    float m_angularSpeed;
    static int benchmarkManyTumblers = RegisterSample("Benchmark", "Many Tumblers", Create);

    static Sample Create(Settings settings)
    {
        return new BenchmarkManyTumblers(settings);
    }

    public BenchmarkManyTumblers(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(1.0f, -5.5f);
            Draw.g_camera.m_zoom = 25.0f * 3.4f;
            settings.drawJoints = false;
        }

        b2BodyDef bodyDef = b2DefaultBodyDef();
        m_groundId = b2CreateBody(m_worldId, bodyDef);

        m_rowCount = g_sampleDebug ? 2 : 19;
        m_columnCount = g_sampleDebug ? 2 : 19;

        m_tumblerIds = null;
        m_positions = null;
        m_tumblerCount = 0;

        m_bodyIds = null;
        m_bodyCount = 0;
        m_bodyIndex = 0;

        m_angularSpeed = 25.0f;

        CreateScene();
    }

    void CreateTumbler(b2Vec2 position, int index)
    {
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_kinematicBody;
        bodyDef.position = new b2Vec2(position.x, position.y);
        bodyDef.angularVelocity = (B2_PI / 180.0f) * m_angularSpeed;
        b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
        m_tumblerIds[index] = bodyId;

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 50.0f;

        b2Polygon polygon;
        polygon = b2MakeOffsetBox(0.25f, 2.0f, new b2Vec2(2.0f, 0.0f), b2Rot_identity);
        b2CreatePolygonShape(bodyId, shapeDef, polygon);
        polygon = b2MakeOffsetBox(0.25f, 2.0f, new b2Vec2(-2.0f, 0.0f), b2Rot_identity);
        b2CreatePolygonShape(bodyId, shapeDef, polygon);
        polygon = b2MakeOffsetBox(2.0f, 0.25f, new b2Vec2(0.0f, 2.0f), b2Rot_identity);
        b2CreatePolygonShape(bodyId, shapeDef, polygon);
        polygon = b2MakeOffsetBox(2.0f, 0.25f, new b2Vec2(0.0f, -2.0f), b2Rot_identity);
        b2CreatePolygonShape(bodyId, shapeDef, polygon);
    }

    void CreateScene()
    {
        for (int i = 0; i < m_bodyCount; ++i)
        {
            if (B2_IS_NON_NULL(m_bodyIds[i]))
            {
                b2DestroyBody(m_bodyIds[i]);
            }
        }

        for (int i = 0; i < m_tumblerCount; ++i)
        {
            b2DestroyBody(m_tumblerIds[i]);
        }

        m_tumblerIds = null;
        m_positions = null;

        m_tumblerCount = m_rowCount * m_columnCount;
        m_tumblerIds = new b2BodyId[m_tumblerCount];
        m_positions = new b2Vec2[m_tumblerCount];

        int index = 0;
        float x = -4.0f * m_rowCount;
        for (int i = 0; i < m_rowCount; ++i)
        {
            float y = -4.0f * m_columnCount;
            for (int j = 0; j < m_columnCount; ++j)
            {
                m_positions[index] = new b2Vec2(x, y);
                CreateTumbler(m_positions[index], index);
                ++index;
                y += 8.0f;
            }

            x += 8.0f;
        }

        m_bodyIds = null;

        int bodiesPerTumbler = g_sampleDebug ? 8 : 50;
        m_bodyCount = bodiesPerTumbler * m_tumblerCount;

        m_bodyIds = new b2BodyId[m_bodyCount];
        //memset( m_bodyIds, 0, m_bodyCount * sizeof( b2BodyId ) );
        m_bodyIndex = 0;
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 110.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));
        ImGui.Begin("Benchmark: Many Tumblers", ref open, ImGuiWindowFlags.NoResize);
        ImGui.PushItemWidth(100.0f);

        bool changed = false;
        changed = changed || ImGui.SliderInt("Row Count", ref m_rowCount, 1, 32);
        changed = changed || ImGui.SliderInt("Column Count", ref m_columnCount, 1, 32);

        if (changed)
        {
            CreateScene();
        }

        if (ImGui.SliderFloat("Speed", ref m_angularSpeed, 0.0f, 100.0f, "%.f"))
        {
            for (int i = 0; i < m_tumblerCount; ++i)
            {
                b2Body_SetAngularVelocity(m_tumblerIds[i], (B2_PI / 180.0f) * m_angularSpeed);
                b2Body_SetAwake(m_tumblerIds[i], true);
            }
        }

        ImGui.PopItemWidth();
        ImGui.End();
    }

    public virtual void Step(Settings settings)
    {
        base.Step(settings);

        if (m_bodyIndex < m_bodyCount && (m_stepCount & 0x7) == 0)
        {
            b2ShapeDef shapeDef = b2DefaultShapeDef();

            b2Capsule capsule = new b2Capsule(new b2Vec2(-0.1f, 0.0f), new b2Vec2(0.1f, 0.0f), 0.075f);

            for (int i = 0; i < m_tumblerCount; ++i)
            {
                Debug.Assert(m_bodyIndex < m_bodyCount);

                b2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = b2BodyType.b2_dynamicBody;
                bodyDef.position = m_positions[i];
                m_bodyIds[m_bodyIndex] = b2CreateBody(m_worldId, bodyDef);
                b2CreateCapsuleShape(m_bodyIds[m_bodyIndex], shapeDef, capsule);

                m_bodyIndex += 1;
            }
        }
    }
}