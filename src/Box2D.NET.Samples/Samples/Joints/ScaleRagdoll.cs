// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Shared;
using ImGuiNET;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.Shared.Humans;

namespace Box2D.NET.Samples.Samples.Joints;

public class ScaleRagdoll : Sample
{
    private static readonly int SampleScaleRagdoll = SampleFactory.Shared.RegisterSample("Joints", "Scale Ragdoll", Create);

    private Human m_human;
    private float m_scale;

    private static Sample Create(SampleContext context)
    {
        return new ScaleRagdoll(context);
    }

    public ScaleRagdoll(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 4.5f);
            m_context.camera.zoom = 6.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            B2Polygon box = b2MakeOffsetBox(20.0f, 1.0f, new B2Vec2(0.0f, -1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        m_scale = 1.0f;

        m_human = new Human();

        Spawn();
    }

    void Spawn()
    {
        float jointFrictionTorque = 0.03f;
        float jointHertz = 1.0f;
        float jointDampingRatio = 0.5f;
        CreateHuman(ref m_human, m_worldId, new B2Vec2(0.0f, 5.0f), m_scale, jointFrictionTorque, jointHertz, jointDampingRatio, 1, B2UserData.Empty, false);
        Human_ApplyRandomAngularImpulse(ref m_human, 0.1f);
    }

    public override void BuildSamplePanel()
    {

        ImGui.PushItemWidth(6.0f * ImGui.GetFontSize());

        if (ImGui.SliderFloat("Scale", ref m_scale, 0.1f, 10.0f, "%3.2f", ImGuiSliderFlags.AlwaysClamp))
        {
            Human_SetScale(ref m_human, m_scale);
        }

        ImGui.PopItemWidth();

    }
}
