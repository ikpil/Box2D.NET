// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Shapes;

public class ShapeFilter : Sample
{
    private static readonly int SampleShapeFilter = SampleFactory.Shared.RegisterSample("Shapes", "Filter", Create);
    
    public const ulong GROUND = 0x00000001;
    public const ulong TEAM1 = 0x00000002;
    public const ulong TEAM2 = 0x00000004;
    public const ulong TEAM3 = 0x00000008;
    public const ulong ALL_BITS = 0xFFFFFFFF;


    B2BodyId m_player1Id;
    B2BodyId m_player2Id;
    B2BodyId m_player3Id;

    B2ShapeId m_shape1Id;
    B2ShapeId m_shape2Id;
    B2ShapeId m_shape3Id;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new ShapeFilter(ctx, settings);
    }


    public ShapeFilter(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_zoom = 25.0f * 0.5f;
            m_context.camera.m_center = new B2Vec2(0.0f, 5.0f);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter.categoryBits = GROUND;
            shapeDef.filter.maskBits = ALL_BITS;

            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            bodyDef.position = new B2Vec2(0.0f, 2.0f);
            m_player1Id = b2CreateBody(m_worldId, ref bodyDef);

            bodyDef.position = new B2Vec2(0.0f, 5.0f);
            m_player2Id = b2CreateBody(m_worldId, ref bodyDef);

            bodyDef.position = new B2Vec2(0.0f, 8.0f);
            m_player3Id = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(2.0f, 1.0f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();

            shapeDef.filter.categoryBits = TEAM1;
            shapeDef.filter.maskBits = GROUND | TEAM2 | TEAM3;
            m_shape1Id = b2CreatePolygonShape(m_player1Id, ref shapeDef, ref box);

            shapeDef.filter.categoryBits = TEAM2;
            shapeDef.filter.maskBits = GROUND | TEAM1 | TEAM3;
            m_shape2Id = b2CreatePolygonShape(m_player2Id, ref shapeDef, ref box);

            shapeDef.filter.categoryBits = TEAM3;
            shapeDef.filter.maskBits = GROUND | TEAM1 | TEAM2;
            m_shape3Id = b2CreatePolygonShape(m_player3Id, ref shapeDef, ref box);
        }
    }

    public override void UpdateUI()
    {
        base.UpdateUI();
        
        float height = 240.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Shape Filter", ImGuiWindowFlags.NoResize);

        ImGui.Text("Player 1 Collides With");
        {
            B2Filter filter1 = b2Shape_GetFilter(m_shape1Id);
            bool team2 = (filter1.maskBits & TEAM2) == TEAM2;
            if (ImGui.Checkbox("Team 2##1", ref team2))
            {
                if (team2)
                {
                    filter1.maskBits |= TEAM2;
                }
                else
                {
                    filter1.maskBits &= ~TEAM2;
                }

                b2Shape_SetFilter(m_shape1Id, filter1);
            }

            bool team3 = (filter1.maskBits & TEAM3) == TEAM3;
            if (ImGui.Checkbox("Team 3##1", ref team3))
            {
                if (team3)
                {
                    filter1.maskBits |= TEAM3;
                }
                else
                {
                    filter1.maskBits &= ~TEAM3;
                }

                b2Shape_SetFilter(m_shape1Id, filter1);
            }
        }

        ImGui.Separator();

        ImGui.Text("Player 2 Collides With");
        {
            B2Filter filter2 = b2Shape_GetFilter(m_shape2Id);
            bool team1 = (filter2.maskBits & TEAM1) == TEAM1;
            if (ImGui.Checkbox("Team 1##2", ref team1))
            {
                if (team1)
                {
                    filter2.maskBits |= TEAM1;
                }
                else
                {
                    filter2.maskBits &= ~TEAM1;
                }

                b2Shape_SetFilter(m_shape2Id, filter2);
            }

            bool team3 = (filter2.maskBits & TEAM3) == TEAM3;
            if (ImGui.Checkbox("Team 3##2", ref team3))
            {
                if (team3)
                {
                    filter2.maskBits |= TEAM3;
                }
                else
                {
                    filter2.maskBits &= ~TEAM3;
                }

                b2Shape_SetFilter(m_shape2Id, filter2);
            }
        }

        ImGui.Separator();

        ImGui.Text("Player 3 Collides With");
        {
            B2Filter filter3 = b2Shape_GetFilter(m_shape3Id);
            bool team1 = (filter3.maskBits & TEAM1) == TEAM1;
            if (ImGui.Checkbox("Team 1##3", ref team1))
            {
                if (team1)
                {
                    filter3.maskBits |= TEAM1;
                }
                else
                {
                    filter3.maskBits &= ~TEAM1;
                }

                b2Shape_SetFilter(m_shape3Id, filter3);
            }

            bool team2 = (filter3.maskBits & TEAM2) == TEAM2;
            if (ImGui.Checkbox("Team 2##3", ref team2))
            {
                if (team2)
                {
                    filter3.maskBits |= TEAM2;
                }
                else
                {
                    filter3.maskBits &= ~TEAM2;
                }

                b2Shape_SetFilter(m_shape3Id, filter3);
            }
        }

        ImGui.End();
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);
        
        B2Vec2 p1 = b2Body_GetPosition(m_player1Id);
        m_context.draw.DrawString(new B2Vec2(p1.x - 0.5f, p1.y), "player 1");

        B2Vec2 p2 = b2Body_GetPosition(m_player2Id);
        m_context.draw.DrawString(new B2Vec2(p2.x - 0.5f, p2.y), "player 2");

        B2Vec2 p3 = b2Body_GetPosition(m_player3Id);
        m_context.draw.DrawString(new B2Vec2(p3.x - 0.5f, p3.y), "player 3");

    }
}
