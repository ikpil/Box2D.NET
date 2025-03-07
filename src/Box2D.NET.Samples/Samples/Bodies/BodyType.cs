// SPDX-FileCopyrightText: 2025 Erin Catto
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
    B2BodyId m_attachmentId;
    B2BodyId m_secondAttachmentId;
    B2BodyId m_platformId;
    B2BodyId m_secondPayloadId;
    B2BodyId m_touchingBodyId;
    B2BodyId m_floatingBodyId;
    B2BodyType m_type;
    float m_speed;
    bool m_isEnabled;

    private static readonly int SampleBodyType = SampleFactory.Shared.RegisterSample("Bodies", "Body Type", BodyType.Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BodyType(ctx, settings);
    }

    public BodyType(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.8f, 6.4f);
            B2.g_camera.m_zoom = 25.0f * 0.4f;
        }

        m_type = B2BodyType.b2_dynamicBody;
        m_isEnabled = true;

        B2BodyId groundId = b2_nullBodyId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
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
            m_platformId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeOffsetBox(0.5f, 4.0f, new B2Vec2(4.0f, 0.0f), b2MakeRot(0.5f * B2_PI));

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.6f;
            shapeDef.density = 2.0f;
            b2CreatePolygonShape(m_platformId, ref shapeDef, ref box);

            B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
            B2Vec2 pivot = new B2Vec2(-2.0f, 5.0f);
            revoluteDef.bodyIdA = m_attachmentId;
            revoluteDef.bodyIdB = m_platformId;
            revoluteDef.localAnchorA = b2Body_GetLocalPoint(m_attachmentId, pivot);
            revoluteDef.localAnchorB = b2Body_GetLocalPoint(m_platformId, pivot);
            revoluteDef.maxMotorTorque = 50.0f;
            revoluteDef.enableMotor = true;
            b2CreateRevoluteJoint(m_worldId, ref revoluteDef);

            pivot = new B2Vec2(3.0f, 5.0f);
            revoluteDef.bodyIdA = m_secondAttachmentId;
            revoluteDef.bodyIdB = m_platformId;
            revoluteDef.localAnchorA = b2Body_GetLocalPoint(m_secondAttachmentId, pivot);
            revoluteDef.localAnchorB = b2Body_GetLocalPoint(m_platformId, pivot);
            revoluteDef.maxMotorTorque = 50.0f;
            revoluteDef.enableMotor = true;
            b2CreateRevoluteJoint(m_worldId, ref revoluteDef);

            B2PrismaticJointDef prismaticDef = b2DefaultPrismaticJointDef();
            B2Vec2 anchor = new B2Vec2(0.0f, 5.0f);
            prismaticDef.bodyIdA = groundId;
            prismaticDef.bodyIdB = m_platformId;
            prismaticDef.localAnchorA = b2Body_GetLocalPoint(groundId, anchor);
            prismaticDef.localAnchorB = b2Body_GetLocalPoint(m_platformId, anchor);
            prismaticDef.localAxisA = new B2Vec2(1.0f, 0.0f);
            prismaticDef.maxMotorForce = 1000.0f;
            prismaticDef.motorSpeed = 0.0f;
            prismaticDef.enableMotor = true;
            prismaticDef.lowerTranslation = -10.0f;
            prismaticDef.upperTranslation = 10.0f;
            prismaticDef.enableLimit = true;

            b2CreatePrismaticJoint(m_worldId, prismaticDef);

            m_speed = 3.0f;
        }

        // Create a payload
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-3.0f, 8.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(0.75f, 0.75f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.6f;
            shapeDef.density = 2.0f;

            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        // Create a second payload
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new B2Vec2(2.0f, 8.0f);
            m_secondPayloadId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(0.75f, 0.75f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.6f;
            shapeDef.density = 2.0f;

            b2CreatePolygonShape(m_secondPayloadId, ref shapeDef, ref box);
        }

        // Create a separate body on the ground
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new B2Vec2(8.0f, 0.2f);
            m_touchingBodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, 0.0f), new B2Vec2(1.0f, 0.0f), 0.25f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.6f;
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
            m_floatingBodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.5f), 0.25f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.6f;
            shapeDef.density = 2.0f;

            b2CreateCircleShape(m_floatingBodyId, ref shapeDef, ref circle);
        }
    }

    public override void UpdateUI()
    {
        bool open = true;

        float height = 140.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, B2.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(180.0f, height));
        ImGui.Begin("Body Type", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

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
                b2Body_Enable(m_platformId);
                b2Body_Enable(m_secondAttachmentId);
                b2Body_Enable(m_secondPayloadId);
                b2Body_Enable(m_touchingBodyId);
                b2Body_Enable(m_floatingBodyId);

                if (m_type == B2BodyType.b2_kinematicBody)
                {
                    b2Body_SetLinearVelocity(m_platformId, new B2Vec2(-m_speed, 0.0f));
                    b2Body_SetAngularVelocity(m_platformId, 0.0f);
                }
            }
            else
            {
                b2Body_Disable(m_platformId);
                b2Body_Disable(m_secondAttachmentId);
                b2Body_Disable(m_secondPayloadId);
                b2Body_Disable(m_touchingBodyId);
                b2Body_Disable(m_floatingBodyId);
            }
        }

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        // Drive the kinematic body.
        if (m_type == B2BodyType.b2_kinematicBody)
        {
            B2Vec2 p = b2Body_GetPosition(m_platformId);
            B2Vec2 v = b2Body_GetLinearVelocity(m_platformId);

            if ((p.x < -14.0f && v.x < 0.0f) || (p.x > 6.0f && v.x > 0.0f))
            {
                v.x = -v.x;
                b2Body_SetLinearVelocity(m_platformId, v);
            }
        }

        base.Step(settings);
    }
}
