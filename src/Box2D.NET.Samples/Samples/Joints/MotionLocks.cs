// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples.Joints;

// This test ensures joints work correctly with bodies that have motion locks
public class MotionLocks : Sample
{
    private static readonly int SampleMotionLocks = SampleFactory.Shared.RegisterSample("Joints", "Motion Locks", Create);

    public const int e_count = 6;

    private B2BodyId[] m_bodyIds = new B2BodyId[e_count];
    private B2MotionLocks m_motionLocks;

    private static Sample Create(SampleContext context)
    {
        return new MotionLocks(context);
    }

    public MotionLocks(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 8.0f);
            m_camera.zoom = 25.0f * 0.7f;
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        m_motionLocks = new B2MotionLocks(false, false, true);

        for (int i = 0; i < e_count; ++i)
        {
            m_bodyIds[i] = b2_nullBodyId;
        }

        B2Vec2 position = new B2Vec2(-12.5f, 10.0f);
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.motionLocks = m_motionLocks;

        B2Polygon box = b2MakeBox(1.0f, 1.0f);

        int index = 0;

        // distance joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], ref shapeDef, ref box);

            float length = 2.0f;
            B2Vec2 pivot1 = new B2Vec2(position.X, position.Y + 1.0f + length);
            B2Vec2 pivot2 = new B2Vec2(position.X, position.Y + 1.0f);
            B2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = m_bodyIds[index];
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot1);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot2);
            jointDef.length = length;
            b2CreateDistanceJoint(m_worldId, ref jointDef);
        }

        position.X += 5.0f;
        ++index;

        // motor joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], ref shapeDef, ref box);

            B2MotorJointDef jointDef = b2DefaultMotorJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = m_bodyIds[index];
            jointDef.@base.localFrameA.p = position;
            jointDef.maxVelocityForce = 200.0f;
            jointDef.maxVelocityTorque = 200.0f;
            b2CreateMotorJoint(m_worldId, ref jointDef);
        }

        position.X += 5.0f;
        ++index;

        // prismatic joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2PrismaticJointDef jointDef = b2DefaultPrismaticJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = m_bodyIds[index];
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
            b2CreatePrismaticJoint(m_worldId, ref jointDef);
        }

        position.X += 5.0f;
        ++index;

        // revolute joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = m_bodyIds[index];
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
            b2CreateRevoluteJoint(m_worldId, ref jointDef);
        }

        position.X += 5.0f;
        ++index;

        // weld joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2WeldJointDef jointDef = b2DefaultWeldJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = m_bodyIds[index];
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
            jointDef.angularHertz = 1.0f;
            jointDef.angularDampingRatio = 0.5f;
            jointDef.linearHertz = 1.0f;
            jointDef.linearDampingRatio = 0.5f;
            b2CreateWeldJoint(m_worldId, ref jointDef);
        }

        position.X += 5.0f;
        ++index;

        // wheel joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            m_bodyIds[index] = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(m_bodyIds[index], ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2WheelJointDef jointDef = b2DefaultWheelJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = m_bodyIds[index];
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
            jointDef.hertz = 1.0f;
            jointDef.dampingRatio = 0.7f;
            jointDef.lowerTranslation = -1.0f;
            jointDef.upperTranslation = 1.0f;
            jointDef.enableLimit = true;
            jointDef.enableMotor = true;
            jointDef.maxMotorTorque = 10.0f;
            jointDef.motorSpeed = 1.0f;
            b2CreateWheelJoint(m_worldId, ref jointDef);
        }

        position.X += 5.0f;
        ++index;
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 8.0f * fontSize;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(14.0f * fontSize, height));

        ImGui.Begin("Motion Locks", ImGuiWindowFlags.NoResize);

        if (ImGui.Checkbox("Lock Linear X", ref m_motionLocks.linearX))
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2Body_SetMotionLocks(m_bodyIds[i], m_motionLocks);
                b2Body_SetAwake(m_bodyIds[i], true);
            }
        }

        if (ImGui.Checkbox("Lock Linear Y", ref m_motionLocks.linearY))
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2Body_SetMotionLocks(m_bodyIds[i], m_motionLocks);
                b2Body_SetAwake(m_bodyIds[i], true);
            }
        }

        if (ImGui.Checkbox("Lock Angular Z", ref m_motionLocks.angularZ))
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2Body_SetMotionLocks(m_bodyIds[i], m_motionLocks);
                b2Body_SetAwake(m_bodyIds[i], true);
            }
        }

        ImGui.End();
    }

    public override void Step()
    {
        base.Step();

        if (GetKey(Keys.L) == InputAction.Press)
        {
            b2Body_ApplyLinearImpulseToCenter(m_bodyIds[0], new B2Vec2(
                100.0f, 0.0f
            ), true);
        }
    }
}