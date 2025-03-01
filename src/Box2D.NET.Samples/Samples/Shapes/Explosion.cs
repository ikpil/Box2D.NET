// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2WeldJoints;

namespace Box2D.NET.Samples.Samples.Shapes;

// This shows how to use explosions and demonstrates the projected perimeter
public class Explosion : Sample
{
    List<B2JointId> m_jointIds = new List<B2JointId>();
    float m_radius;
    float m_falloff;
    float m_impulse;
    float m_referenceAngle;

    private static readonly int SampleExplosion = SampleFactory.Shared.RegisterSample("Shapes", "Explosion", Create);

    private static Sample Create(Settings settings)
    {
        return new Explosion(settings);
    }

    public Explosion(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.0f, 0.0f);
            B2.g_camera.m_zoom = 14.0f;
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.gravityScale = 0.0f;
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        m_referenceAngle = 0.0f;

        B2WeldJointDef weldDef = b2DefaultWeldJointDef();
        weldDef.referenceAngle = m_referenceAngle;
        weldDef.angularHertz = 0.5f;
        weldDef.angularDampingRatio = 0.7f;
        weldDef.linearHertz = 0.5f;
        weldDef.linearDampingRatio = 0.7f;
        weldDef.bodyIdA = groundId;
        weldDef.localAnchorB = b2Vec2_zero;

        float r = 8.0f;
        for (float angle = 0.0f; angle < 360.0f; angle += 30.0f)
        {
            B2CosSin cosSin = b2ComputeCosSin(angle * B2_PI / 180.0f);
            bodyDef.position = new B2Vec2(r * cosSin.cosine, r * cosSin.sine);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeBox(1.0f, 0.1f);
            b2CreatePolygonShape(bodyId, shapeDef, box);

            weldDef.localAnchorA = bodyDef.position;
            weldDef.bodyIdB = bodyId;
            B2JointId jointId = b2CreateWeldJoint(m_worldId, weldDef);
            m_jointIds.Add(jointId);
        }

        m_radius = 7.0f;
        m_falloff = 3.0f;
        m_impulse = 10.0f;
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 160.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, B2.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Explosion", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (ImGui.Button("Explode"))
        {
            B2ExplosionDef def = b2DefaultExplosionDef();
            def.position = b2Vec2_zero;
            def.radius = m_radius;
            def.falloff = m_falloff;
            def.impulsePerLength = m_impulse;
            b2World_Explode(m_worldId, def);
        }

        ImGui.SliderFloat("radius", ref m_radius, 0.0f, 20.0f, "%.1f");
        ImGui.SliderFloat("falloff", ref m_falloff, 0.0f, 20.0f, "%.1f");
        ImGui.SliderFloat("impulse", ref m_impulse, -20.0f, 20.0f, "%.1f");

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        if (settings.pause == false || settings.singleStep == true)
        {
            m_referenceAngle += settings.hertz > 0.0f ? 60.0f * B2_PI / 180.0f / settings.hertz : 0.0f;
            m_referenceAngle = b2UnwindAngle(m_referenceAngle);

            int count = m_jointIds.Count;
            for (int i = 0; i < count; ++i)
            {
                b2WeldJoint_SetReferenceAngle(m_jointIds[i], m_referenceAngle);
            }
        }

        base.Step(settings);

        B2.g_draw.DrawString(5, m_textLine, "reference angle = %g", m_referenceAngle);
        m_textLine += m_textIncrement;

        B2.g_draw.DrawCircle(b2Vec2_zero, m_radius + m_falloff, B2HexColor.b2_colorBox2DBlue);
        B2.g_draw.DrawCircle(b2Vec2_zero, m_radius, B2HexColor.b2_colorBox2DYellow);
    }
}
