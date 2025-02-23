// SPDX-FileCopyrightText: 2022 Erin Catto
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

namespace Box2D.NET.Samples;

public class Truck
{
    b2BodyId m_chassisId;
    b2BodyId m_rearWheelId;
    b2BodyId m_frontWheelId;
    b2JointId m_rearAxleId;
    b2JointId m_frontAxleId;
    bool m_isSpawned;

    public Truck()
    {
    }

    public void Spawn(b2WorldId worldId, b2Vec2 position, float scale, float hertz, float dampingRatio, float torque, float density, object userData)
    {
        Debug.Assert(m_isSpawned == false);

        Debug.Assert(B2_IS_NULL(m_chassisId));
        Debug.Assert(B2_IS_NULL(m_frontWheelId));
        Debug.Assert(B2_IS_NULL(m_rearWheelId));

        // b2Vec2 vertices[6] = {
        //	{ -1.5f, -0.5f }, { 1.5f, -0.5f }, { 1.5f, 0.0f }, { 0.0f, 0.9f }, { -1.15f, 0.9f }, { -1.5f, 0.2f },
        // };

        b2Vec2[] vertices = new b2Vec2[5]
        {
            new b2Vec2(-0.65f, -0.4f),
            new b2Vec2(1.5f, -0.4f),
            new b2Vec2(1.5f, 0.0f),
            new b2Vec2(0.0f, 0.9f),
            new b2Vec2(-0.65f, 0.9f),
        };

        for (int i = 0; i < 5; ++i)
        {
            vertices[i].x *= 0.85f * scale;
            vertices[i].y *= 0.85f * scale;
        }

        b2Hull hull = b2ComputeHull(vertices, 5);
        b2Polygon chassis = b2MakePolygon(hull, 0.15f * scale);

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = density;
        shapeDef.friction = 0.2f;
        shapeDef.customColor = (uint)b2HexColor.b2_colorHotPink;

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = b2Add(new b2Vec2(0.0f, 1.0f * scale), position);
        m_chassisId = b2CreateBody(worldId, bodyDef);
        b2CreatePolygonShape(m_chassisId, shapeDef, chassis);

        b2Polygon box = b2MakeOffsetBox(1.25f * scale, 0.1f * scale, new b2Vec2(-2.05f * scale, -0.275f * scale), b2Rot_identity);
        box.radius = 0.1f * scale;
        b2CreatePolygonShape(m_chassisId, shapeDef, box);

        box = b2MakeOffsetBox(0.05f * scale, 0.35f * scale, new b2Vec2(-3.25f * scale, 0.375f * scale), b2Rot_identity);
        box.radius = 0.1f * scale;
        b2CreatePolygonShape(m_chassisId, shapeDef, box);

        shapeDef.density = 2.0f * density;
        shapeDef.friction = 2.5f;
        shapeDef.customColor = (uint)b2HexColor.b2_colorSilver;

        b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.4f * scale);
        bodyDef.position = b2Add(new b2Vec2(-2.75f * scale, 0.3f * scale), position);
        m_rearWheelId = b2CreateBody(worldId, bodyDef);
        b2CreateCircleShape(m_rearWheelId, shapeDef, circle);

        bodyDef.position = b2Add(new b2Vec2(0.8f * scale, 0.3f * scale), position);
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