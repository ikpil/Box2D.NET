// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2WeldJoints;

namespace Box2D.NET.Samples.Samples.Joints;

// This sample shows the limitations of an iterative solver. The cantilever sags even though the weld
// joint is stiff as possible.
public class Cantilever : Sample
{
    private static readonly int SampleCantileverIndex = SampleFactory.Shared.RegisterSample("Joints", "Cantilever", Create);

    public const int e_count = 8;

    private float m_linearHertz;
    private float m_linearDampingRatio;
    private float m_angularHertz;
    private float m_angularDampingRatio;
    private float m_gravityScale;
    private B2BodyId m_tipId;
    private B2BodyId[] m_bodyIds = new B2BodyId[e_count];
    private B2JointId[] m_jointIds = new B2JointId[e_count];
    private bool m_collideConnected;


    private static Sample Create(SampleContext context)
    {
        return new Cantilever(context);
    }


    public Cantilever(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 0.0f);
            m_camera.m_zoom = 25.0f * 0.35f;
        }

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);
        }

        {
            m_linearHertz = 15.0f;
            m_linearDampingRatio = 0.5f;
            m_angularHertz = 5.0f;
            m_angularDampingRatio = 0.5f;
            m_gravityScale = 1.0f;
            m_collideConnected = false;

            float hx = 0.5f;
            B2Capsule capsule = new B2Capsule(new B2Vec2(-hx, 0.0f), new B2Vec2(hx, 0.0f), 0.125f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;

            B2WeldJointDef jointDef = b2DefaultWeldJointDef();

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.isAwake = false;

            B2BodyId prevBodyId = groundId;
            for (int i = 0; i < e_count; ++i)
            {
                bodyDef.position = new B2Vec2((1.0f + 2.0f * i) * hx, 0.0f);
                m_bodyIds[i] = b2CreateBody(m_worldId, ref bodyDef);
                b2CreateCapsuleShape(m_bodyIds[i], ref shapeDef, ref capsule);

                B2Vec2 pivot = new B2Vec2((2.0f * i) * hx, 0.0f);
                jointDef.@base.bodyIdA = prevBodyId;
                jointDef.@base.bodyIdB = m_bodyIds[i];
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.linearHertz = m_linearHertz;
                jointDef.linearDampingRatio = m_linearDampingRatio;
                jointDef.angularHertz = m_angularHertz;
                jointDef.angularDampingRatio = m_angularDampingRatio;
                jointDef.@base.collideConnected = m_collideConnected;
                m_jointIds[i] = b2CreateWeldJoint(m_worldId, ref jointDef);

                // Experimental tuning
                b2Joint_SetConstraintTuning(m_jointIds[i], 120.0f, 10.0f);

                prevBodyId = m_bodyIds[i];
            }

            m_tipId = prevBodyId;
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float height = 180.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Cantilever", ImGuiWindowFlags.NoResize);
        ImGui.PushItemWidth(100.0f);

        if (ImGui.SliderFloat("Linear Hertz", ref m_linearHertz, 0.0f, 20.0f, "%.0f"))
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2WeldJoint_SetLinearHertz(m_jointIds[i], m_linearHertz);
            }
        }

        if (ImGui.SliderFloat("Linear Damping Ratio", ref m_linearDampingRatio, 0.0f, 10.0f, "%.1f"))
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2WeldJoint_SetLinearDampingRatio(m_jointIds[i], m_linearDampingRatio);
            }
        }

        if (ImGui.SliderFloat("Angular Hertz", ref m_angularHertz, 0.0f, 20.0f, "%.0f"))
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2WeldJoint_SetAngularHertz(m_jointIds[i], m_angularHertz);
            }
        }

        if (ImGui.SliderFloat("Angular Damping Ratio", ref m_angularDampingRatio, 0.0f, 10.0f, "%.1f"))
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2WeldJoint_SetAngularDampingRatio(m_jointIds[i], m_angularDampingRatio);
            }
        }

        if (ImGui.Checkbox("Collide Connected", ref m_collideConnected))
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2Joint_SetCollideConnected(m_jointIds[i], m_collideConnected);
            }
        }

        if (ImGui.SliderFloat("Gravity Scale", ref m_gravityScale, -1.0f, 1.0f, "%.1f"))
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2Body_SetGravityScale(m_bodyIds[i], m_gravityScale);
            }
        }

        ImGui.PopItemWidth();
        ImGui.End();
    }


    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        B2Vec2 tipPosition = b2Body_GetPosition(m_tipId);
        DrawTextLine($"tip-y = {tipPosition.Y:F2}");
    }
}