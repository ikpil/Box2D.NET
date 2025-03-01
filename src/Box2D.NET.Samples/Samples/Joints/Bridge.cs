// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2RevoluteJoints;

namespace Box2D.NET.Samples.Samples.Joints;

// A suspension bridge
public class Bridge : Sample
{
    public const int e_count = 160;

    B2BodyId[] m_bodyIds = new B2BodyId[e_count];
    B2JointId[] m_jointIds = new B2JointId[e_count + 1];
    float m_frictionTorque;
    float m_gravityScale;
    private static readonly int SampleBridgeIndex = SampleFactory.Shared.RegisterSample("Joints", "Bridge", Create);

    private static Sample Create(Settings settings)
    {
        return new Bridge(settings);
    }


    public Bridge(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_zoom = 25.0f * 2.5f;
        }

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);
        }

        {
            B2Polygon box = b2MakeBox(0.5f, 0.125f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            int jointIndex = 0;
            m_frictionTorque = 200.0f;
            m_gravityScale = 1.0f;

            float xbase = -80.0f;

            B2BodyId prevBodyId = groundId;
            for (int i = 0; i < e_count; ++i)
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(xbase + 0.5f + 1.0f * i, 20.0f);
                bodyDef.linearDamping = 0.1f;
                bodyDef.angularDamping = 0.1f;
                m_bodyIds[i] = b2CreateBody(m_worldId, bodyDef);
                b2CreatePolygonShape(m_bodyIds[i], shapeDef, box);

                B2Vec2 pivot = new B2Vec2(xbase + 1.0f * i, 20.0f);
                jointDef.bodyIdA = prevBodyId;
                jointDef.bodyIdB = m_bodyIds[i];
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableMotor = true;
                jointDef.maxMotorTorque = m_frictionTorque;
                m_jointIds[jointIndex++] = b2CreateRevoluteJoint(m_worldId, jointDef);

                prevBodyId = m_bodyIds[i];
            }

            {
                B2Vec2 pivot = new B2Vec2(xbase + 1.0f * e_count, 20.0f);
                jointDef.bodyIdA = prevBodyId;
                jointDef.bodyIdB = groundId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableMotor = true;
                jointDef.maxMotorTorque = m_frictionTorque;
                m_jointIds[jointIndex++] = b2CreateRevoluteJoint(m_worldId, jointDef);

                Debug.Assert(jointIndex == e_count + 1);
            }
        }

        for (int i = 0; i < 2; ++i)
        {
            B2Vec2[] vertices = new B2Vec2[3] { new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), new B2Vec2(0.0f, 1.5f) };

            B2Hull hull = b2ComputeHull(vertices, 3);
            B2Polygon triangle = b2MakePolygon(hull, 0.0f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-8.0f + 8.0f * i, 22.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(bodyId, shapeDef, triangle);
        }

        for (int i = 0; i < 3; ++i)
        {
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-6.0f + 6.0f * i, 25.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            b2CreateCircleShape(bodyId, shapeDef, circle);
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 80.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, B2.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Bridge", ref open, ImGuiWindowFlags.NoResize);

        // Slider takes half the window
        ImGui.PushItemWidth(ImGui.GetWindowWidth() * 0.5f);
        bool updateFriction = ImGui.SliderFloat("Joint Friction", ref m_frictionTorque, 0.0f, 1000.0f, "%2.f");
        if (updateFriction)
        {
            for (int i = 0; i <= e_count; ++i)
            {
                b2RevoluteJoint_SetMaxMotorTorque(m_jointIds[i], m_frictionTorque);
            }
        }

        if (ImGui.SliderFloat("Gravity scale", ref m_gravityScale, -1.0f, 1.0f, "%.1f"))
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2Body_SetGravityScale(m_bodyIds[i], m_gravityScale);
            }
        }

        ImGui.End();
    }
}
