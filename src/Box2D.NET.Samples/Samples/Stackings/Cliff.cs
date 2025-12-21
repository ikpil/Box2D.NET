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

namespace Box2D.NET.Samples.Samples.Stackings;

public class Cliff : Sample
{
    private static readonly int SampleCliff = SampleFactory.Shared.RegisterSample("Stacking", "Cliff", Create);

    private B2BodyId[] m_bodyIds = new B2BodyId[9];
    private bool m_flip;

    private static Sample Create(SampleContext context)
    {
        return new Cliff(context);
    }


    public Cliff(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.zoom = 25.0f * 0.5f;
            m_camera.center = new B2Vec2(0.0f, 5.0f);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, 0.0f);
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(100.0f, 1.0f, new B2Vec2(0.0f, -1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            B2Segment segment = new B2Segment(new B2Vec2(-14.0f, 4.0f), new B2Vec2(-8.0f, 4.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);

            box = b2MakeOffsetBox(3.0f, 0.5f, new B2Vec2(0.0f, 4.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            B2Capsule capsule = new B2Capsule(new B2Vec2(8.5f, 4.0f), new B2Vec2(13.5f, 4.0f), 0.5f);
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

        B2Capsule capsule = new B2Capsule(new B2Vec2(-0.25f, 0.0f), new B2Vec2(0.25f, 0.0f), 0.25f);
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        B2Polygon square = b2MakeSquare(0.5f);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;

        {
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.01f;
            bodyDef.linearVelocity = new B2Vec2(2.0f * sign, 0.0f);

            float offset = m_flip ? -4.0f : 0.0f;

            bodyDef.position = new B2Vec2(-9.0f + offset, 4.25f);
            m_bodyIds[0] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCapsuleShape(m_bodyIds[0], shapeDef, capsule);

            bodyDef.position = new B2Vec2(2.0f + offset, 4.75f);
            m_bodyIds[1] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCapsuleShape(m_bodyIds[1], shapeDef, capsule);

            bodyDef.position = new B2Vec2(13.0f + offset, 4.75f);
            m_bodyIds[2] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCapsuleShape(m_bodyIds[2], shapeDef, capsule);
        }

        {
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.01f;
            bodyDef.linearVelocity = new B2Vec2(2.5f * sign, 0.0f);

            bodyDef.position = new B2Vec2(-11.0f, 4.5f);
            m_bodyIds[3] = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(m_bodyIds[3], shapeDef, square);

            bodyDef.position = new B2Vec2(0.0f, 5.0f);
            m_bodyIds[4] = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(m_bodyIds[4], shapeDef, square);

            bodyDef.position = new B2Vec2(11.0f, 5.0f);
            m_bodyIds[5] = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(m_bodyIds[5], shapeDef, square);
        }

        {
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.2f;
            bodyDef.linearVelocity = new B2Vec2(1.5f * sign, 0.0f);

            float offset = m_flip ? 4.0f : 0.0f;

            bodyDef.position = new B2Vec2(-13.0f + offset, 4.5f);
            m_bodyIds[6] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCircleShape(m_bodyIds[6], shapeDef, circle);

            bodyDef.position = new B2Vec2(-2.0f + offset, 5.0f);
            m_bodyIds[7] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCircleShape(m_bodyIds[7], shapeDef, circle);

            bodyDef.position = new B2Vec2(9.0f + offset, 5.0f);
            m_bodyIds[8] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCircleShape(m_bodyIds[8], shapeDef, circle);
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 60.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(160.0f, height));

        ImGui.Begin("Cliff", ImGuiWindowFlags.NoResize);

        if (ImGui.Button("Flip"))
        {
            m_flip = !m_flip;
            CreateBodies();
        }

        ImGui.End();
    }
}