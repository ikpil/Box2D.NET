// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Issues;

public class DisableCrash : Sample
{
    private static readonly int SampleDisableCrash = SampleFactory.Shared.RegisterSample("Issues", "Disable", Create);

    private B2BodyId m_attachmentId;
    private B2BodyId m_platformId;
    bool m_isEnabled;

    private static Sample Create(SampleContext context)
    {
        return new DisableCrash(context);
    }

    public DisableCrash(SampleContext context)
        : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.8f, 6.4f);
            m_context.camera.zoom = 25.0f * 0.4f;
        }

        m_isEnabled = true;

        // Define attachment
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-2.0f, 3.0f);
            bodyDef.isEnabled = m_isEnabled;
            m_attachmentId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeBox(0.5f, 2.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_attachmentId, shapeDef, box);
        }

        // Define platform
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(-4.0f, 5.0f);
            m_platformId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeOffsetBox(0.5f, 4.0f, new B2Vec2(4.0f, 0.0f), b2MakeRot(0.5f * B2_PI));

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_platformId, shapeDef, box);

            B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
            B2Vec2 pivot = new B2Vec2(-2.0f, 5.0f);
            revoluteDef.@base.bodyIdA = m_attachmentId;
            revoluteDef.@base.bodyIdB = m_platformId;
            revoluteDef.@base.localFrameA.p = b2Body_GetLocalPoint(m_attachmentId, pivot);
            revoluteDef.@base.localFrameB.p = b2Body_GetLocalPoint(m_platformId, pivot);
            revoluteDef.maxMotorTorque = 50.0f;
            revoluteDef.enableMotor = true;
            b2CreateRevoluteJoint(m_worldId, revoluteDef);
        }
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 11.0f * fontSize;
        float winX = 0.5f * fontSize;
        float winY = m_camera.height - height - 2.0f * fontSize;
        ImGui.SetNextWindowPos(new Vector2(winX, winY), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(9.0f * fontSize, height));
        ImGui.Begin("Disable Crash", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (ImGui.Checkbox("Enable", ref m_isEnabled))
        {
            if (m_isEnabled)
            {
                b2Body_Enable(m_attachmentId);
            }
            else
            {
                b2Body_Disable(m_attachmentId);
            }
        }

        ImGui.End();
    }
}