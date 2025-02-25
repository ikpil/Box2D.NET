// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Bodies;

public class BodyType : Sample
{
    b2BodyId m_attachmentId;
    b2BodyId m_secondAttachmentId;
    b2BodyId m_platformId;
    b2BodyId m_secondPayloadId;
    b2BodyId m_touchingBodyId;
    b2BodyId m_floatingBodyId;
    b2BodyType m_type;
    float m_speed;
    bool m_isEnabled;

    private static int sampleBodyType = RegisterSample("Bodies", "Body Type", BodyType.Create);

    private static Sample Create(Settings settings)
    {
        return new BodyType(settings);
    }

    public BodyType(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.8f, 6.4f);
            Draw.g_camera.m_zoom = 25.0f * 0.4f;
        }

        m_type = b2BodyType.b2_dynamicBody;
        m_isEnabled = true;

        b2BodyId groundId = b2_nullBodyId;
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);

            b2Segment segment = new b2Segment(new b2Vec2(-20.0f, 0.0f), new b2Vec2(20.0f, 0.0f));
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        // Define attachment
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = new b2Vec2(-2.0f, 3.0f);
            m_attachmentId = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeBox(0.5f, 2.0f);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            b2CreatePolygonShape(m_attachmentId, shapeDef, box);
        }

        // Define second attachment
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new b2Vec2(3.0f, 3.0f);
            m_secondAttachmentId = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeBox(0.5f, 2.0f);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            b2CreatePolygonShape(m_secondAttachmentId, shapeDef, box);
        }

        // Define platform
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new b2Vec2(-4.0f, 5.0f);
            m_platformId = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeOffsetBox(0.5f, 4.0f, new b2Vec2(4.0f, 0.0f), b2MakeRot(0.5f * B2_PI));

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.6f;
            shapeDef.density = 2.0f;
            b2CreatePolygonShape(m_platformId, shapeDef, box);

            b2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
            b2Vec2 pivot = new b2Vec2(-2.0f, 5.0f);
            revoluteDef.bodyIdA = m_attachmentId;
            revoluteDef.bodyIdB = m_platformId;
            revoluteDef.localAnchorA = b2Body_GetLocalPoint(m_attachmentId, pivot);
            revoluteDef.localAnchorB = b2Body_GetLocalPoint(m_platformId, pivot);
            revoluteDef.maxMotorTorque = 50.0f;
            revoluteDef.enableMotor = true;
            b2CreateRevoluteJoint(m_worldId, revoluteDef);

            pivot = new b2Vec2(3.0f, 5.0f);
            revoluteDef.bodyIdA = m_secondAttachmentId;
            revoluteDef.bodyIdB = m_platformId;
            revoluteDef.localAnchorA = b2Body_GetLocalPoint(m_secondAttachmentId, pivot);
            revoluteDef.localAnchorB = b2Body_GetLocalPoint(m_platformId, pivot);
            revoluteDef.maxMotorTorque = 50.0f;
            revoluteDef.enableMotor = true;
            b2CreateRevoluteJoint(m_worldId, revoluteDef);

            b2PrismaticJointDef prismaticDef = b2DefaultPrismaticJointDef();
            b2Vec2 anchor = new b2Vec2(0.0f, 5.0f);
            prismaticDef.bodyIdA = groundId;
            prismaticDef.bodyIdB = m_platformId;
            prismaticDef.localAnchorA = b2Body_GetLocalPoint(groundId, anchor);
            prismaticDef.localAnchorB = b2Body_GetLocalPoint(m_platformId, anchor);
            prismaticDef.localAxisA = new b2Vec2(1.0f, 0.0f);
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
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = new b2Vec2(-3.0f, 8.0f);
            b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeBox(0.75f, 0.75f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.6f;
            shapeDef.density = 2.0f;

            b2CreatePolygonShape(bodyId, shapeDef, box);
        }

        // Create a second payload
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new b2Vec2(2.0f, 8.0f);
            m_secondPayloadId = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeBox(0.75f, 0.75f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.6f;
            shapeDef.density = 2.0f;

            b2CreatePolygonShape(m_secondPayloadId, shapeDef, box);
        }

        // Create a separate body on the ground
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new b2Vec2(8.0f, 0.2f);
            m_touchingBodyId = b2CreateBody(m_worldId, bodyDef);

            b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, 0.0f), new b2Vec2(1.0f, 0.0f), 0.25f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.6f;
            shapeDef.density = 2.0f;

            b2CreateCapsuleShape(m_touchingBodyId, shapeDef, capsule);
        }

        // Create a separate floating body
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = m_type;
            bodyDef.isEnabled = m_isEnabled;
            bodyDef.position = new b2Vec2(-8.0f, 12.0f);
            bodyDef.gravityScale = 0.0f;
            m_floatingBodyId = b2CreateBody(m_worldId, bodyDef);

            b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.5f), 0.25f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.6f;
            shapeDef.density = 2.0f;

            b2CreateCircleShape(m_floatingBodyId, shapeDef, circle);
        }
    }

    public override void UpdateUI()
    {
        bool open = false;

        float height = 140.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(180.0f, height));
        ImGui.Begin("Body Type", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (ImGui.RadioButton("Static", m_type == b2BodyType.b2_staticBody))
        {
            m_type = b2BodyType.b2_staticBody;
            b2Body_SetType(m_platformId, b2BodyType.b2_staticBody);
            b2Body_SetType(m_secondAttachmentId, b2BodyType.b2_staticBody);
            b2Body_SetType(m_secondPayloadId, b2BodyType.b2_staticBody);
            b2Body_SetType(m_touchingBodyId, b2BodyType.b2_staticBody);
            b2Body_SetType(m_floatingBodyId, b2BodyType.b2_staticBody);
        }

        if (ImGui.RadioButton("Kinematic", m_type == b2BodyType.b2_kinematicBody))
        {
            m_type = b2BodyType.b2_kinematicBody;
            b2Body_SetType(m_platformId, b2BodyType.b2_kinematicBody);
            b2Body_SetLinearVelocity(m_platformId, new b2Vec2(-m_speed, 0.0f));
            b2Body_SetAngularVelocity(m_platformId, 0.0f);
            b2Body_SetType(m_secondAttachmentId, b2BodyType.b2_kinematicBody);
            b2Body_SetType(m_secondPayloadId, b2BodyType.b2_kinematicBody);
            b2Body_SetType(m_touchingBodyId, b2BodyType.b2_kinematicBody);
            b2Body_SetType(m_floatingBodyId, b2BodyType.b2_kinematicBody);
        }

        if (ImGui.RadioButton("Dynamic", m_type == b2BodyType.b2_dynamicBody))
        {
            m_type = b2BodyType.b2_dynamicBody;
            b2Body_SetType(m_platformId, b2BodyType.b2_dynamicBody);
            b2Body_SetType(m_secondAttachmentId, b2BodyType.b2_dynamicBody);
            b2Body_SetType(m_secondPayloadId, b2BodyType.b2_dynamicBody);
            b2Body_SetType(m_touchingBodyId, b2BodyType.b2_dynamicBody);
            b2Body_SetType(m_floatingBodyId, b2BodyType.b2_dynamicBody);
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

                if (m_type == b2BodyType.b2_kinematicBody)
                {
                    b2Body_SetLinearVelocity(m_platformId, new b2Vec2(-m_speed, 0.0f));
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
        if (m_type == b2BodyType.b2_kinematicBody)
        {
            b2Vec2 p = b2Body_GetPosition(m_platformId);
            b2Vec2 v = b2Body_GetLinearVelocity(m_platformId);

            if ((p.x < -14.0f && v.x < 0.0f) || (p.x > 6.0f && v.x > 0.0f))
            {
                v.x = -v.x;
                b2Body_SetLinearVelocity(m_platformId, v);
            }
        }

        base.Step(settings);
    }
}
