// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Samples.Primitives;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Samples.Samples.Events;

// This sample shows how to break joints when the internal reaction force becomes large. Instead of polling, this uses events.
public class JointEvent : Sample
{
    private static readonly int SampleJointEvent = SampleFactory.Shared.RegisterSample("Events", "Joint", Create);
    
    public const int e_count = 6;

    private readonly B2JointId[] m_jointIds;

    private static Sample Create(SampleContext context)
    {
        return new JointEvent(context);
    }

    public JointEvent(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 8.0f);
            m_context.camera.zoom = 25.0f * 0.7f;
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
        b2CreateSegmentShape(groundId, shapeDef, segment);

        m_jointIds = new B2JointId[e_count];
        for (int i = 0; i < e_count; ++i)
        {
            m_jointIds[i] = b2_nullJointId;
        }

        B2Vec2 position = new B2Vec2(-12.5f, 10.0f);
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.enableSleep = false;

        B2Polygon box = b2MakeBox(1.0f, 1.0f);

        int index = 0;

        float forceThreshold = 20000.0f;
        float torqueThreshold = 10000.0f;

        // distance joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(bodyId, shapeDef, box);

            float length = 2.0f;
            B2Vec2 pivot1 = new B2Vec2(position.X, position.Y + 1.0f + length);
            B2Vec2 pivot2 = new B2Vec2(position.X, position.Y + 1.0f);
            B2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = bodyId;
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot1);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot2);
            jointDef.length = length;
            jointDef.@base.forceThreshold = forceThreshold;
            jointDef.@base.torqueThreshold = torqueThreshold;
            jointDef.@base.collideConnected = true;
            jointDef.@base.userData = B2UserData.Signed(index);
            m_jointIds[index] = b2CreateDistanceJoint(m_worldId, jointDef);
        }

        position.X += 5.0f;
        ++index;

        // motor joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(bodyId, shapeDef, box);

            B2MotorJointDef jointDef = b2DefaultMotorJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = bodyId;
            jointDef.@base.localFrameA.p = position;
            jointDef.maxVelocityForce = 1000.0f;
            jointDef.maxVelocityTorque = 20.0f;
            jointDef.@base.forceThreshold = forceThreshold;
            jointDef.@base.torqueThreshold = torqueThreshold;
            jointDef.@base.collideConnected = true;
            jointDef.@base.userData = B2UserData.Signed(index);
            m_jointIds[index] = b2CreateMotorJoint(m_worldId, jointDef);
        }

        position.X += 5.0f;
        ++index;

        // prismatic joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(bodyId, shapeDef, box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2PrismaticJointDef jointDef = b2DefaultPrismaticJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = bodyId;
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
            jointDef.@base.forceThreshold = forceThreshold;
            jointDef.@base.torqueThreshold = torqueThreshold;
            jointDef.@base.collideConnected = true;
            jointDef.@base.userData = B2UserData.Signed(index);
            m_jointIds[index] = b2CreatePrismaticJoint(m_worldId, jointDef);
        }

        position.X += 5.0f;
        ++index;

        // revolute joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(bodyId, shapeDef, box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = bodyId;
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
            jointDef.@base.forceThreshold = forceThreshold;
            jointDef.@base.torqueThreshold = torqueThreshold;
            jointDef.@base.collideConnected = true;
            jointDef.@base.userData = B2UserData.Signed(index);
            m_jointIds[index] = b2CreateRevoluteJoint(m_worldId, jointDef);
        }

        position.X += 5.0f;
        ++index;

        // weld joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(bodyId, shapeDef, box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2WeldJointDef jointDef = b2DefaultWeldJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = bodyId;
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
            jointDef.angularHertz = 2.0f;
            jointDef.angularDampingRatio = 0.5f;
            jointDef.@base.forceThreshold = forceThreshold;
            jointDef.@base.torqueThreshold = torqueThreshold;
            jointDef.@base.collideConnected = true;
            jointDef.@base.userData = B2UserData.Signed(index);
            m_jointIds[index] = b2CreateWeldJoint(m_worldId, jointDef);
        }

        position.X += 5.0f;
        ++index;

        // wheel joint
        {
            B2_ASSERT(index < e_count);

            bodyDef.position = position;
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(bodyId, shapeDef, box);

            B2Vec2 pivot = new B2Vec2(position.X - 1.0f, position.Y);
            B2WheelJointDef jointDef = b2DefaultWheelJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = bodyId;
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
            jointDef.@base.forceThreshold = forceThreshold;
            jointDef.@base.torqueThreshold = torqueThreshold;
            jointDef.@base.collideConnected = true;
            jointDef.@base.userData = B2UserData.Signed(index);
            m_jointIds[index] = b2CreateWheelJoint(m_worldId, jointDef);
        }

        position.X += 5.0f;
        ++index;
    }

    public override void Step()
    {
        base.Step();

        // Process joint events
        B2JointEvents events = b2World_GetJointEvents(m_worldId);
        for (int i = 0; i < events.count; ++i)
        {
            // Destroy the joint if it is still valid
            ref readonly B2JointEvent @event = ref events.jointEvents[i];

            if (b2Joint_IsValid(@event.jointId))
            {
                var userData = @event.userData.GetSigned(-1);
                var index = userData;
                B2_ASSERT(0 <= index && index < e_count);
                b2DestroyJoint(@event.jointId, true);
                m_jointIds[index] = b2_nullJointId;
            }
        }
    }
};