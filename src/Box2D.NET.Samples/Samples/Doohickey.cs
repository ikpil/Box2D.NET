// SPDX-FileCopyrightText: 2022 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples;

public class Doohickey
{
    B2BodyId m_wheelId1;
    B2BodyId m_wheelId2;
    B2BodyId m_barId1;
    B2BodyId m_barId2;

    B2JointId m_axleId1;
    B2JointId m_axleId2;
    B2JointId m_sliderId;

    bool m_isSpawned;


    public Doohickey()
    {
    }

    public void Spawn(B2WorldId worldId, B2Vec2 position, float scale)
    {
        Debug.Assert(m_isSpawned == false);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 1.0f * scale);
        B2Capsule capsule = new B2Capsule(new B2Vec2(-3.5f * scale, 0.0f), new B2Vec2(3.5f * scale, 0.0f), 0.15f * scale);

        bodyDef.position = b2MulAdd(position, scale, new B2Vec2(-5.0f, 3.0f));
        m_wheelId1 = b2CreateBody(worldId, ref bodyDef);
        b2CreateCircleShape(m_wheelId1, shapeDef, circle);

        bodyDef.position = b2MulAdd(position, scale, new B2Vec2(5.0f, 3.0f));
        m_wheelId2 = b2CreateBody(worldId, ref bodyDef);
        b2CreateCircleShape(m_wheelId2, shapeDef, circle);

        bodyDef.position = b2MulAdd(position, scale, new B2Vec2(-1.5f, 3.0f));
        m_barId1 = b2CreateBody(worldId, ref bodyDef);
        b2CreateCapsuleShape(m_barId1, shapeDef, capsule);

        bodyDef.position = b2MulAdd(position, scale, new B2Vec2(1.5f, 3.0f));
        m_barId2 = b2CreateBody(worldId, ref bodyDef);
        b2CreateCapsuleShape(m_barId2, shapeDef, capsule);

        B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();

        revoluteDef.bodyIdA = m_wheelId1;
        revoluteDef.bodyIdB = m_barId1;
        revoluteDef.localAnchorA = new B2Vec2(0.0f, 0.0f);
        revoluteDef.localAnchorB = new B2Vec2(-3.5f * scale, 0.0f);
        revoluteDef.enableMotor = true;
        revoluteDef.maxMotorTorque = 2.0f * scale;
        b2CreateRevoluteJoint(worldId, revoluteDef);

        revoluteDef.bodyIdA = m_wheelId2;
        revoluteDef.bodyIdB = m_barId2;
        revoluteDef.localAnchorA = new B2Vec2(0.0f, 0.0f);
        revoluteDef.localAnchorB = new B2Vec2(3.5f * scale, 0.0f);
        revoluteDef.enableMotor = true;
        revoluteDef.maxMotorTorque = 2.0f * scale;
        b2CreateRevoluteJoint(worldId, revoluteDef);

        B2PrismaticJointDef prismaticDef = b2DefaultPrismaticJointDef();
        prismaticDef.bodyIdA = m_barId1;
        prismaticDef.bodyIdB = m_barId2;
        prismaticDef.localAxisA = new B2Vec2(1.0f, 0.0f);
        prismaticDef.localAnchorA = new B2Vec2(2.0f * scale, 0.0f);
        prismaticDef.localAnchorB = new B2Vec2(-2.0f * scale, 0.0f);
        prismaticDef.lowerTranslation = -2.0f * scale;
        prismaticDef.upperTranslation = 2.0f * scale;
        prismaticDef.enableLimit = true;
        prismaticDef.enableMotor = true;
        prismaticDef.maxMotorForce = 2.0f * scale;
        prismaticDef.enableSpring = true;
        prismaticDef.hertz = 1.0f;
        prismaticDef.dampingRatio = 0.5f;
        b2CreatePrismaticJoint(worldId, prismaticDef);
    }

    public void Despawn()
    {
        Debug.Assert(m_isSpawned == true);

        b2DestroyJoint(m_axleId1);
        b2DestroyJoint(m_axleId2);
        b2DestroyJoint(m_sliderId);

        b2DestroyBody(m_wheelId1);
        b2DestroyBody(m_wheelId2);
        b2DestroyBody(m_barId1);
        b2DestroyBody(m_barId2);

        m_isSpawned = false;
    }
}
