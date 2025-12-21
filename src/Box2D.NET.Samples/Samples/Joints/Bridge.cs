// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples.Joints;

// A suspension bridge
public class Bridge : Sample
{
    private static readonly int SampleBridgeIndex = SampleFactory.Shared.RegisterSample("Joints", "Bridge", Create);

    public const int m_count = 160;

    private B2BodyId[] m_bodyIds = new B2BodyId[m_count];
    private B2JointId[] m_jointIds = new B2JointId[m_count + 1];
    private float m_frictionTorque;
    private float m_constraintHertz;
    private float m_constraintDampingRatio;
    private float m_springHertz;
    private float m_springDampingRatio;

    private static Sample Create(SampleContext context)
    {
        return new Bridge(context);
    }


    public Bridge(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.zoom = 25.0f * 2.5f;
        }

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);
        }

        {
            m_constraintHertz = 60.0f;
            m_constraintDampingRatio = 0.0f;
            m_springHertz = 2.0f;
            m_springDampingRatio = 0.7f;
            m_frictionTorque = 200.0f;

            B2Polygon box = b2MakeBox(0.5f, 0.125f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.enableMotor = true;
            jointDef.maxMotorTorque = m_frictionTorque;
            jointDef.enableSpring = true;
            jointDef.hertz = m_springHertz;
            jointDef.dampingRatio = m_springDampingRatio;

            int jointIndex = 0;

            float xbase = -80.0f;

            B2BodyId prevBodyId = groundId;
            for (int i = 0; i < m_count; ++i)
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(xbase + 0.5f + 1.0f * i, 20.0f);
                bodyDef.linearDamping = 0.1f;
                bodyDef.angularDamping = 0.1f;
                m_bodyIds[i] = b2CreateBody(m_worldId, bodyDef);
                b2CreatePolygonShape(m_bodyIds[i], shapeDef, box);

                B2Vec2 pivot = new B2Vec2(xbase + 1.0f * i, 20.0f);
                jointDef.@base.bodyIdA = prevBodyId;
                jointDef.@base.bodyIdB = m_bodyIds[i];
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                m_jointIds[jointIndex++] = b2CreateRevoluteJoint(m_worldId, jointDef);

                prevBodyId = m_bodyIds[i];
            }

            {
                B2Vec2 pivot = new B2Vec2(xbase + 1.0f * m_count, 20.0f);
                jointDef.@base.bodyIdA = prevBodyId;
                jointDef.@base.bodyIdB = groundId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                m_jointIds[jointIndex++] = b2CreateRevoluteJoint(m_worldId, jointDef);

                B2_ASSERT(jointIndex == m_count + 1);
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

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 180.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(320.0f, height));

        ImGui.Begin("Bridge", ImGuiWindowFlags.NoResize);

        ImGui.PushItemWidth(ImGui.GetWindowWidth() * 0.6f);
        bool updateFriction = ImGui.SliderFloat("Joint Friction", ref m_frictionTorque, 0.0f, 10000.0f, "%2.f");
        if (updateFriction)
        {
            for (int i = 0; i <= m_count; ++i)
            {
                b2RevoluteJoint_SetMaxMotorTorque(m_jointIds[i], m_frictionTorque);
            }
        }

        if (ImGui.SliderFloat("Spring hertz", ref m_springHertz, 0.0f, 30.0f, "%.0f"))
        {
            for (int i = 0; i <= m_count; ++i)
            {
                b2RevoluteJoint_SetSpringHertz(m_jointIds[i], m_springHertz);
            }
        }

        if (ImGui.SliderFloat("Spring damping", ref m_springDampingRatio, 0.0f, 2.0f, "%.1f"))
        {
            for (int i = 0; i <= m_count; ++i)
            {
                b2RevoluteJoint_SetSpringDampingRatio(m_jointIds[i], m_springDampingRatio);
            }
        }

        if (ImGui.SliderFloat("Constraint hertz", ref m_constraintHertz, 15.0f, 240.0f, "%.0f"))
        {
            for (int i = 0; i <= m_count; ++i)
            {
                b2Joint_SetConstraintTuning(m_jointIds[i], m_constraintHertz, m_constraintDampingRatio);
            }
        }

        if (ImGui.SliderFloat("Constraint damping", ref m_constraintDampingRatio, 0.0f, 10.0f, "%.1f"))
        {
            for (int i = 0; i <= m_count; ++i)
            {
                b2Joint_SetConstraintTuning(m_jointIds[i], m_constraintHertz, m_constraintDampingRatio);
            }
        }

        ImGui.PopItemWidth();

        ImGui.End();
    }
}