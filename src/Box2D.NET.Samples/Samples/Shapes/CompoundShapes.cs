// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Shapes;

// This sample shows how careful creation of compound shapes leads to better simulation and avoids
// objects getting stuck.
// This also shows how to get the combined AABB for the body.
public class CompoundShapes : Sample
{
    B2BodyId m_table1Id;
    B2BodyId m_table2Id;
    B2BodyId m_ship1Id;
    B2BodyId m_ship2Id;
    bool m_drawBodyAABBs;

    private static readonly int SampleCompoundShape = SampleFactory.Shared.RegisterSample("Shapes", "Compound Shapes", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new CompoundShapes(ctx, settings);
    }

    public CompoundShapes(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 6.0f);
            m_context.camera.m_zoom = 25.0f * 0.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(50.0f, 0.0f), new B2Vec2(-50.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        // Table 1
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-15.0f, 1.0f);
            m_table1Id = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon top = b2MakeOffsetBox(3.0f, 0.5f, new B2Vec2(0.0f, 3.5f), b2Rot_identity);
            B2Polygon leftLeg = b2MakeOffsetBox(0.5f, 1.5f, new B2Vec2(-2.5f, 1.5f), b2Rot_identity);
            B2Polygon rightLeg = b2MakeOffsetBox(0.5f, 1.5f, new B2Vec2(2.5f, 1.5f), b2Rot_identity);

            b2CreatePolygonShape(m_table1Id, ref shapeDef, ref top);
            b2CreatePolygonShape(m_table1Id, ref shapeDef, ref leftLeg);
            b2CreatePolygonShape(m_table1Id, ref shapeDef, ref rightLeg);
        }

        // Table 2
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-5.0f, 1.0f);
            m_table2Id = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon top = b2MakeOffsetBox(3.0f, 0.5f, new B2Vec2(0.0f, 3.5f), b2Rot_identity);
            B2Polygon leftLeg = b2MakeOffsetBox(0.5f, 2.0f, new B2Vec2(-2.5f, 2.0f), b2Rot_identity);
            B2Polygon rightLeg = b2MakeOffsetBox(0.5f, 2.0f, new B2Vec2(2.5f, 2.0f), b2Rot_identity);

            b2CreatePolygonShape(m_table2Id, ref shapeDef, ref top);
            b2CreatePolygonShape(m_table2Id, ref shapeDef, ref leftLeg);
            b2CreatePolygonShape(m_table2Id, ref shapeDef, ref rightLeg);
        }

        // Spaceship 1
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(5.0f, 1.0f);
            m_ship1Id = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Vec2[] vertices = new B2Vec2[3];

            vertices[0] = new B2Vec2(-2.0f, 0.0f);
            vertices[1] = new B2Vec2(0.0f, 4.0f / 3.0f);
            vertices[2] = new B2Vec2(0.0f, 4.0f);
            B2Hull hull = b2ComputeHull(vertices, 3);
            B2Polygon left = b2MakePolygon(hull, 0.0f);

            vertices[0] = new B2Vec2(2.0f, 0.0f);
            vertices[1] = new B2Vec2(0.0f, 4.0f / 3.0f);
            vertices[2] = new B2Vec2(0.0f, 4.0f);
            hull = b2ComputeHull(vertices, 3);
            B2Polygon right = b2MakePolygon(hull, 0.0f);

            b2CreatePolygonShape(m_ship1Id, ref shapeDef, ref left);
            b2CreatePolygonShape(m_ship1Id, ref shapeDef, ref right);
        }

        // Spaceship 2
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(15.0f, 1.0f);
            m_ship2Id = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Vec2[] vertices = new B2Vec2[3];

            vertices[0] = new B2Vec2(-2.0f, 0.0f);
            vertices[1] = new B2Vec2(1.0f, 2.0f);
            vertices[2] = new B2Vec2(0.0f, 4.0f);
            B2Hull hull = b2ComputeHull(vertices, 3);
            B2Polygon left = b2MakePolygon(hull, 0.0f);

            vertices[0] = new B2Vec2(2.0f, 0.0f);
            vertices[1] = new B2Vec2(-1.0f, 2.0f);
            vertices[2] = new B2Vec2(0.0f, 4.0f);
            hull = b2ComputeHull(vertices, 3);
            B2Polygon right = b2MakePolygon(hull, 0.0f);

            b2CreatePolygonShape(m_ship2Id, ref shapeDef, ref left);
            b2CreatePolygonShape(m_ship2Id, ref shapeDef, ref right);
        }

        m_drawBodyAABBs = false;
    }

    void Spawn()
    {
        // Table 1 obstruction
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = b2Body_GetPosition(m_table1Id);
            bodyDef.rotation = b2Body_GetRotation(m_table1Id);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(4.0f, 0.1f, new B2Vec2(0.0f, 3.0f), b2Rot_identity);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        // Table 2 obstruction
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = b2Body_GetPosition(m_table2Id);
            bodyDef.rotation = b2Body_GetRotation(m_table2Id);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(4.0f, 0.1f, new B2Vec2(0.0f, 3.0f), b2Rot_identity);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        // Ship 1 obstruction
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = b2Body_GetPosition(m_ship1Id);
            bodyDef.rotation = b2Body_GetRotation(m_ship1Id);
            // bodyDef.gravityScale = 0.0f;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 2.0f), 0.5f);
            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }

        // Ship 2 obstruction
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = b2Body_GetPosition(m_ship2Id);
            bodyDef.rotation = b2Body_GetRotation(m_ship2Id);
            // bodyDef.gravityScale = 0.0f;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 2.0f), 0.5f);
            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 100.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(180.0f, height));

        ImGui.Begin("Compound Shapes", ref open, ImGuiWindowFlags.NoResize);

        if (ImGui.Button("Intrude"))
        {
            Spawn();
        }

        ImGui.Checkbox("Body AABBs", ref m_drawBodyAABBs);

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        if (m_drawBodyAABBs)
        {
            B2AABB aabb = b2Body_ComputeAABB(m_table1Id);
            m_context.g_draw.DrawAABB(aabb, B2HexColor.b2_colorYellow);

            aabb = b2Body_ComputeAABB(m_table2Id);
            m_context.g_draw.DrawAABB(aabb, B2HexColor.b2_colorYellow);

            aabb = b2Body_ComputeAABB(m_ship1Id);
            m_context.g_draw.DrawAABB(aabb, B2HexColor.b2_colorYellow);

            aabb = b2Body_ComputeAABB(m_ship2Id);
            m_context.g_draw.DrawAABB(aabb, B2HexColor.b2_colorYellow);
        }
    }
}
