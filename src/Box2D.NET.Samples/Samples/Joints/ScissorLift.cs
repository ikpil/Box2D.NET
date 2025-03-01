// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2DistanceJoints;

namespace Box2D.NET.Samples.Samples.Joints;

public class ScissorLift : Sample
{
    B2JointId m_liftJointId;
    float m_motorForce;
    float m_motorSpeed;
    bool m_enableMotor;

    private static readonly int SampleScissorLift = SampleFactory.Shared.RegisterSample("Joints", "Scissor Lift", Create);

    private static Sample Create(Settings settings)
    {
        return new ScissorLift(settings);
    }


    public ScissorLift(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.0f, 9.0f);
            B2.g_camera.m_zoom = 25.0f * 0.4f;
        }

        // Need 8 sub-steps for smoother operation
        settings.subStepCount = 8;

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, segment);
        }


        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.sleepThreshold = 0.01f;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Capsule capsule = new B2Capsule(new B2Vec2(-2.5f, 0.0f), new B2Vec2(2.5f, 0.0f), 0.15f);

            B2BodyId baseId1 = groundId;
            B2BodyId baseId2 = groundId;
            B2Vec2 baseAnchor1 = new B2Vec2(-2.5f, 0.2f);
            B2Vec2 baseAnchor2 = new B2Vec2(2.5f, 0.2f);
            float y = 0.5f;

            B2BodyId linkId1 = new B2BodyId();
            int N = 3;

            for (int i = 0; i < N; ++i)
            {
                bodyDef.position = new B2Vec2(0.0f, y);
                bodyDef.rotation = b2MakeRot(0.15f);
                B2BodyId bodyId1 = b2CreateBody(m_worldId, ref bodyDef);
                b2CreateCapsuleShape(bodyId1, ref shapeDef, capsule);

                bodyDef.position = new B2Vec2(0.0f, y);
                bodyDef.rotation = b2MakeRot(-0.15f);

                B2BodyId bodyId2 = b2CreateBody(m_worldId, ref bodyDef);
                b2CreateCapsuleShape(bodyId2, ref shapeDef, capsule);

                if (i == 1)
                {
                    linkId1 = bodyId2;
                }

                B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();

                // left pin
                revoluteDef.bodyIdA = baseId1;
                revoluteDef.bodyIdB = bodyId1;
                revoluteDef.localAnchorA = baseAnchor1;
                revoluteDef.localAnchorB = new B2Vec2(-2.5f, 0.0f);
                revoluteDef.enableMotor = false;
                revoluteDef.maxMotorTorque = 1.0f;
                revoluteDef.collideConnected = (i == 0) ? true : false;

                b2CreateRevoluteJoint(m_worldId, ref revoluteDef);

                // right pin
                if (i == 0)
                {
                    B2WheelJointDef wheelDef = b2DefaultWheelJointDef();
                    wheelDef.bodyIdA = baseId2;
                    wheelDef.bodyIdB = bodyId2;
                    wheelDef.localAxisA = new B2Vec2(1.0f, 0.0f);
                    wheelDef.localAnchorA = baseAnchor2;
                    wheelDef.localAnchorB = new B2Vec2(2.5f, 0.0f);
                    wheelDef.enableSpring = false;
                    wheelDef.collideConnected = true;

                    b2CreateWheelJoint(m_worldId, ref wheelDef);
                }
                else
                {
                    revoluteDef.bodyIdA = baseId2;
                    revoluteDef.bodyIdB = bodyId2;
                    revoluteDef.localAnchorA = baseAnchor2;
                    revoluteDef.localAnchorB = new B2Vec2(2.5f, 0.0f);
                    revoluteDef.enableMotor = false;
                    revoluteDef.maxMotorTorque = 1.0f;
                    revoluteDef.collideConnected = false;

                    b2CreateRevoluteJoint(m_worldId, ref revoluteDef);
                }

                // middle pin
                revoluteDef.bodyIdA = bodyId1;
                revoluteDef.bodyIdB = bodyId2;
                revoluteDef.localAnchorA = new B2Vec2(0.0f, 0.0f);
                revoluteDef.localAnchorB = new B2Vec2(0.0f, 0.0f);
                revoluteDef.enableMotor = false;
                revoluteDef.maxMotorTorque = 1.0f;
                revoluteDef.collideConnected = false;

                b2CreateRevoluteJoint(m_worldId, ref revoluteDef);

                baseId1 = bodyId2;
                baseId2 = bodyId1;
                baseAnchor1 = new B2Vec2(-2.5f, 0.0f);
                baseAnchor2 = new B2Vec2(2.5f, 0.0f);
                y += 1.0f;
            }

            bodyDef.position = new B2Vec2(0.0f, y);
            bodyDef.rotation = b2Rot_identity;
            B2BodyId platformId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(3.0f, 0.2f);
            b2CreatePolygonShape(platformId, ref shapeDef, box);

            // left pin
            {
                B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
                revoluteDef.bodyIdA = platformId;
                revoluteDef.bodyIdB = baseId1;
                revoluteDef.localAnchorA = new B2Vec2(-2.5f, -0.4f);
                revoluteDef.localAnchorB = baseAnchor1;
                revoluteDef.enableMotor = false;
                revoluteDef.maxMotorTorque = 1.0f;
                revoluteDef.collideConnected = true;
                b2CreateRevoluteJoint(m_worldId, ref revoluteDef);
            }

            // right pin
            {
                B2WheelJointDef wheelDef = b2DefaultWheelJointDef();
                wheelDef.bodyIdA = platformId;
                wheelDef.bodyIdB = baseId2;
                wheelDef.localAxisA = new B2Vec2(1.0f, 0.0f);
                wheelDef.localAnchorA = new B2Vec2(2.5f, -0.4f);
                wheelDef.localAnchorB = baseAnchor2;
                wheelDef.enableSpring = false;
                wheelDef.collideConnected = true;
                b2CreateWheelJoint(m_worldId, ref wheelDef);
            }

            m_enableMotor = false;
            m_motorSpeed = 0.25f;
            m_motorForce = 2000.0f;

            B2DistanceJointDef distanceDef = b2DefaultDistanceJointDef();
            distanceDef.bodyIdA = groundId;
            distanceDef.bodyIdB = linkId1;
            distanceDef.localAnchorA = new B2Vec2(-2.5f, 0.2f);
            distanceDef.localAnchorB = new B2Vec2(0.5f, 0.0f);
            distanceDef.enableSpring = true;
            distanceDef.minLength = 0.2f;
            distanceDef.maxLength = 5.5f;
            distanceDef.enableLimit = true;
            distanceDef.enableMotor = m_enableMotor;
            distanceDef.motorSpeed = m_motorSpeed;
            distanceDef.maxMotorForce = m_motorForce;
            m_liftJointId = b2CreateDistanceJoint(m_worldId, ref distanceDef);

            Car car = new Car();
            car.Spawn(m_worldId, new B2Vec2(0.0f, y + 2.0f), 1.0f, 3.0f, 0.7f, 0.0f, null);
        }
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 140.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, B2.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Scissor Lift", ref open, ImGuiWindowFlags.NoResize);

        if (ImGui.Checkbox("Motor", ref m_enableMotor))
        {
            b2DistanceJoint_EnableMotor(m_liftJointId, m_enableMotor);
            b2Joint_WakeBodies(m_liftJointId);
        }

        if (ImGui.SliderFloat("Max Force", ref m_motorForce, 0.0f, 3000.0f, "%.0f"))
        {
            b2DistanceJoint_SetMaxMotorForce(m_liftJointId, m_motorForce);
            b2Joint_WakeBodies(m_liftJointId);
        }

        if (ImGui.SliderFloat("Speed", ref m_motorSpeed, -0.3f, 0.3f, "%.2f"))
        {
            b2DistanceJoint_SetMotorSpeed(m_liftJointId, m_motorSpeed);
            b2Joint_WakeBodies(m_liftJointId);
        }

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);
    }
}