// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.Shared.RandomSupports;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples.Stackings;

// This sample shows some aspects of Box2D continuous collision:
// - bullet dynamic bodies which support continuous collision with non-bullet dynamic bodies
// - prevention of chain reaction tunneling
// Try disabling continuous collision and firing a bullet. You might see a bullet push a
// a through the static wall.
public class VerticalStack : Sample
{
    private static readonly int SampleVerticalStack = SampleFactory.Shared.RegisterSample("Stacking", "Vertical Stack", Create);

    public const int e_maxColumns = 10;
    public const int e_maxRows = 15;
    public const int e_maxBullets = 8;

    enum ShapeType
    {
        e_circleShape = 0,
        e_boxShape
    };

    private B2BodyId[] m_bullets = new B2BodyId[e_maxBullets];
    private B2BodyId[] m_bodies = new B2BodyId[e_maxRows * e_maxColumns];
    private int m_columnCount;
    private int m_rowCount;
    private int m_bulletCount;
    private ShapeType m_shapeType;
    private ShapeType m_bulletType;

    private static Sample Create(SampleContext context)
    {
        return new VerticalStack(context);
    }


    public VerticalStack(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(-7.0f, 9.0f);
            m_camera.zoom = 14.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, 0.0f);
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            //B2Polygon box = b2MakeBox(100.0f, 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            //b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            B2Segment segment = new B2Segment(new B2Vec2(10.0f, 0.0f), new B2Vec2(10.0f, 20.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);

            segment = new B2Segment(new B2Vec2(-30.0f, 0.0f), new B2Vec2(30.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        for (int i = 0; i < e_maxRows * e_maxColumns; ++i)
        {
            m_bodies[i] = b2_nullBodyId;
        }

        for (int i = 0; i < e_maxBullets; ++i)
        {
            m_bullets[i] = b2_nullBodyId;
        }

        m_shapeType = ShapeType.e_boxShape;
        m_rowCount = 12;
        m_columnCount = 1;
        m_bulletCount = 1;
        m_bulletType = ShapeType.e_circleShape;

        CreateStacks();
    }

    void CreateStacks()
    {
        for (int i = 0; i < e_maxRows * e_maxColumns; ++i)
        {
            if (B2_IS_NON_NULL(m_bodies[i]))
            {
                b2DestroyBody(m_bodies[i]);
                m_bodies[i] = b2_nullBodyId;
            }
        }

        B2Circle circle = new B2Circle(new B2Vec2(), 0.0f);
        circle.radius = 0.5f;

        B2Polygon box = b2MakeRoundedBox(0.45f, 0.45f, 0.05f);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = 0.3f;

        float offset;

        if (m_shapeType == ShapeType.e_circleShape)
        {
            offset = 0.0f;
        }
        else
        {
            offset = 0.01f;
        }

        float dx = -3.0f;
        float xroot = 8.0f;

        for (int j = 0; j < m_columnCount; ++j)
        {
            float x = xroot + j * dx;

            for (int i = 0; i < m_rowCount; ++i)
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;

                int n = j * m_rowCount + i;

                float shift = (i % 2 == 0 ? -offset : offset);
                bodyDef.position = new B2Vec2(x + shift, 0.5f + 1.0f * i);
                // bodyDef.position = {x + shift, 1.0f + 1.51f * i};
                B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

                m_bodies[n] = bodyId;

                if (m_shapeType == ShapeType.e_circleShape)
                {
                    b2CreateCircleShape(bodyId, shapeDef, circle);
                }
                else
                {
                    b2CreatePolygonShape(bodyId, shapeDef, box);
                }
            }
        }
    }

    void DestroyBody()
    {
        for (int j = 0; j < m_columnCount; ++j)
        {
            for (int i = 0; i < m_rowCount; ++i)
            {
                int n = j * m_rowCount + i;

                if (B2_IS_NON_NULL(m_bodies[n]))
                {
                    b2DestroyBody(m_bodies[n]);
                    m_bodies[n] = b2_nullBodyId;
                    break;
                }
            }
        }
    }

    void DestroyBullets()
    {
        for (int i = 0; i < e_maxBullets; ++i)
        {
            B2BodyId bullet = m_bullets[i];

            if (B2_IS_NON_NULL(bullet))
            {
                b2DestroyBody(bullet);
                m_bullets[i] = b2_nullBodyId;
            }
        }
    }

    void FireBullets()
    {
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.25f);
        B2Polygon box = b2MakeBox(0.25f, 0.25f);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 4.0f;

        for (int i = 0; i < m_bulletCount; ++i)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-26.7f - i, 6.0f);
            float speed = RandomFloatRange(200.0f, 300.0f);
            bodyDef.linearVelocity = new B2Vec2(speed, 0.0f);
            bodyDef.isBullet = true;

            B2BodyId bullet = b2CreateBody(m_worldId, bodyDef);

            if (m_bulletType == ShapeType.e_boxShape)
            {
                b2CreatePolygonShape(bullet, shapeDef, box);
            }
            else
            {
                b2CreateCircleShape(bullet, shapeDef, circle);
            }

            B2_ASSERT(B2_IS_NULL(m_bullets[i]));
            m_bullets[i] = bullet;
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 230.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Vertical Stack", ImGuiWindowFlags.NoResize);

        ImGui.PushItemWidth(120.0f);

        bool changed = false;
        string[] shapeTypes = ["Circle", "Box"];

        int shapeType = (int)m_shapeType;
        changed = changed || ImGui.Combo("Shape", ref shapeType, shapeTypes, shapeTypes.Length);
        m_shapeType = (ShapeType)shapeType;

        changed = changed || ImGui.SliderInt("Rows", ref m_rowCount, 1, e_maxRows);
        changed = changed || ImGui.SliderInt("Columns", ref m_columnCount, 1, e_maxColumns);

        ImGui.SliderInt("Bullets", ref m_bulletCount, 1, e_maxBullets);

        int bulletType = (int)m_bulletType;
        ImGui.Combo("Bullet Shape", ref bulletType, shapeTypes, shapeTypes.Length);
        m_bulletType = (ShapeType)bulletType;

        ImGui.PopItemWidth();

        if (ImGui.Button("Fire Bullets") || GetKey(Keys.B) == InputAction.Press)
        {
            DestroyBullets();
            FireBullets();
        }

        if (ImGui.Button("Destroy Body"))
        {
            DestroyBody();
        }

        changed = changed || ImGui.Button("Reset Stack");

        if (changed)
        {
            DestroyBullets();
            CreateStacks();
        }

        ImGui.End();
    }
}