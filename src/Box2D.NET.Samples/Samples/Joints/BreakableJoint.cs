// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Joints;

// This sample shows how to break joints when the internal reaction force becomes large.
public class BreakableJoint : Sample
{
    private static readonly int SampleBreakableJoint = SampleFactory.Shared.RegisterSample("Joints", "Breakable", Create);

    public const int e_count = 6;

    private B2JointId[] m_jointIds = new B2JointId[e_count];
    private float m_breakForce;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BreakableJoint(ctx, settings);
    }

    public BreakableJoint(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 8.0f);
            m_context.camera.m_zoom = 25.0f * 0.7f;
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
        b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

        for (int i = 0; i < e_count; ++i)
        {
            m_jointIds[i] = b2_nullJointId;
        }

        B2Vec2 position = new B2Vec2(-12.5f, 10.0f);
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.enableSleep = false;

        B2Polygon box = b2MakeBox(1.0f, 1.0f);

        int index = 0;

        // distance joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            float length = 2.0f;
            B2Vec2 pivot1 = new B2Vec2(position.X, position.Y + 1.0f + length);
            B2Vec2 pivot2 = new B2Vec2(position.X, position.Y + 1.0f);
            B2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = bodyId;
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot1);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot2);
            jointDef.length = length;
            jointDef.collideConnected = true;
            m_jointIds[index] = b2CreateDistanceJoint(m_worldId, ref jointDef);
        }

        position.X += 5.0f;
        ++index;

        // motor joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            B2MotorJointDef jointDef = b2DefaultMotorJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = bodyId;
            jointDef.linearOffset = position;
            jointDef.maxForce = 1000.0f;
            jointDef.maxTorque = 20.0f;
            jointDef.collideConnected = true;
            m_jointIds[index] = b2CreateMotorJoint(m_worldId, ref jointDef);
        }

        position.X += 5.0f;
        ++index;

        // prismatic joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2PrismaticJointDef jointDef = b2DefaultPrismaticJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = bodyId;
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.localAxisA = b2Body_GetLocalVector(jointDef.bodyIdA, new B2Vec2(1.0f, 0.0f));
            jointDef.collideConnected = true;
            m_jointIds[index] = b2CreatePrismaticJoint(m_worldId, jointDef);
        }

        position.X += 5.0f;
        ++index;

        // revolute joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = bodyId;
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.collideConnected = true;
            m_jointIds[index] = b2CreateRevoluteJoint(m_worldId, ref jointDef);
        }

        position.X += 5.0f;
        ++index;

        // weld joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2WeldJointDef jointDef = b2DefaultWeldJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = bodyId;
            jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
            jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
            jointDef.angularHertz = 2.0f;
            jointDef.angularDampingRatio = 0.5f;
            jointDef.linearHertz = 2.0f;
            jointDef.linearDampingRatio = 0.5f;
            jointDef.collideConnected = true;
            m_jointIds[index] = b2CreateWeldJoint(m_worldId, ref jointDef);
        }

        position.X += 5.0f;
        ++index;

        // wheel joint
        {
            Debug.Assert(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2WheelJointDef jointDef = b2DefaultWheelJointDef();
            jointDef.bodyIdA = groundId;
            jointDef.bodyIdB = bodyId;
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

        position.X += 5.0f;
        ++index;

        m_breakForce = 1000.0f;
    }

    public override void UpdateUI()
    {
        base.UpdateUI();

        float height = 100.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Breakable Joint", ImGuiWindowFlags.NoResize);

        ImGui.SliderFloat("break force", ref m_breakForce, 0.0f, 10000.0f, "%.1f");

        B2Vec2 gravity = b2World_GetGravity(m_worldId);
        if (ImGui.SliderFloat("gravity", ref gravity.Y, -50.0f, 50.0f, "%.1f"))
        {
            b2World_SetGravity(m_worldId, gravity);
        }

        ImGui.End();
    }


    public override void Step(Settings settings)
    {
        for (int i = 0; i < e_count; ++i)
        {
            if (B2_IS_NULL(m_jointIds[i]))
            {
                continue;
            }

            B2Vec2 force = b2Joint_GetConstraintForce(m_jointIds[i]);
            if (b2LengthSquared(force) > m_breakForce * m_breakForce)
            {
                b2DestroyJoint(m_jointIds[i]);
                m_jointIds[i] = b2_nullJointId;
            }
        }

        base.Step(settings);
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

            B2Vec2 force = b2Joint_GetConstraintForce(m_jointIds[i]);
            if (b2LengthSquared(force) <= m_breakForce * m_breakForce)
            {
                B2Vec2 point = b2Joint_GetLocalAnchorA(m_jointIds[i]);
                m_context.draw.DrawString(point, $"({force.X:F1}, {force.Y:F1})");
            }
        }
    }
}