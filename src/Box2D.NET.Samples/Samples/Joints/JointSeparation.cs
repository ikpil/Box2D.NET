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
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Ids;

namespace Box2D.NET.Samples.Samples.Joints;

// This sample shows how to measure joint separation. This is the unresolved constraint error.
public class JointSeparation : Sample
{
    private static readonly int SampleJointSeparation = SampleFactory.Shared.RegisterSample("Joints", "Separation", Create);

    private const int e_count = 5;

    private B2BodyId[] m_bodyIds = new B2BodyId[e_count];
    private B2JointId[] m_jointIds = new B2JointId[e_count];
    private float m_impulse;

    private static Sample Create(SampleContext context)
    {
        return new JointSeparation(context);
    }

    public JointSeparation(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 8.0f);
            m_context.camera.m_zoom = 25.0f;
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
        b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

        B2Vec2 position = new B2Vec2(-20.0f, 10.0f);
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.enableSleep = false;

        B2Polygon box = b2MakeBox(1.0f, 1.0f);

        int index = 0;

        // distance joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(m_bodyIds[index], ref shapeDef, ref box);

            float length = 2.0f;
            B2Vec2 pivot1 = new B2Vec2(position.X, position.Y + 1.0f + length);
            B2Vec2 pivot2 = new B2Vec2(position.X, position.Y + 1.0f);
            B2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = m_bodyIds[index];
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot1);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot2);
            jointDef.length = length;
            jointDef.collideConnected = true;
            m_jointIds[index] = b2CreateDistanceJoint(m_worldId, ref jointDef);
        }

        position.X += 10.0f;
        ++index;

        // prismatic joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(m_bodyIds[index], ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2PrismaticJointDef jointDef = b2DefaultPrismaticJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = m_bodyIds[index];
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.localAxisA = b2Body_GetLocalVector(jointDef.bodyIdA, new B2Vec2(1.0f, 0.0f));
            jointDef.collideConnected = true;
            m_jointIds[index] = b2CreatePrismaticJoint(m_worldId, jointDef);
        }

        position.X += 10.0f;
        ++index;

        // revolute joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(m_bodyIds[index], ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = m_bodyIds[index];
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.collideConnected = true;
            m_jointIds[index] = b2CreateRevoluteJoint(m_worldId, ref jointDef);
        }

        position.X += 10.0f;
        ++index;

        // weld joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(m_bodyIds[index], ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2WeldJointDef jointDef = b2DefaultWeldJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = m_bodyIds[index];
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.collideConnected = true;
            m_jointIds[index] = b2CreateWeldJoint(m_worldId, ref jointDef);
        }

        position.X += 10.0f;
        ++index;

        // wheel joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(m_bodyIds[index], ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2WheelJointDef jointDef = b2DefaultWheelJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = m_bodyIds[index];
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.localAxisA = b2Body_GetLocalVector(jointDef.bodyIdA, new B2Vec2(1.0f, 0.0f));
            jointDef.hertz = 1.0f;
            jointDef.dampingRatio = 0.7f;
            jointDef.lowerTranslation = -1.0f;
            jointDef.upperTranslation = 1.0f;
            jointDef.enableLimit = true;
            jointDef.enableMotor = true;
            jointDef.maxMotorTorque = 10.0f;
            jointDef.motorSpeed = 1.0f;
            jointDef.collideConnected = true;
            m_jointIds[index] = b2CreateWheelJoint(m_worldId, ref jointDef);
        }

        m_impulse = 500.0f;
    }

    public override void UpdateGui()
    {
        float height = 120.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(260.0f, height));

        ImGui.Begin("Joint Separation", ImGuiWindowFlags.NoResize);

        B2Vec2 gravity = b2World_GetGravity(m_worldId);
        if (ImGui.SliderFloat("gravity", ref gravity.Y, -500.0f, 500.0f, "%.0f"))
        {
            b2World_SetGravity(m_worldId, gravity);
        }

        if (ImGui.Button("impulse"))
        {
            for (int i = 0; i < e_count; ++i)
            {
                B2Vec2 p = b2Body_GetWorldPoint(m_bodyIds[i], new B2Vec2(1.0f, 1.0f));
                b2Body_ApplyLinearImpulse(m_bodyIds[i], new B2Vec2(m_impulse, -m_impulse), p, true);
            }
        }

        ImGui.SliderFloat("magnitude", ref m_impulse, 0.0f, 1000.0f, "%.0f");

        ImGui.End();
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        for (int i = 0; i < e_count; ++i)
        {
            if (B2_IS_NULL(m_jointIds[i]))
            {
                continue;
            }

            float linear = b2Joint_GetLinearSeparation(m_jointIds[i]);
            float angular = b2Joint_GetAngularSeparation(m_jointIds[i]);
            B2Vec2 point = b2Joint_GetLocalAnchorA(m_jointIds[i]);
            m_draw.DrawString(point, $"{linear:F2} m, {180.0f * angular / B2_PI:F1} deg");
        }
    }
}