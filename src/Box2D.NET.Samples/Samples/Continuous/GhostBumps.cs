// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Continuous;

// This sample shows ghost bumps
public class GhostBumps : Sample
{
    private enum ShapeType
    {
        e_circleShape = 0,
        e_capsuleShape,
        e_boxShape
    }

    B2BodyId m_groundId;
    B2BodyId m_bodyId;
    B2ShapeId m_shapeId;
    ShapeType m_shapeType;
    float m_round;
    float m_friction;
    float m_bevel;
    bool m_useChain;

    private static readonly int SampleGhostCollision = SampleFactory.Shared.RegisterSample("Continuous", "Ghost Bumps", Create);

    private static Sample Create(Settings settings)
    {
        return new GhostBumps(settings);
    }

    public GhostBumps(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(1.5f, 16.0f);
            B2.g_camera.m_zoom = 25.0f * 0.8f;
        }

        m_groundId = b2_nullBodyId;
        m_bodyId = b2_nullBodyId;
        m_shapeId = b2_nullShapeId;
        m_shapeType = ShapeType.e_circleShape;
        m_round = 0.0f;
        m_friction = 0.2f;
        m_bevel = 0.0f;
        m_useChain = true;

        CreateScene();
        Launch();
    }

    void CreateScene()
    {
        if (B2_IS_NON_NULL(m_groundId))
        {
            b2DestroyBody(m_groundId);
        }

        m_shapeId = b2_nullShapeId;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        m_groundId = b2CreateBody(m_worldId, ref bodyDef);

        float m = 1.0f / MathF.Sqrt(2.0f);
        float mm = 2.0f * (MathF.Sqrt(2.0f) - 1.0f);
        float hx = 4.0f, hy = 0.25f;

        if (m_useChain)
        {
            B2Vec2[] points = new B2Vec2[20];
            points[0] = new B2Vec2(-3.0f * hx, hy);
            points[1] = b2Add(points[0], new B2Vec2(-2.0f * hx * m, 2.0f * hx * m));
            points[2] = b2Add(points[1], new B2Vec2(-2.0f * hx * m, 2.0f * hx * m));
            points[3] = b2Add(points[2], new B2Vec2(-2.0f * hx * m, 2.0f * hx * m));
            points[4] = b2Add(points[3], new B2Vec2(-2.0f * hy * m, -2.0f * hy * m));
            points[5] = b2Add(points[4], new B2Vec2(2.0f * hx * m, -2.0f * hx * m));
            points[6] = b2Add(points[5], new B2Vec2(2.0f * hx * m, -2.0f * hx * m));
            points[7] = b2Add(points[6], new B2Vec2(2.0f * hx * m + 2.0f * hy * (1.0f - m), -2.0f * hx * m - 2.0f * hy * (1.0f - m)));
            points[8] = b2Add(points[7], new B2Vec2(2.0f * hx + hy * mm, 0.0f));
            points[9] = b2Add(points[8], new B2Vec2(2.0f * hx, 0.0f));
            points[10] = b2Add(points[9], new B2Vec2(2.0f * hx + hy * mm, 0.0f));
            points[11] = b2Add(points[10], new B2Vec2(2.0f * hx * m + 2.0f * hy * (1.0f - m), 2.0f * hx * m + 2.0f * hy * (1.0f - m)));
            points[12] = b2Add(points[11], new B2Vec2(2.0f * hx * m, 2.0f * hx * m));
            points[13] = b2Add(points[12], new B2Vec2(2.0f * hx * m, 2.0f * hx * m));
            points[14] = b2Add(points[13], new B2Vec2(-2.0f * hy * m, 2.0f * hy * m));
            points[15] = b2Add(points[14], new B2Vec2(-2.0f * hx * m, -2.0f * hx * m));
            points[16] = b2Add(points[15], new B2Vec2(-2.0f * hx * m, -2.0f * hx * m));
            points[17] = b2Add(points[16], new B2Vec2(-2.0f * hx * m, -2.0f * hx * m));
            points[18] = b2Add(points[17], new B2Vec2(-2.0f * hx, 0.0f));
            points[19] = b2Add(points[18], new B2Vec2(-2.0f * hx, 0.0f));

            B2SurfaceMaterial material = new B2SurfaceMaterial();
            material.friction = m_friction;

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = 20;
            chainDef.isLoop = true;
            chainDef.materials = new[] { material };
            chainDef.materialCount = 1;

            b2CreateChain(m_groundId, ref chainDef);
        }
        else
        {
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = m_friction;

            B2Hull hull = new B2Hull();

            if (m_bevel > 0.0f)
            {
                float hb = m_bevel;
                B2Vec2[] vs = new B2Vec2[8]
                {
                    new B2Vec2(hx + hb, hy - 0.05f),
                    new B2Vec2(hx, hy),
                    new B2Vec2(-hx, hy),
                    new B2Vec2(-hx - hb, hy - 0.05f),
                    new B2Vec2(-hx - hb, -hy + 0.05f),
                    new B2Vec2(-hx, -hy),
                    new B2Vec2(hx, -hy),
                    new B2Vec2(hx + hb, -hy + 0.05f),
                };
                hull = b2ComputeHull(vs, 8);
            }
            else
            {
                B2Vec2[] vs = new B2Vec2[4]
                {
                    new B2Vec2(hx, hy),
                    new B2Vec2(-hx, hy),
                    new B2Vec2(-hx, -hy),
                    new B2Vec2(hx, -hy),
                };
                hull = b2ComputeHull(vs, 4);
            }

            B2Transform transform;
            float x, y;

            // Left slope
            x = -3.0f * hx - m * hx - m * hy;
            y = hy + m * hx - m * hy;
            transform.q = b2MakeRot(-0.25f * B2_PI);

            {
                transform.p = new B2Vec2(x, y);
                B2Polygon polygon = b2MakeOffsetPolygon(hull, transform.p, transform.q);
                b2CreatePolygonShape(m_groundId, shapeDef, polygon);
                x -= 2.0f * m * hx;
                y += 2.0f * m * hx;
            }
            {
                transform.p = new B2Vec2(x, y);
                B2Polygon polygon = b2MakeOffsetPolygon(hull, transform.p, transform.q);
                b2CreatePolygonShape(m_groundId, shapeDef, polygon);
                x -= 2.0f * m * hx;
                y += 2.0f * m * hx;
            }
            {
                transform.p = new B2Vec2(x, y);
                B2Polygon polygon = b2MakeOffsetPolygon(hull, transform.p, transform.q);
                b2CreatePolygonShape(m_groundId, shapeDef, polygon);
                x -= 2.0f * m * hx;
                y += 2.0f * m * hx;
            }

            x = -2.0f * hx;
            y = 0.0f;
            transform.q = b2MakeRot(0.0f);

            {
                transform.p = new B2Vec2(x, y);
                B2Polygon polygon = b2MakeOffsetPolygon(hull, transform.p, transform.q);
                b2CreatePolygonShape(m_groundId, shapeDef, polygon);
                x += 2.0f * hx;
            }
            {
                transform.p = new B2Vec2(x, y);
                B2Polygon polygon = b2MakeOffsetPolygon(hull, transform.p, transform.q);
                b2CreatePolygonShape(m_groundId, shapeDef, polygon);
                x += 2.0f * hx;
            }
            {
                transform.p = new B2Vec2(x, y);
                B2Polygon polygon = b2MakeOffsetPolygon(hull, transform.p, transform.q);
                b2CreatePolygonShape(m_groundId, shapeDef, polygon);
                x += 2.0f * hx;
            }

            x = 3.0f * hx + m * hx + m * hy;
            y = hy + m * hx - m * hy;
            transform.q = b2MakeRot(0.25f * B2_PI);

            {
                transform.p = new B2Vec2(x, y);
                B2Polygon polygon = b2MakeOffsetPolygon(hull, transform.p, transform.q);
                b2CreatePolygonShape(m_groundId, shapeDef, polygon);
                x += 2.0f * m * hx;
                y += 2.0f * m * hx;
            }
            {
                transform.p = new B2Vec2(x, y);
                B2Polygon polygon = b2MakeOffsetPolygon(hull, transform.p, transform.q);
                b2CreatePolygonShape(m_groundId, shapeDef, polygon);
                x += 2.0f * m * hx;
                y += 2.0f * m * hx;
            }
            {
                transform.p = new B2Vec2(x, y);
                B2Polygon polygon = b2MakeOffsetPolygon(hull, transform.p, transform.q);
                b2CreatePolygonShape(m_groundId, shapeDef, polygon);
                x += 2.0f * m * hx;
                y += 2.0f * m * hx;
            }
        }
    }

    void Launch()
    {
        if (B2_IS_NON_NULL(m_bodyId))
        {
            b2DestroyBody(m_bodyId);
            m_shapeId = b2_nullShapeId;
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(-28.0f, 18.0f);
        bodyDef.linearVelocity = new B2Vec2(0.0f, 0.0f);
        m_bodyId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.friction = m_friction;

        if (m_shapeType == ShapeType.e_circleShape)
        {
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            m_shapeId = b2CreateCircleShape(m_bodyId, shapeDef, circle);
        }
        else if (m_shapeType == ShapeType.e_capsuleShape)
        {
            B2Capsule capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);
            m_shapeId = b2CreateCapsuleShape(m_bodyId, shapeDef, capsule);
        }
        else
        {
            float h = 0.5f - m_round;
            B2Polygon box = b2MakeRoundedBox(h, 2.0f * h, m_round);
            m_shapeId = b2CreatePolygonShape(m_bodyId, shapeDef, box);
        }
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 140.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, B2.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(180.0f, height));

        ImGui.Begin("Ghost Bumps", ref open, ImGuiWindowFlags.NoResize);
        ImGui.PushItemWidth(100.0f);

        if (ImGui.Checkbox("Chain", ref m_useChain))
        {
            CreateScene();
        }

        if (m_useChain == false)
        {
            if (ImGui.SliderFloat("Bevel", ref m_bevel, 0.0f, 1.0f, "%.2f"))
            {
                CreateScene();
            }
        }

        {
            string[] shapeTypes = { "Circle", "Capsule", "Box" };
            int shapeType = (int)m_shapeType;
            ImGui.Combo("Shape", ref shapeType, shapeTypes, shapeTypes.Length);
            m_shapeType = (ShapeType)shapeType;
        }

        if (m_shapeType == ShapeType.e_boxShape)
        {
            ImGui.SliderFloat("Round", ref m_round, 0.0f, 0.4f, "%.1f");
        }

        if (ImGui.SliderFloat("Friction", ref m_friction, 0.0f, 1.0f, "%.1f"))
        {
            if (B2_IS_NON_NULL(m_shapeId))
            {
                b2Shape_SetFriction(m_shapeId, m_friction);
            }

            CreateScene();
        }

        if (ImGui.Button("Launch"))
        {
            Launch();
        }

        ImGui.PopItemWidth();
        ImGui.End();
    }
}
