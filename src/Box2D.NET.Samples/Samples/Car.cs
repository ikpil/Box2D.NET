// SPDX-FileCopyrightText: 2022 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

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


namespace Box2D.NET.Samples.Samples;

public class Car
{
    public b2BodyId m_chassisId;
    public b2BodyId m_rearWheelId;
    public b2BodyId m_frontWheelId;
    public b2JointId m_rearAxleId;
    public b2JointId m_frontAxleId;
    public bool m_isSpawned;

    public Car()
    {
    }

    public void Spawn(b2WorldId worldId, b2Vec2 position, float scale, float hertz, float dampingRatio, float torque, object userData)
    {
        Debug.Assert(m_isSpawned == false);

        Debug.Assert(B2_IS_NULL(m_chassisId));
        Debug.Assert(B2_IS_NULL(m_frontWheelId));
        Debug.Assert(B2_IS_NULL(m_rearWheelId));

        b2Vec2[] vertices = new b2Vec2[6]
        {
            new b2Vec2(-1.5f, -0.5f), new b2Vec2(1.5f, -0.5f), new b2Vec2(1.5f, 0.0f), new b2Vec2(0.0f, 0.9f), new b2Vec2(-1.15f, 0.9f), new b2Vec2(-1.5f, 0.2f)
        };

        for (int i = 0; i < 6; ++i)
        {
            vertices[i].x *= 0.85f * scale;
            vertices[i].y *= 0.85f * scale;
        }

        b2Hull hull = b2ComputeHull(vertices, 6);
        b2Polygon chassis = b2MakePolygon(hull, 0.15f * scale);

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f / scale;
        shapeDef.friction = 0.2f;

        b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.4f * scale);

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = b2Add(new b2Vec2(0.0f, 1.0f * scale), position);
        m_chassisId = b2CreateBody(worldId, bodyDef);
        b2CreatePolygonShape(m_chassisId, shapeDef, chassis);

        shapeDef.density = 2.0f / scale;
        shapeDef.friction = 1.5f;
        shapeDef.rollingResistance = 0.1f;

        bodyDef.position = b2Add(new b2Vec2(-1.0f * scale, 0.35f * scale), position);
        bodyDef.allowFastRotation = true;
        m_rearWheelId = b2CreateBody(worldId, bodyDef);
        b2CreateCircleShape(m_rearWheelId, shapeDef, circle);

        bodyDef.position = b2Add(new b2Vec2(1.0f * scale, 0.4f * scale), position);
        bodyDef.allowFastRotation = true;
        m_frontWheelId = b2CreateBody(worldId, bodyDef);
        b2CreateCircleShape(m_frontWheelId, shapeDef, circle);

        b2Vec2 axis = new b2Vec2(0.0f, 1.0f);
        b2Vec2 pivot = b2Body_GetPosition(m_rearWheelId);

        // float throttle = 0.0f;
        // float speed = 35.0f;
        // float torque = 2.5f * scale;
        // float hertz = 5.0f;
        // float dampingRatio = 0.7f;

        b2WheelJointDef jointDef = b2DefaultWheelJointDef();

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
        m_rearAxleId = b2CreateWheelJoint(worldId, jointDef);

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
        m_frontAxleId = b2CreateWheelJoint(worldId, jointDef);
    }

    public void Despawn()
    {
        Debug.Assert(m_isSpawned == true);

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
