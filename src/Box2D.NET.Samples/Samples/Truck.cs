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
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples;

public class Truck
{
    private B2BodyId m_chassisId;
    private B2BodyId m_rearWheelId;
    private B2BodyId m_frontWheelId;
    private B2JointId m_rearAxleId;
    private B2JointId m_frontAxleId;
    private bool m_isSpawned;

    public Truck()
    {
    }

    public void Spawn(B2WorldId worldId, B2Vec2 position, float scale, float hertz, float dampingRatio, float torque, float density, object userData)
    {
        B2_ASSERT(m_isSpawned == false);

        B2_ASSERT(B2_IS_NULL(m_chassisId));
        B2_ASSERT(B2_IS_NULL(m_frontWheelId));
        B2_ASSERT(B2_IS_NULL(m_rearWheelId));

        // B2Vec2 vertices[6] = {
        //	{ -1.5f, -0.5f }, { 1.5f, -0.5f }, { 1.5f, 0.0f }, { 0.0f, 0.9f }, { -1.15f, 0.9f }, { -1.5f, 0.2f },
        // };

        B2Vec2[] vertices = new B2Vec2[5]
        {
            new B2Vec2(-0.65f, -0.4f),
            new B2Vec2(1.5f, -0.4f),
            new B2Vec2(1.5f, 0.0f),
            new B2Vec2(0.0f, 0.9f),
            new B2Vec2(-0.65f, 0.9f),
        };

        for (int i = 0; i < 5; ++i)
        {
            vertices[i].X *= 0.85f * scale;
            vertices[i].Y *= 0.85f * scale;
        }

        B2Hull hull = b2ComputeHull(vertices, 5);
        B2Polygon chassis = b2MakePolygon(ref hull, 0.15f * scale);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = density;
        shapeDef.material.friction = 0.2f;
        shapeDef.material.customColor = (uint)B2HexColor.b2_colorHotPink;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = b2Add(new B2Vec2(0.0f, 1.0f * scale), position);
        m_chassisId = b2CreateBody(worldId, bodyDef);
        b2CreatePolygonShape(m_chassisId, ref shapeDef, ref chassis);

        B2Polygon box = b2MakeOffsetBox(1.25f * scale, 0.1f * scale, new B2Vec2(-2.05f * scale, -0.275f * scale), b2Rot_identity);
        box.radius = 0.1f * scale;
        b2CreatePolygonShape(m_chassisId, ref shapeDef, ref box);

        box = b2MakeOffsetBox(0.05f * scale, 0.35f * scale, new B2Vec2(-3.25f * scale, 0.375f * scale), b2Rot_identity);
        box.radius = 0.1f * scale;
        b2CreatePolygonShape(m_chassisId, ref shapeDef, ref box);

        shapeDef.density = 2.0f * density;
        shapeDef.material.friction = 2.5f;
        shapeDef.material.customColor = (uint)B2HexColor.b2_colorSilver;

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.4f * scale);
        bodyDef.position = b2Add(new B2Vec2(-2.75f * scale, 0.3f * scale), position);
        m_rearWheelId = b2CreateBody(worldId, bodyDef);
        b2CreateCircleShape(m_rearWheelId, ref shapeDef, ref circle);

        bodyDef.position = b2Add(new B2Vec2(0.8f * scale, 0.3f * scale), position);
        m_frontWheelId = b2CreateBody(worldId, bodyDef);
        b2CreateCircleShape(m_frontWheelId, ref shapeDef, ref circle);

        B2Vec2 axis = new B2Vec2(0.0f, 1.0f);
        B2Vec2 pivot = b2Body_GetPosition(m_rearWheelId);

        // float throttle = 0.0f;
        // float speed = 35.0f;
        // float torque = 2.5f * scale;
        // float hertz = 5.0f;
        // float dampingRatio = 0.7f;

        B2WheelJointDef jointDef = b2DefaultWheelJointDef();

        jointDef.@base.bodyIdA = m_chassisId;
        jointDef.@base.bodyIdB = m_rearWheelId;
        jointDef.@base.localFrameA.q = b2MakeRot(0.5f * B2_PI);
        jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
        jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
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
        jointDef.@base.bodyIdA = m_chassisId;
        jointDef.@base.bodyIdB = m_frontWheelId;
        jointDef.@base.localFrameA.q = b2MakeRot(0.5f * B2_PI);
        jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
        jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
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

        b2DestroyJoint(m_rearAxleId, false);
        b2DestroyJoint(m_frontAxleId, false);
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