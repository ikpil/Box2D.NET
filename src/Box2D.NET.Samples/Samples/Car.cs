// SPDX-FileCopyrightText: 2022 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2WheelJoints;
using static Box2D.NET.B2Cores;

namespace Box2D.NET.Samples.Samples;

public struct Car
{
    public B2BodyId m_chassisId;
    public B2BodyId m_rearWheelId;
    public B2BodyId m_frontWheelId;
    public B2JointId m_rearAxleId;
    public B2JointId m_frontAxleId;
    public bool m_isSpawned;

    public void Spawn(B2WorldId worldId, B2Vec2 position, float scale, float hertz, float dampingRatio, float torque, object userData)
    {
        B2_ASSERT(m_isSpawned == false);

        B2_ASSERT(B2_IS_NULL(m_chassisId));
        B2_ASSERT(B2_IS_NULL(m_frontWheelId));
        B2_ASSERT(B2_IS_NULL(m_rearWheelId));

        B2Vec2[] vertices = new B2Vec2[6]
        {
            new B2Vec2(-1.5f, -0.5f), new B2Vec2(1.5f, -0.5f), new B2Vec2(1.5f, 0.0f), new B2Vec2(0.0f, 0.9f), new B2Vec2(-1.15f, 0.9f), new B2Vec2(-1.5f, 0.2f)
        };

        for (int i = 0; i < 6; ++i)
        {
            vertices[i].X *= 0.85f * scale;
            vertices[i].Y *= 0.85f * scale;
        }

        B2Hull hull = b2ComputeHull(vertices, 6);
        B2Polygon chassis = b2MakePolygon(ref hull, 0.15f * scale);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f / scale;
        shapeDef.material.friction = 0.2f;

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.4f * scale);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = b2Add(new B2Vec2(0.0f, 1.0f * scale), position);
        m_chassisId = b2CreateBody(worldId, ref bodyDef);
        b2CreatePolygonShape(m_chassisId, ref shapeDef, ref chassis);

        shapeDef.density = 2.0f / scale;
        shapeDef.material.friction = 1.5f;
        shapeDef.material.rollingResistance = 0.1f;

        bodyDef.position = b2Add(new B2Vec2(-1.0f * scale, 0.35f * scale), position);
        bodyDef.allowFastRotation = true;
        m_rearWheelId = b2CreateBody(worldId, ref bodyDef);
        b2CreateCircleShape(m_rearWheelId, ref shapeDef, ref circle);

        bodyDef.position = b2Add(new B2Vec2(1.0f * scale, 0.4f * scale), position);
        bodyDef.allowFastRotation = true;
        m_frontWheelId = b2CreateBody(worldId, ref bodyDef);
        b2CreateCircleShape(m_frontWheelId, ref shapeDef, ref circle);

        B2Vec2 axis = new B2Vec2(0.0f, 1.0f);
        B2Vec2 pivot = b2Body_GetPosition(m_rearWheelId);

        // float throttle = 0.0f;
        // float speed = 35.0f;
        // float torque = 2.5f * scale;
        // float hertz = 5.0f;
        // float dampingRatio = 0.7f;

        B2WheelJointDef jointDef = b2DefaultWheelJointDef();

        jointDef.bodyIdA = m_chassisId;
        jointDef.bodyIdB = m_rearWheelId;
        jointDef.localAxisA = b2Body_GetLocalVector(jointDef.bodyIdA, axis);
        jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
        jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
        jointDef.motorSpeed = 0.0f;
        jointDef.maxMotorTorque = torque;
        jointDef.enableMotor = true;
        jointDef.hertz = hertz;
        jointDef.dampingRatio = dampingRatio;
        jointDef.lowerTranslation = -0.25f * scale;
        jointDef.upperTranslation = 0.25f * scale;
        jointDef.enableLimit = true;
        m_rearAxleId = b2CreateWheelJoint(worldId, ref jointDef);

        pivot = b2Body_GetPosition(m_frontWheelId);
        jointDef.bodyIdA = m_chassisId;
        jointDef.bodyIdB = m_frontWheelId;
        jointDef.localAxisA = b2Body_GetLocalVector(jointDef.bodyIdA, axis);
        jointDef.localAnchorA = b2Body_GetLocalPoint(jointDef.bodyIdA, pivot);
        jointDef.localAnchorB = b2Body_GetLocalPoint(jointDef.bodyIdB, pivot);
        jointDef.motorSpeed = 0.0f;
        jointDef.maxMotorTorque = torque;
        jointDef.enableMotor = true;
        jointDef.hertz = hertz;
        jointDef.dampingRatio = dampingRatio;
        jointDef.lowerTranslation = -0.25f * scale;
        jointDef.upperTranslation = 0.25f * scale;
        jointDef.enableLimit = true;
        m_frontAxleId = b2CreateWheelJoint(worldId, ref jointDef);
    }

    public void Despawn()
    {
        B2_ASSERT(m_isSpawned == true);

        b2DestroyJoint(m_rearAxleId);
        b2DestroyJoint(m_frontAxleId);
        b2DestroyBody(m_rearWheelId);
        b2DestroyBody(m_frontWheelId);
        b2DestroyBody(m_chassisId);

        m_isSpawned = false;
    }

    public void SetSpeed(float speed)
    {
        b2WheelJoint_SetMotorSpeed(m_rearAxleId, speed);
        b2WheelJoint_SetMotorSpeed(m_frontAxleId, speed);
        b2Joint_WakeBodies(m_rearAxleId);
    }

    public void SetTorque(float torque)
    {
        b2WheelJoint_SetMaxMotorTorque(m_rearAxleId, torque);
        b2WheelJoint_SetMaxMotorTorque(m_frontAxleId, torque);
    }

    public void SetHertz(float hertz)
    {
        b2WheelJoint_SetSpringHertz(m_rearAxleId, hertz);
        b2WheelJoint_SetSpringHertz(m_frontAxleId, hertz);
    }

    public void SetDampingRadio(float dampingRatio)
    {
        b2WheelJoint_SetSpringDampingRatio(m_rearAxleId, dampingRatio);
        b2WheelJoint_SetSpringDampingRatio(m_frontAxleId, dampingRatio);
    }
}
