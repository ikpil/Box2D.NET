﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Bodies;

public class BodyType : Sample
{
    private static readonly int SampleBodyType = SampleFactory.Shared.RegisterSample("Bodies", "Body Type", BodyType.Create);

    private B2BodyId m_attachmentId;
    private B2BodyId m_secondAttachmentId;
    private B2BodyId m_platformId;
    private B2BodyId m_secondPayloadId;
    private B2BodyId m_touchingBodyId;
    private B2BodyId m_floatingBodyId;
    private B2BodyType m_type;
    private float m_speed;
    private bool m_isEnabled;

    private static Sample Create(SampleContext context)
    {
        return new BodyType(context);
    }

    public BodyType(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.8f, 6.4f);
            m_camera.m_zoom = 25.0f * 0.4f;
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

        // Define second attachment
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new B2Vec2(3.0f, 3.0f);
            bodyDef.name = "attach2";
            m_secondAttachmentId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(0.5f, 2.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            b2CreatePolygonShape(m_secondAttachmentId, ref shapeDef, ref box);
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

            pivot = new B2Vec2(3.0f, 5.0f);
            revoluteDef.@base.bodyIdA = m_secondAttachmentId;
            revoluteDef.@base.bodyIdB = m_platformId;
            revoluteDef.@base.localFrameA.p = b2Body_GetLocalPoint(m_secondAttachmentId, pivot);
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

            m_speed = 3.0f;
        }

        // Create a payload
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-3.0f, 8.0f);
            bodyDef.name = "crate1";
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(0.75f, 0.75f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 2.0f;

            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        // Create a second payload
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new B2Vec2(2.0f, 8.0f);
            bodyDef.name = "crate2";
            m_secondPayloadId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(0.75f, 0.75f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 2.0f;

            b2CreatePolygonShape(m_secondPayloadId, ref shapeDef, ref box);
        }

        // Create a separate body on the ground
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new B2Vec2(8.0f, 0.2f);
            bodyDef.name = "debris";
            m_touchingBodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, 0.0f), new B2Vec2(1.0f, 0.0f), 0.25f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 2.0f;

            b2CreateCapsuleShape(m_touchingBodyId, ref shapeDef, ref capsule);
        }

        // Create a separate floating body
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new B2Vec2(-8.0f, 12.0f);
            bodyDef.gravityScale = 0.0f;
            bodyDef.name = "floater";
            m_floatingBodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.5f), 0.25f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 2.0f;

            b2CreateCircleShape(m_floatingBodyId, ref shapeDef, ref circle);
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 11.0f * fontSize;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.m_height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(9.0f * fontSize, height));
        ImGui.Begin("Body Type", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (ImGui.RadioButton("Static", m_type == B2BodyType.b2_staticBody))
        {
            m_type = B2BodyType.b2_staticBody;
            b2Body_SetType(m_platformId, B2BodyType.b2_staticBody);
            b2Body_SetType(m_secondAttachmentId, B2BodyType.b2_staticBody);
            b2Body_SetType(m_secondPayloadId, B2BodyType.b2_staticBody);
            b2Body_SetType(m_touchingBodyId, B2BodyType.b2_staticBody);
            b2Body_SetType(m_floatingBodyId, B2BodyType.b2_staticBody);
        }

        if (ImGui.RadioButton("Kinematic", m_type == B2BodyType.b2_kinematicBody))
        {
            m_type = B2BodyType.b2_kinematicBody;
            b2Body_SetType(m_platformId, B2BodyType.b2_kinematicBody);
            b2Body_SetLinearVelocity(m_secondAttachmentId, b2Vec2_zero);
            b2Body_SetAngularVelocity(m_secondAttachmentId, 0.0f);

            b2Body_SetLinearVelocity(m_platformId, new B2Vec2(-m_speed, 0.0f));
            b2Body_SetAngularVelocity(m_platformId, 0.0f);
            b2Body_SetType(m_secondAttachmentId, B2BodyType.b2_kinematicBody);
            b2Body_SetType(m_secondPayloadId, B2BodyType.b2_kinematicBody);
            b2Body_SetType(m_touchingBodyId, B2BodyType.b2_kinematicBody);
            b2Body_SetType(m_floatingBodyId, B2BodyType.b2_kinematicBody);
        }

        if (ImGui.RadioButton("Dynamic", m_type == B2BodyType.b2_dynamicBody))
        {
            m_type = B2BodyType.b2_dynamicBody;
            b2Body_SetType(m_platformId, B2BodyType.b2_dynamicBody);
            b2Body_SetType(m_secondAttachmentId, B2BodyType.b2_dynamicBody);
            b2Body_SetType(m_secondPayloadId, B2BodyType.b2_dynamicBody);
            b2Body_SetType(m_touchingBodyId, B2BodyType.b2_dynamicBody);
            b2Body_SetType(m_floatingBodyId, B2BodyType.b2_dynamicBody);
        }

        if (ImGui.Checkbox("Enable", ref m_isEnabled))
        {
            if (m_isEnabled)
            {
                b2Body_Enable(m_attachmentId);
                b2Body_Enable(m_secondPayloadId);
                b2Body_Enable(m_floatingBodyId);
            }
            else
            {
                b2Body_Disable(m_attachmentId);
                b2Body_Disable(m_secondPayloadId);
                b2Body_Disable(m_floatingBodyId);
            }
        }

        ImGui.End();
    }

    public override void Step()
    {
        // Drive the kinematic body.
        if (m_type == B2BodyType.b2_kinematicBody)
        {
            B2Vec2 p = b2Body_GetPosition(m_platformId);
            B2Vec2 v = b2Body_GetLinearVelocity(m_platformId);

            if ((p.X < -14.0f && v.X < 0.0f) || (p.X > 6.0f && v.X > 0.0f))
            {
                v.X = -v.X;
                b2Body_SetLinearVelocity(m_platformId, v);
            }
        }

        base.Step();
    }
}