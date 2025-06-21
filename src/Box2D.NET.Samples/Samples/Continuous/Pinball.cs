﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Silk.NET.GLFW;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2RevoluteJoints;

namespace Box2D.NET.Samples.Samples.Continuous;

// This shows a fast moving body that uses continuous collision versus static and dynamic bodies.
// This is achieved by setting the ball body as a *bullet*.
public class Pinball : Sample
{
    private static readonly int SamplePinball = SampleFactory.Shared.RegisterSample("Continuous", "Pinball", Create);

    private B2JointId m_leftJointId;
    private B2JointId m_rightJointId;
    private B2BodyId m_ballId;

    private static Sample Create(SampleContext context)
    {
        return new Pinball(context);
    }

    public Pinball(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 9.0f);
            m_camera.m_zoom = 25.0f * 0.5f;
        }

        m_context.settings.drawJoints = false;

        // Ground body
        B2BodyId groundId = new B2BodyId();
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Vec2[] vs = new B2Vec2[5]
            {
                new B2Vec2(-8.0f, 6.0f),
                new B2Vec2(-8.0f, 20.0f),
                new B2Vec2(8.0f, 20.0f),
                new B2Vec2(8.0f, 6.0f),
                new B2Vec2(0.0f, -2.0f),
            };

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = vs;
            chainDef.count = 5;
            chainDef.isLoop = true;
            b2CreateChain(groundId, ref chainDef);
        }

        // Flippers
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, 0.0f), p2 = new B2Vec2(2.0f, 0.0f);

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.enableSleep = false;

            bodyDef.position = p1;
            B2BodyId leftFlipperId = b2CreateBody(m_worldId, ref bodyDef);

            bodyDef.position = p2;
            B2BodyId rightFlipperId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(1.75f, 0.2f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();

            b2CreatePolygonShape(leftFlipperId, ref shapeDef, ref box);
            b2CreatePolygonShape(rightFlipperId, ref shapeDef, ref box);

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.localFrameB.p = b2Vec2_zero;
            jointDef.enableMotor = true;
            jointDef.maxMotorTorque = 1000.0f;
            jointDef.enableLimit = true;

            jointDef.motorSpeed = 0.0f;
            jointDef.@base.localFrameA.p = p1;
            jointDef.@base.bodyIdB = leftFlipperId;
            jointDef.lowerAngle = -30.0f * B2_PI / 180.0f;
            jointDef.upperAngle = 5.0f * B2_PI / 180.0f;
            m_leftJointId = b2CreateRevoluteJoint(m_worldId, ref jointDef);

            jointDef.motorSpeed = 0.0f;
            jointDef.@base.localFrameA.p = p2;
            jointDef.@base.bodyIdB = rightFlipperId;
            jointDef.lowerAngle = -5.0f * B2_PI / 180.0f;
            jointDef.upperAngle = 30.0f * B2_PI / 180.0f;
            m_rightJointId = b2CreateRevoluteJoint(m_worldId, ref jointDef);
        }

        // Spinners
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-4.0f, 17.0f);

            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box1 = b2MakeBox(1.5f, 0.125f);
            B2Polygon box2 = b2MakeBox(0.125f, 1.5f);

            b2CreatePolygonShape(bodyId, ref shapeDef, ref box1);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box2);

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = bodyId;
            jointDef.@base.localFrameA.p = bodyDef.position;
            jointDef.@base.localFrameB.p = b2Vec2_zero;
            jointDef.enableMotor = true;
            jointDef.maxMotorTorque = 0.1f;
            b2CreateRevoluteJoint(m_worldId, ref jointDef);

            bodyDef.position = new B2Vec2(4.0f, 8.0f);
            bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box1);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box2);
            jointDef.@base.localFrameA.p = bodyDef.position;
            jointDef.@base.bodyIdB = bodyId;
            b2CreateRevoluteJoint(m_worldId, ref jointDef);
        }

        // Bumpers
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(-4.0f, 8.0f);

            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.restitution = 1.5f;

            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 1.0f);
            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);

            bodyDef.position = new B2Vec2(4.0f, 17.0f);
            bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }

        // Ball
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(1.0f, 15.0f);
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.isBullet = true;

            m_ballId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.2f);
            b2CreateCircleShape(m_ballId, ref shapeDef, ref circle);
        }
    }

    public override void Step()
    {
        base.Step();

        if (GetKey(Keys.Space) == InputAction.Press)
        {
            b2RevoluteJoint_SetMotorSpeed(m_leftJointId, 20.0f);
            b2RevoluteJoint_SetMotorSpeed(m_rightJointId, -20.0f);
        }
        else
        {
            b2RevoluteJoint_SetMotorSpeed(m_leftJointId, -10.0f);
            b2RevoluteJoint_SetMotorSpeed(m_rightJointId, 10.0f);
        }
    }
}