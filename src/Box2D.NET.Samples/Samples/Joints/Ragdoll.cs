﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Primitives;
using Box2D.NET.Shared.Primitives;
using ImGuiNET;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.Shared.human;

namespace Box2D.NET.Samples.Samples.Joints;

public class Ragdoll : Sample
{
    Human m_human;
    float m_jointFrictionTorque;
    float m_jointHertz;
    float m_jointDampingRatio;
    static int sampleRagdoll = RegisterSample("Joints", "Ragdoll", Create);

    static Sample Create(Settings settings)
    {
        return new Ragdoll(settings);
    }

    public Ragdoll(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 12.0f);
            Draw.g_camera.m_zoom = 16.0f;

            // Draw.g_camera.m_center = { 0.0f, 26.0f };
            // Draw.g_camera.m_zoom = 1.0f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment = new b2Segment(new b2Vec2(-20.0f, 0.0f), new b2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        m_jointFrictionTorque = 0.03f;
        m_jointHertz = 5.0f;
        m_jointDampingRatio = 0.5f;

        m_human = new Human();

        Spawn();
    }

    void Spawn()
    {
        CreateHuman(m_human, m_worldId, new b2Vec2(0.0f, 25.0f), 1.0f, m_jointFrictionTorque, m_jointHertz, m_jointDampingRatio, 1, null, false);
        Human_ApplyRandomAngularImpulse(m_human, 10.0f);
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 140.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(180.0f, height));

        ImGui.Begin("Ragdoll", ref open, ImGuiWindowFlags.NoResize);
        ImGui.PushItemWidth(100.0f);

        if (ImGui.SliderFloat("Friction", ref m_jointFrictionTorque, 0.0f, 1.0f, "%3.2f"))
        {
            Human_SetJointFrictionTorque(m_human, m_jointFrictionTorque);
        }

        if (ImGui.SliderFloat("Hertz", ref m_jointHertz, 0.0f, 10.0f, "%3.1f"))
        {
            Human_SetJointSpringHertz(m_human, m_jointHertz);
        }

        if (ImGui.SliderFloat("Damping", ref m_jointDampingRatio, 0.0f, 4.0f, "%3.1f"))
        {
            Human_SetJointDampingRatio(m_human, m_jointDampingRatio);
        }

        if (ImGui.Button("Respawn"))
        {
            DestroyHuman(m_human);
            Spawn();
        }

        ImGui.PopItemWidth();
        ImGui.End();
    }
}
