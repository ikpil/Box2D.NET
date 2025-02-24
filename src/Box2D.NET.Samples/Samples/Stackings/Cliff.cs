using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Stackings;

public class Cliff : Sample
{
    b2BodyId[] m_bodyIds = new b2BodyId[9];
    bool m_flip;

    static int sampleCliff = RegisterSample("Stacking", "Cliff", Create);

    static Sample Create(Settings settings)
    {
        return new Cliff(settings);
    }


    public Cliff(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_zoom = 25.0f * 0.5f;
            Draw.g_camera.m_center = new b2Vec2(0.0f, 5.0f);
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new b2Vec2(0.0f, 0.0f);
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Polygon box = b2MakeOffsetBox(100.0f, 1.0f, new b2Vec2(0.0f, -1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            b2Segment segment = new b2Segment(new b2Vec2(-14.0f, 4.0f), new b2Vec2(-8.0f, 4.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);

            box = b2MakeOffsetBox(3.0f, 0.5f, new b2Vec2(0.0f, 4.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            b2Capsule capsule = new b2Capsule(new b2Vec2(8.5f, 4.0f), new b2Vec2(13.5f, 4.0f), 0.5f);
            b2CreateCapsuleShape(groundId, shapeDef, capsule);
        }

        m_flip = false;

        for (int i = 0; i < 9; ++i)
        {
            m_bodyIds[i] = b2_nullBodyId;
        }

        CreateBodies();
    }

    void CreateBodies()
    {
        for (int i = 0; i < 9; ++i)
        {
            if (B2_IS_NON_NULL(m_bodyIds[i]))
            {
                b2DestroyBody(m_bodyIds[i]);
                m_bodyIds[i] = b2_nullBodyId;
            }
        }

        float sign = m_flip ? -1.0f : 1.0f;

        b2Capsule capsule = new b2Capsule(new b2Vec2(-0.25f, 0.0f), new b2Vec2(0.25f, 0.0f), 0.25f);
        b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.5f);
        b2Polygon square = b2MakeSquare(0.5f);

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;

        {
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.01f;
            bodyDef.linearVelocity = new b2Vec2(2.0f * sign, 0.0f);

            float offset = m_flip ? -4.0f : 0.0f;

            bodyDef.position = new b2Vec2(-9.0f + offset, 4.25f);
            m_bodyIds[0] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCapsuleShape(m_bodyIds[0], shapeDef, capsule);

            bodyDef.position = new b2Vec2(2.0f + offset, 4.75f);
            m_bodyIds[1] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCapsuleShape(m_bodyIds[1], shapeDef, capsule);

            bodyDef.position = new b2Vec2(13.0f + offset, 4.75f);
            m_bodyIds[2] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCapsuleShape(m_bodyIds[2], shapeDef, capsule);
        }

        {
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.01f;
            bodyDef.linearVelocity = new b2Vec2(2.5f * sign, 0.0f);

            bodyDef.position = new b2Vec2(-11.0f, 4.5f);
            m_bodyIds[3] = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(m_bodyIds[3], shapeDef, square);

            bodyDef.position = new b2Vec2(0.0f, 5.0f);
            m_bodyIds[4] = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(m_bodyIds[4], shapeDef, square);

            bodyDef.position = new b2Vec2(11.0f, 5.0f);
            m_bodyIds[5] = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(m_bodyIds[5], shapeDef, square);
        }

        {
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.2f;
            bodyDef.linearVelocity = new b2Vec2(1.5f * sign, 0.0f);

            float offset = m_flip ? 4.0f : 0.0f;

            bodyDef.position = new b2Vec2(-13.0f + offset, 4.5f);
            m_bodyIds[6] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCircleShape(m_bodyIds[6], shapeDef, circle);

            bodyDef.position = new b2Vec2(-2.0f + offset, 5.0f);
            m_bodyIds[7] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCircleShape(m_bodyIds[7], shapeDef, circle);

            bodyDef.position = new b2Vec2(9.0f + offset, 5.0f);
            m_bodyIds[8] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCircleShape(m_bodyIds[8], shapeDef, circle);
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 60.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(160.0f, height));

        ImGui.Begin("Cliff", ref open, ImGuiWindowFlags.NoResize);

        if (ImGui.Button("Flip"))
        {
            m_flip = !m_flip;
            CreateBodies();
        }

        ImGui.End();
    }
}