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
    private static readonly int SampleIndex = SampleFactory.Shared.RegisterSample("Shapes", "Restitution", Create);

    enum ShapeType
    {
        e_circleShape = 0,
        e_boxShape
    };

    public const int m_count = 40;

    private B2BodyId[] m_bodyIds = new B2BodyId[m_count];
    private ShapeType m_shapeType;

    private static Sample Create(SampleContext context)
    {
        return new Restitution(context);
    }

    public Restitution(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(4.0f, 17.0f);
            m_context.camera.m_zoom = 27.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            float h = 1.0f * m_count;
            B2Segment segment = new B2Segment(new B2Vec2(-h, 0.0f), new B2Vec2(h, 0.0f));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        m_shapeType = ShapeType.e_circleShape;

        CreateBodies();
    }

    void CreateBodies()
    {
        for (int i = 0; i < m_count; ++i)
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
        shapeDef.material.restitution = 0.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;

        float dr = 1.0f / (m_count > 1 ? m_count - 1 : 1);
        float x = -1.0f * (m_count - 1);
        float dx = 2.0f;

        for (int i = 0; i < m_count; ++i)
        {
            bodyDef.position = new B2Vec2(x, 40.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            m_bodyIds[i] = bodyId;

            if (m_shapeType == ShapeType.e_circleShape)
            {
                b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
            }
            else
            {
                b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
            }

            shapeDef.material.restitution += dr;
            x += dx;
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float height = 100.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Restitution", ImGuiWindowFlags.NoResize);

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