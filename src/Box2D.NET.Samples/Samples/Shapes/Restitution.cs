// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

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


    B2BodyId[] m_bodyIds = new B2BodyId[e_count];
    ShapeType m_shapeType;

    private static readonly int SampleIndex = SampleFactory.Shared.RegisterSample("Shapes", "Restitution", Create);

    private static Sample Create(Settings settings)
    {
        return new Restitution(settings);
    }


    public Restitution(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(4.0f, 17.0f);
            B2.g_camera.m_zoom = 27.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            float h = 1.0f * e_count;
            B2Segment segment = new B2Segment(new B2Vec2(-h, 0.0f), new B2Vec2(h, 0.0f));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(groundId, ref shapeDef, segment);
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

        B2Circle circle = new B2Circle(new B2Vec2(), 0.0f);
        circle.radius = 0.5f;

        B2Polygon box = b2MakeBox(0.5f, 0.5f);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.restitution = 0.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;

        float dr = 1.0f / (e_count > 1 ? e_count - 1 : 1);
        float x = -1.0f * (e_count - 1);
        float dx = 2.0f;

        for (int i = 0; i < e_count; ++i)
        {
            bodyDef.position = new B2Vec2(x, 40.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            m_bodyIds[i] = bodyId;

            if (m_shapeType == ShapeType.e_circleShape)
            {
                b2CreateCircleShape(bodyId, ref shapeDef, circle);
            }
            else
            {
                b2CreatePolygonShape(bodyId, ref shapeDef, box);
            }

            shapeDef.restitution += dr;
            x += dx;
        }
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 100.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, B2.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
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
