// SPDX-FileCopyrightText: 2022 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Bodies;

public class Weeble : Sample
{
    b2BodyId m_weebleId;
    b2Vec2 m_explosionPosition;
    float m_explosionRadius;
    float m_explosionMagnitude;

    private static int sampleWeeble = RegisterSample("Bodies", "Weeble", Create);

    static Sample Create(Settings settings)
    {
        return new Weeble(settings);
    }

    public Weeble(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(2.3f, 10.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.5f;
        }

        // Test friction and restitution callbacks
        b2World_SetFrictionCallback(m_worldId, FrictionCallback);
        b2World_SetRestitutionCallback(m_worldId, RestitutionCallback);

        b2BodyId groundId = b2_nullBodyId;
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);

            b2Segment segment = new b2Segment(new b2Vec2(-20.0f, 0.0f), new b2Vec2(20.0f, 0.0f));
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        // Build weeble
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = new b2Vec2(0.0f, 3.0f);
            bodyDef.rotation = b2MakeRot(0.25f * B2_PI);
            m_weebleId = b2CreateBody(m_worldId, bodyDef);

            b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -1.0f), new b2Vec2(0.0f, 1.0f), 1.0f);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            b2CreateCapsuleShape(m_weebleId, shapeDef, capsule);

            float mass = b2Body_GetMass(m_weebleId);
            float inertiaTensor = b2Body_GetRotationalInertia(m_weebleId);

            float offset = 1.5f;

            // See: https://en.wikipedia.org/wiki/Parallel_axis_theorem
            inertiaTensor += mass * offset * offset;

            b2MassData massData = new b2MassData(mass, new b2Vec2(0.0f, -offset), inertiaTensor);
            b2Body_SetMassData(m_weebleId, massData);
        }

        m_explosionPosition = new b2Vec2(0.0f, 0.0f);
        m_explosionRadius = 2.0f;
        m_explosionMagnitude = 8.0f;
    }

    static float FrictionCallback(float frictionA, int materialA, float frictionB, int materialB)
    {
        return 0.1f;
    }

    static float RestitutionCallback(float restitutionA, int materialA, float restitutionB, int materialB)
    {
        return 1.0f;
    }


    public override void UpdateUI()
    {
        bool open = false;
        float height = 120.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));
        ImGui.Begin("Weeble", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        if (ImGui.Button("Teleport"))
        {
            b2Body_SetTransform(m_weebleId, new b2Vec2(0.0f, 5.0f), b2MakeRot(0.95f * B2_PI));
        }

        if (ImGui.Button("Explode"))
        {
            b2ExplosionDef def = b2DefaultExplosionDef();
            def.position = m_explosionPosition;
            def.radius = m_explosionRadius;
            def.falloff = 0.1f;
            def.impulsePerLength = m_explosionMagnitude;
            b2World_Explode(m_worldId, def);
        }

        ImGui.PushItemWidth(100.0f);

        ImGui.SliderFloat("Magnitude", ref m_explosionMagnitude, -100.0f, 100.0f, "%.1f");

        ImGui.PopItemWidth();
        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        Draw.g_draw.DrawCircle(m_explosionPosition, m_explosionRadius, b2HexColor.b2_colorAzure);

        // This shows how to get the velocity of a point on a body
        b2Vec2 localPoint = new b2Vec2(0.0f, 2.0f);
        b2Vec2 worldPoint = b2Body_GetWorldPoint(m_weebleId, localPoint);

        b2Vec2 v1 = b2Body_GetLocalPointVelocity(m_weebleId, localPoint);
        b2Vec2 v2 = b2Body_GetWorldPointVelocity(m_weebleId, worldPoint);

        b2Vec2 offset = new b2Vec2(0.05f, 0.0f);
        Draw.g_draw.DrawSegment(worldPoint, worldPoint + v1, b2HexColor.b2_colorRed);
        Draw.g_draw.DrawSegment(worldPoint + offset, worldPoint + v2 + offset, b2HexColor.b2_colorGreen);
    }
}
