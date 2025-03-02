// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples;

public class Donut
{
    public const int e_sides = 7;

    private B2BodyId[] m_bodyIds;
    private B2JointId[] m_jointIds;
    private bool m_isSpawned;

    public Donut()
    {
        m_bodyIds = new B2BodyId[e_sides];
        m_jointIds = new B2JointId[e_sides];

        for (int i = 0; i < e_sides; ++i)
        {
            m_bodyIds[i] = b2_nullBodyId;
            m_jointIds[i] = b2_nullJointId;
        }

        m_isSpawned = false;
    }

    public void Spawn(B2WorldId worldId, B2Vec2 position, float scale, int groupIndex, object userData)
    {
        Debug.Assert(m_isSpawned == false);

        for (int i = 0; i < e_sides; ++i)
        {
            Debug.Assert(B2_IS_NULL(m_bodyIds[i]));
            Debug.Assert(B2_IS_NULL(m_jointIds[i]));
        }

        float radius = 1.0f * scale;
        float deltaAngle = 2.0f * B2_PI / e_sides;
        float length = 2.0f * B2_PI * radius / e_sides;

        B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -0.5f * length), new B2Vec2(0.0f, 0.5f * length), 0.25f * scale);

        B2Vec2 center = position;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.userData = userData;

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.filter.groupIndex = -groupIndex;
        shapeDef.friction = 0.3f;

        // Create bodies
        float angle = 0.0f;
        for (int i = 0; i < e_sides; ++i)
        {
            bodyDef.position = new B2Vec2(radius * MathF.Cos(angle) + center.x, radius * MathF.Sin(angle) + center.y);
            bodyDef.rotation = b2MakeRot(angle);

            m_bodyIds[i] = b2CreateBody(worldId, ref bodyDef);
            b2CreateCapsuleShape(m_bodyIds[i], ref shapeDef, ref capsule);

            angle += deltaAngle;
        }

        // Create joints
        B2WeldJointDef weldDef = b2DefaultWeldJointDef();
        weldDef.angularHertz = 5.0f;
        weldDef.angularDampingRatio = 0.0f;
        weldDef.localAnchorA = new B2Vec2(0.0f, 0.5f * length);
        weldDef.localAnchorB = new B2Vec2(0.0f, -0.5f * length);

        B2BodyId prevBodyId = m_bodyIds[e_sides - 1];
        for (int i = 0; i < e_sides; ++i)
        {
            weldDef.bodyIdA = prevBodyId;
            weldDef.bodyIdB = m_bodyIds[i];
            B2Rot rotA = b2Body_GetRotation(prevBodyId);
            B2Rot rotB = b2Body_GetRotation(m_bodyIds[i]);
            weldDef.referenceAngle = b2RelativeAngle(rotB, rotA);
            m_jointIds[i] = b2CreateWeldJoint(worldId, ref weldDef);
            prevBodyId = weldDef.bodyIdB;
        }

        m_isSpawned = true;
    }

    public void Despawn()
    {
        Debug.Assert(m_isSpawned == true);

        for (int i = 0; i < e_sides; ++i)
        {
            b2DestroyBody(m_bodyIds[i]);
            m_bodyIds[i] = b2_nullBodyId;
            m_jointIds[i] = b2_nullJointId;
        }

        m_isSpawned = false;
    }
}
