﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2RevoluteJoints;

namespace Box2D.NET.Samples.Samples.Joints;

public class BallAndChain : Sample
{
    private static readonly int SampleBallAndChainIndex = SampleFactory.Shared.RegisterSample("Joints", "Ball & Chain", Create);

    public const int m_count = 30;

    private B2JointId[] m_jointIds = new B2JointId[m_count + 1];
    private float m_frictionTorque;


    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BallAndChain(ctx, settings);
    }

    public BallAndChain(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, -8.0f);
            m_context.camera.m_zoom = 27.5f;
        }

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);
        }

        m_frictionTorque = 100.0f;

        {
            float hx = 0.5f;
            B2Capsule capsule = new B2Capsule(new B2Vec2(-hx, 0.0f), new B2Vec2(hx, 0.0f), 0.125f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();

            int jointIndex = 0;

            B2BodyId prevBodyId = groundId;
            for (int i = 0; i < m_count; ++i)
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2((1.0f + 2.0f * i) * hx, m_count * hx);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);

                B2Vec2 pivot = new B2Vec2((2.0f * i) * hx, m_count * hx);
                jointDef.bodyIdA = prevBodyId;
                jointDef.bodyIdB = bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                // jointDef.enableMotor = true;
                jointDef.maxMotorTorque = m_frictionTorque;
                m_jointIds[jointIndex++] = b2CreateRevoluteJoint(m_worldId, ref jointDef);

                prevBodyId = bodyId;
            }

            {
                B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 4.0f);

                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2((1.0f + 2.0f * m_count) * hx + circle.radius - hx, m_count * hx);

                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreateCircleShape(bodyId, ref shapeDef, ref circle);

                B2Vec2 pivot = new B2Vec2((2.0f * m_count) * hx, m_count * hx);
                jointDef.bodyIdA = prevBodyId;
                jointDef.bodyIdB = bodyId;
                jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
                jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
                jointDef.enableMotor = true;
                jointDef.maxMotorTorque = m_frictionTorque;
                m_jointIds[jointIndex++] = b2CreateRevoluteJoint(m_worldId, ref jointDef);
                Debug.Assert(jointIndex == m_count + 1);
            }
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float height = 60.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Ball and Chain", ImGuiWindowFlags.NoResize);

        bool updateFriction = ImGui.SliderFloat("Joint Friction", ref m_frictionTorque, 0.0f, 1000.0f, "%2.f");
        if (updateFriction)
        {
            for (int i = 0; i <= m_count; ++i)
            {
                b2RevoluteJoint_SetMaxMotorTorque(m_jointIds[i], m_frictionTorque);
            }
        }

        ImGui.End();
    }
}