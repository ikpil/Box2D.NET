// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using Box2D.NET.Primitives;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.wheel_joint;

namespace Box2D.NET.Samples;

public class Donut
{
    public const int e_sides = 7;

    private b2BodyId[] m_bodyIds;
    private b2JointId[] m_jointIds;
    private bool m_isSpawned;

    public Donut()
    {
        m_bodyIds = new b2BodyId[e_sides];
        m_jointIds = new b2JointId[e_sides];

        for (int i = 0; i < e_sides; ++i)
        {
            m_bodyIds[i] = b2_nullBodyId;
            m_jointIds[i] = b2_nullJointId;
        }

        m_isSpawned = false;
    }

    public void Spawn(b2WorldId worldId, b2Vec2 position, float scale, int groupIndex, object userData)
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

        b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.5f * length), new b2Vec2(0.0f, 0.5f * length), 0.25f * scale);

        b2Vec2 center = position;

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.userData = userData;

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.filter.groupIndex = -groupIndex;
        shapeDef.friction = 0.3f;

        // Create bodies
        float angle = 0.0f;
        for (int i = 0; i < e_sides; ++i)
        {
            bodyDef.position = new b2Vec2(radius * MathF.Cos(angle) + center.x, radius * MathF.Sin(angle) + center.y);
            bodyDef.rotation = b2MakeRot(angle);

            m_bodyIds[i] = b2CreateBody(worldId, bodyDef);
            b2CreateCapsuleShape(m_bodyIds[i], shapeDef, capsule);

            angle += deltaAngle;
        }

        // Create joints
        b2WeldJointDef weldDef = b2DefaultWeldJointDef();
        weldDef.angularHertz = 5.0f;
        weldDef.angularDampingRatio = 0.0f;
        weldDef.localAnchorA = new b2Vec2(0.0f, 0.5f * length);
        weldDef.localAnchorB = new b2Vec2(0.0f, -0.5f * length);

        b2BodyId prevBodyId = m_bodyIds[e_sides - 1];
        for (int i = 0; i < e_sides; ++i)
        {
            weldDef.bodyIdA = prevBodyId;
            weldDef.bodyIdB = m_bodyIds[i];
            b2Rot rotA = b2Body_GetRotation(prevBodyId);
            b2Rot rotB = b2Body_GetRotation(m_bodyIds[i]);
            weldDef.referenceAngle = b2RelativeAngle(rotB, rotA);
            m_jointIds[i] = b2CreateWeldJoint(worldId, weldDef);
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