// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Cores;

namespace Box2D.NET.Samples.Samples.Benchmarks;

// todo try removing kinematics from graph coloring
public class BenchmarkManyTumblers : Sample
{
    private static readonly int SampleBenchmarkManyTumblers = SampleFactory.Shared.RegisterSample("Benchmark", "Many Tumblers", Create);
    
    private B2BodyId m_groundId;

    private int m_rowCount;
    private int m_columnCount;

    private B2BodyId[] m_tumblerIds;
    private B2Vec2[] m_positions;
    private int m_tumblerCount;

    private B2BodyId[] m_bodyIds;
    private int m_bodyCount;
    private int m_bodyIndex;

    private float m_angularSpeed;

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkManyTumblers(context);
    }

    public BenchmarkManyTumblers(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(1.0f, -5.5f);
            m_context.camera.m_zoom = 25.0f * 3.4f;
            m_context.settings.drawJoints = false;
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        m_groundId = b2CreateBody(m_worldId, ref bodyDef);

        m_rowCount = m_isDebug ? 2 : 19;
        m_columnCount = m_isDebug ? 2 : 19;

        m_tumblerIds = null;
        m_positions = null;
        m_tumblerCount = 0;

        m_bodyIds = null;
        m_bodyCount = 0;
        m_bodyIndex = 0;

        m_angularSpeed = 25.0f;

        CreateScene();
    }

    void CreateTumbler(B2Vec2 position, int index)
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_kinematicBody;
        bodyDef.position = new B2Vec2(position.X, position.Y);
        bodyDef.angularVelocity = (B2_PI / 180.0f) * m_angularSpeed;
        B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
        m_tumblerIds[index] = bodyId;

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 50.0f;

        B2Polygon polygon;
        polygon = b2MakeOffsetBox(0.25f, 2.0f, new B2Vec2(2.0f, 0.0f), b2Rot_identity);
        b2CreatePolygonShape(bodyId, ref shapeDef, ref polygon);
        polygon = b2MakeOffsetBox(0.25f, 2.0f, new B2Vec2(-2.0f, 0.0f), b2Rot_identity);
        b2CreatePolygonShape(bodyId, ref shapeDef, ref polygon);
        polygon = b2MakeOffsetBox(2.0f, 0.25f, new B2Vec2(0.0f, 2.0f), b2Rot_identity);
        b2CreatePolygonShape(bodyId, ref shapeDef, ref polygon);
        polygon = b2MakeOffsetBox(2.0f, 0.25f, new B2Vec2(0.0f, -2.0f), b2Rot_identity);
        b2CreatePolygonShape(bodyId, ref shapeDef, ref polygon);
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
        m_tumblerIds = new B2BodyId[m_tumblerCount];
        m_positions = new B2Vec2[m_tumblerCount];

        int index = 0;
        float x = -4.0f * m_rowCount;
        for (int i = 0; i < m_rowCount; ++i)
        {
            float y = -4.0f * m_columnCount;
            for (int j = 0; j < m_columnCount; ++j)
            {
                m_positions[index] = new B2Vec2(x, y);
                CreateTumbler(m_positions[index], index);
                ++index;
                y += 8.0f;
            }

            x += 8.0f;
        }

        m_bodyIds = null;

        int bodiesPerTumbler = m_isDebug ? 8 : 50;
        m_bodyCount = bodiesPerTumbler * m_tumblerCount;

        m_bodyIds = new B2BodyId[m_bodyCount];
        //memset( m_bodyIds, 0, m_bodyCount * sizeof( b2BodyId ) );
        m_bodyIndex = 0;
    }

    public override void UpdateGui()
    {
        base.UpdateGui();
        
        float height = 110.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));
        ImGui.Begin("Benchmark: Many Tumblers", ImGuiWindowFlags.NoResize);
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

    public override void Step()
    {
        base.Step();

        if (m_bodyIndex < m_bodyCount && (m_stepCount & 0x7) == 0)
        {
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            B2Capsule capsule = new B2Capsule(new B2Vec2(-0.1f, 0.0f), new B2Vec2(0.1f, 0.0f), 0.075f);

            for (int i = 0; i < m_tumblerCount; ++i)
            {
                B2_ASSERT(m_bodyIndex < m_bodyCount);

                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = m_positions[i];
                m_bodyIds[m_bodyIndex] = b2CreateBody(m_worldId, ref bodyDef);
                b2CreateCapsuleShape(m_bodyIds[m_bodyIndex], ref shapeDef, ref capsule);

                m_bodyIndex += 1;
            }
        }
    }
}
