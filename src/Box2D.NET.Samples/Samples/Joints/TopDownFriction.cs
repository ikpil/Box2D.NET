// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.Shared.RandomSupports;

namespace Box2D.NET.Samples.Samples.Joints;

public class TopDownFriction : Sample
{
    private static int sampleTopDownFriction = SampleFactory.Shared.RegisterSample("Joints", "Top Down Friction", Create);

    private static Sample Create(SampleContext context)
    {
        return new TopDownFriction(context);
    }


    private TopDownFriction(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 7.0f);
            m_context.camera.m_zoom = 25.0f * 0.4f;
        }

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(10.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

            segment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(-10.0f, 20.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

            segment = new B2Segment(new B2Vec2(10.0f, 0.0f), new B2Vec2(10.0f, 20.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

            segment = new B2Segment(new B2Vec2(-10.0f, 20.0f), new B2Vec2(10.0f, 20.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        B2MotorJointDef jointDef = b2DefaultMotorJointDef();
        jointDef.@base.bodyIdA = groundId;
        jointDef.@base.collideConnected = true;
        jointDef.maxVelocityForce = 10.0f;
        jointDef.maxVelocityTorque = 10.0f;

        B2Capsule capsule = new B2Capsule(new B2Vec2(-0.25f, 0.0f), new B2Vec2(0.25f, 0.0f), 0.25f);
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.35f);
        B2Polygon square = b2MakeSquare(0.35f);

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.gravityScale = 0.0f;
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.restitution = 0.8f;

            int n = 10;
            float x = -5.0f, y = 15.0f;
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    bodyDef.position = new B2Vec2(x, y);
                    B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

                    int remainder = (n * i + j) % 4;
                    if (remainder == 0)
                    {
                        b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);
                    }
                    else if (remainder == 1)
                    {
                        b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
                    }
                    else if (remainder == 2)
                    {
                        b2CreatePolygonShape(bodyId, ref shapeDef, ref square);
                    }
                    else
                    {
                        B2Polygon poly = RandomPolygon(0.75f);
                        poly.radius = 0.1f;
                        b2CreatePolygonShape(bodyId, ref shapeDef, ref poly);
                    }

                    jointDef.@base.bodyIdB = bodyId;
                    b2CreateMotorJoint(m_worldId, ref jointDef);

                    x += 1.0f;
                }

                x = -5.0f;
                y -= 1.0f;
            }
        }
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 180.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.m_height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Top Down Friction", ImGuiWindowFlags.NoResize);

        if (ImGui.Button("Explode"))
        {
            B2ExplosionDef def = b2DefaultExplosionDef();
            def.position = new B2Vec2(0.0f, 10.0f);
            def.radius = 10.0f;
            def.falloff = 5.0f;
            def.impulsePerLength = 10.0f;
            b2World_Explode(m_worldId, ref def);

            m_draw.DrawCircle(def.position, 10.0f, B2HexColor.b2_colorWhite);
        }

        ImGui.End();
    }
}