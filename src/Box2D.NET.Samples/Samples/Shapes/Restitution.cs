using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;

// Restitution is approximate since Box2D uses speculative collision
public class Restitution : Sample
{
    enum ShapeType
    {
        e_circleShape = 0,
        e_boxShape
    };

    public const int e_count = 40;


    b2BodyId[] m_bodyIds = new b2BodyId[e_count];
    ShapeType m_shapeType;

    static int sampleIndex = RegisterSample("Shapes", "Restitution", Create);

    static Sample Create(Settings settings)
    {
        return new Restitution(settings);
    }


    public Restitution(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(4.0f, 17.0f);
            Draw.g_camera.m_zoom = 27.5f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            float h = 1.0f * e_count;
            b2Segment segment = new b2Segment(new b2Vec2(-h, 0.0f), new b2Vec2(h, 0.0f));
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        for (int i = 0; i < e_count; ++i)
        {
            m_bodyIds[i] = b2_nullBodyId;
        }

        m_shapeType = ShapeType.e_circleShape;

        CreateBodies();
    }

    void CreateBodies()
    {
        for (int i = 0; i < e_count; ++i)
        {
            if (B2_IS_NON_NULL(m_bodyIds[i]))
            {
                b2DestroyBody(m_bodyIds[i]);
                m_bodyIds[i] = b2_nullBodyId;
            }
        }

        b2Circle circle = new b2Circle(new b2Vec2(), 0.0f);
        circle.radius = 0.5f;

        b2Polygon box = b2MakeBox(0.5f, 0.5f);

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.restitution = 0.0f;

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;

        float dr = 1.0f / (e_count > 1 ? e_count - 1 : 1);
        float x = -1.0f * (e_count - 1);
        float dx = 2.0f;

        for (int i = 0; i < e_count; ++i)
        {
            bodyDef.position = new b2Vec2(x, 40.0f);
            b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            m_bodyIds[i] = bodyId;

            if (m_shapeType == ShapeType.e_circleShape)
            {
                b2CreateCircleShape(bodyId, shapeDef, circle);
            }
            else
            {
                b2CreatePolygonShape(bodyId, shapeDef, box);
            }

            shapeDef.restitution += dr;
            x += dx;
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 100.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Restitution", ref open, ImGuiWindowFlags.NoResize);

        bool changed = false;
        string[] shapeTypes = ["Circle", "Box"];

        int shapeType = (int)m_shapeType;
        changed = changed || ImGui.Combo("Shape", ref shapeType, shapeTypes, shapeTypes.Length);
        m_shapeType = (ShapeType)shapeType;

        changed = changed || ImGui.Button("Reset");

        if (changed)
        {
            CreateBodies();
        }

        ImGui.End();
    }
}