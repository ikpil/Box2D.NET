// SPDX-FileCopyrightText: 2022 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Bodies;

public class Weeble : Sample
{
    private static readonly int SampleWeeble = SampleFactory.Shared.RegisterSample("Bodies", "Weeble", Create);
    
    private B2BodyId m_weebleId;
    private B2Vec2 m_explosionPosition;
    private float m_explosionRadius;
    private float m_explosionMagnitude;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new Weeble(ctx, settings);
    }

    public Weeble(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(2.3f, 10.0f);
            m_context.camera.m_zoom = 25.0f * 0.5f;
        }

        // Test friction and restitution callbacks
        b2World_SetFrictionCallback(m_worldId, FrictionCallback);
        b2World_SetRestitutionCallback(m_worldId, RestitutionCallback);

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        // Build weeble
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 3.0f);
            bodyDef.rotation = b2MakeRot(0.25f * B2_PI);
            m_weebleId = b2CreateBody(m_worldId, ref bodyDef);

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -1.0f), new B2Vec2(0.0f, 1.0f), 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateCapsuleShape(m_weebleId, ref shapeDef, ref capsule);

            float mass = b2Body_GetMass(m_weebleId);
            float inertiaTensor = b2Body_GetRotationalInertia(m_weebleId);

            float offset = 1.5f;

            // See: https://en.wikipedia.org/wiki/Parallel_axis_theorem
            inertiaTensor += mass * offset * offset;

            B2MassData massData = new B2MassData(mass, new B2Vec2(0.0f, -offset), inertiaTensor);
            b2Body_SetMassData(m_weebleId, massData);
        }

        m_explosionPosition = new B2Vec2(0.0f, 0.0f);
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


    public override void UpdateGui()
    {
        base.UpdateGui();
        
        float height = 120.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));
        ImGui.Begin("Weeble", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        if (ImGui.Button("Teleport"))
        {
            b2Body_SetTransform(m_weebleId, new B2Vec2(0.0f, 5.0f), b2MakeRot(0.95f * B2_PI));
        }

        if (ImGui.Button("Explode"))
        {
            B2ExplosionDef def = b2DefaultExplosionDef();
            def.position = m_explosionPosition;
            def.radius = m_explosionRadius;
            def.falloff = 0.1f;
            def.impulsePerLength = m_explosionMagnitude;
            b2World_Explode(m_worldId, ref def);
        }

        ImGui.PushItemWidth(100.0f);

        ImGui.SliderFloat("Magnitude", ref m_explosionMagnitude, -100.0f, 100.0f, "%.1f");

        ImGui.PopItemWidth();
        ImGui.End();
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        m_context.draw.DrawCircle(m_explosionPosition, m_explosionRadius, B2HexColor.b2_colorAzure);

        // This shows how to get the velocity of a point on a body
        B2Vec2 localPoint = new B2Vec2(0.0f, 2.0f);
        B2Vec2 worldPoint = b2Body_GetWorldPoint(m_weebleId, localPoint);

        B2Vec2 v1 = b2Body_GetLocalPointVelocity(m_weebleId, localPoint);
        B2Vec2 v2 = b2Body_GetWorldPointVelocity(m_weebleId, worldPoint);

        B2Vec2 offset = new B2Vec2(0.05f, 0.0f);
        m_context.draw.DrawSegment(worldPoint, worldPoint + v1, B2HexColor.b2_colorRed);
        m_context.draw.DrawSegment(worldPoint + offset, worldPoint + v2 + offset, B2HexColor.b2_colorGreen);
    }
}
