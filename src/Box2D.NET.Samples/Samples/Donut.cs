// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples;

public struct Donut
{
    public const int m_sides = 7;

    private B2FixedArray7<B2BodyId> m_bodyIds;
    private B2FixedArray7<B2JointId> m_jointIds;
    private bool m_isSpawned;

    public Donut()
    {
        B2_ASSERT(m_sides == B2FixedArray7<B2BodyId>.Size);
        B2_ASSERT(m_sides == B2FixedArray7<B2JointId>.Size);
    }

    public void Create(B2WorldId worldId, B2Vec2 position, float scale, int groupIndex, bool enableSensorEvents, object userData)
    {
        B2_ASSERT(m_isSpawned == false);

        for (int i = 0; i < m_sides; ++i)
        {
            B2_ASSERT(B2_IS_NULL(m_bodyIds[i]));
            B2_ASSERT(B2_IS_NULL(m_jointIds[i]));
        }

        float radius = 1.0f * scale;
        float deltaAngle = 2.0f * B2_PI / m_sides;
        float length = 2.0f * B2_PI * radius / m_sides;

        B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.5f * length), new B2Vec2(0.0f, 0.5f * length), 0.25f * scale);

        B2Vec2 center = position;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.userData = userData;

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.enableSensorEvents = enableSensorEvents;
        shapeDef.filter.groupIndex = -groupIndex;
        shapeDef.material.friction = 0.3f;

        // Create bodies
        float angle = 0.0f;
        for (int i = 0; i < m_sides; ++i)
        {
            bodyDef.position = new B2Vec2(radius * MathF.Cos(angle) + center.X, radius * MathF.Sin(angle) + center.Y);
            bodyDef.rotation = b2MakeRot(angle);

            m_bodyIds[i] = b2CreateBody(worldId, ref bodyDef);
            b2CreateCapsuleShape(m_bodyIds[i], ref shapeDef, ref capsule);

            angle += deltaAngle;
        }

        // Create joints
        B2WeldJointDef weldDef = b2DefaultWeldJointDef();
        weldDef.angularHertz = 5.0f;
        weldDef.angularDampingRatio = 0.0f;
        weldDef.@base.localFrameA.p = new B2Vec2(0.0f, 0.5f * length);
        weldDef.@base.localFrameB.p = new B2Vec2(0.0f, -0.5f * length);
        weldDef.@base.drawSize = 0.5f * scale;

        B2BodyId prevBodyId = m_bodyIds[m_sides - 1];
        for (int i = 0; i < m_sides; ++i)
        {
            weldDef.@base.bodyIdA = prevBodyId;
            weldDef.@base.bodyIdB = m_bodyIds[i];
            B2Rot qA = b2Body_GetRotation(prevBodyId);
            B2Rot qB = b2Body_GetRotation(m_bodyIds[i]);
            weldDef.@base.localFrameA.q = b2InvMulRot(qA, qB);
            m_jointIds[i] = b2CreateWeldJoint(worldId, ref weldDef);
            prevBodyId = weldDef.@base.bodyIdB;
        }

        m_isSpawned = true;
    }

    public void Destroy()
    {
        B2_ASSERT(m_isSpawned == true);

        for (int i = 0; i < m_sides; ++i)
        {
            b2DestroyBody(m_bodyIds[i]);
            m_bodyIds[i] = b2_nullBodyId;
            m_jointIds[i] = b2_nullJointId;
        }

        m_isSpawned = false;
    }
}