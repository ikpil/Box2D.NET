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
using static Box2D.NET.B2Ids;

namespace Box2D.NET.Samples.Samples.Issues;

public class Crash01 : Sample
{
    private static readonly int SampleBodyType = SampleFactory.Shared.RegisterSample("Issues", "Crash01", Create);

    private B2BodyId m_attachmentId;
    private B2BodyId m_platformId;
    private B2BodyType m_type;
    private bool m_isEnabled;

    private static Sample Create(SampleContext context)
    {
        return new Crash01(context);
    }

    public Crash01(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.8f, 6.4f);
            m_context.camera.m_zoom = 25.0f * 0.4f;
        }

        m_type = B2BodyType.b2_dynamicBody;
        m_isEnabled = true;

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.name = "ground";
            groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        // Define attachment
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-2.0f, 3.0f);
            bodyDef.name = "attach1";
            m_attachmentId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(0.5f, 2.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            b2CreatePolygonShape(m_attachmentId, ref shapeDef, ref box);
        }

        // Define platform
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new B2Vec2(-4.0f, 5.0f);
            bodyDef.name = "platform";
            m_platformId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeOffsetBox(0.5f, 4.0f, new B2Vec2(4.0f, 0.0f), b2MakeRot(0.5f * B2_PI));

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 2.0f;
            b2CreatePolygonShape(m_platformId, ref shapeDef, ref box);

            B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
            B2Vec2 pivot = new B2Vec2(-2.0f, 5.0f);
            revoluteDef.@base.bodyIdA = m_attachmentId;
            revoluteDef.@base.bodyIdB = m_platformId;
            revoluteDef.@base.localFrameA.p = b2Body_GetLocalPoint(m_attachmentId, pivot);
            revoluteDef.@base.localFrameB.p = b2Body_GetLocalPoint(m_platformId, pivot);
            revoluteDef.maxMotorTorque = 50.0f;
            revoluteDef.enableMotor = true;
            b2CreateRevoluteJoint(m_worldId, ref revoluteDef);

            B2PrismaticJointDef prismaticDef = b2DefaultPrismaticJointDef();
            B2Vec2 anchor = new B2Vec2(0.0f, 5.0f);
            prismaticDef.@base.bodyIdA = groundId;
            prismaticDef.@base.bodyIdB = m_platformId;
            prismaticDef.@base.localFrameA.p = b2Body_GetLocalPoint(groundId, anchor);
            prismaticDef.@base.localFrameB.p = b2Body_GetLocalPoint(m_platformId, anchor);
            prismaticDef.maxMotorForce = 1000.0f;
            prismaticDef.motorSpeed = 0.0f;
            prismaticDef.enableMotor = true;
            prismaticDef.lowerTranslation = -10.0f;
            prismaticDef.upperTranslation = 10.0f;
            prismaticDef.enableLimit = true;

            b2CreatePrismaticJoint(m_worldId, ref prismaticDef);
        }
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 11.0f * fontSize;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.m_height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(9.0f * fontSize, height));
        ImGui.Begin("Crash 01", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (ImGui.RadioButton("Static", m_type == B2BodyType.b2_staticBody))
        {
            m_type = B2BodyType.b2_staticBody;
            b2Body_SetType(m_platformId, B2BodyType.b2_staticBody);
        }

        if (ImGui.RadioButton("Kinematic", m_type == B2BodyType.b2_kinematicBody))
        {
            m_type = B2BodyType.b2_kinematicBody;
            b2Body_SetType(m_platformId, B2BodyType.b2_kinematicBody);
            b2Body_SetLinearVelocity(m_platformId, new B2Vec2(-0.1f, 0.0f));
        }

        if (ImGui.RadioButton("Dynamic", m_type == B2BodyType.b2_dynamicBody))
        {
            m_type = B2BodyType.b2_dynamicBody;
            b2Body_SetType(m_platformId, B2BodyType.b2_dynamicBody);
        }

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