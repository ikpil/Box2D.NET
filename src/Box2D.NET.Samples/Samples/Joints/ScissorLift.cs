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
    private static readonly int SampleScissorLift = SampleFactory.Shared.RegisterSample("Joints", "Scissor Lift", Create);

    private B2JointId m_liftJointId;
    private float m_motorForce;
    private float m_motorSpeed;
    private bool m_enableMotor;

    private static Sample Create(SampleContext context)
    {
        return new ScissorLift(context);
    }

    public ScissorLift(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 9.0f);
            m_camera.m_zoom = 25.0f * 0.4f;
        }

        // Need 8 sub-steps for smoother operation
        m_context.settings.subStepCount = 8;

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
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

            float constraintDampingRatio = 20.0f;
            float constraintHertz = 240.0f;

            for (int i = 0; i < N; ++i)
            {
                bodyDef.position = new B2Vec2(0.0f, y);
                bodyDef.rotation = b2MakeRot(0.15f);
                B2BodyId bodyId1 = b2CreateBody(m_worldId, ref bodyDef);
                b2CreateCapsuleShape(bodyId1, ref shapeDef, ref capsule);

                bodyDef.position = new B2Vec2(0.0f, y);
                bodyDef.rotation = b2MakeRot(-0.15f);

                B2BodyId bodyId2 = b2CreateBody(m_worldId, ref bodyDef);
                b2CreateCapsuleShape(bodyId2, ref shapeDef, ref capsule);

                if (i == 1)
                {
                    linkId1 = bodyId2;
                }

                B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();

                // left pin
                revoluteDef.@base.bodyIdA = baseId1;
                revoluteDef.@base.bodyIdB = bodyId1;
                revoluteDef.@base.localFrameA.p = baseAnchor1;
                revoluteDef.@base.localFrameB.p = new B2Vec2(-2.5f, 0.0f);
                revoluteDef.@base.collideConnected = (i == 0) ? true : false;
                revoluteDef.@base.constraintDampingRatio = constraintDampingRatio;
                revoluteDef.@base.constraintHertz = constraintHertz;

                b2CreateRevoluteJoint(m_worldId, ref revoluteDef);

                // right pin
                if (i == 0)
                {
                    B2WheelJointDef wheelDef = b2DefaultWheelJointDef();
                    wheelDef.@base.bodyIdA = baseId2;
                    wheelDef.@base.bodyIdB = bodyId2;
                    wheelDef.@base.localFrameA.p = baseAnchor2;
                    wheelDef.@base.localFrameB.p = new B2Vec2(2.5f, 0.0f);
                    wheelDef.enableSpring = false;
                    wheelDef.@base.collideConnected = true;
                    wheelDef.@base.constraintDampingRatio = constraintDampingRatio;
                    wheelDef.@base.constraintHertz = constraintHertz;

                    b2CreateWheelJoint(m_worldId, ref wheelDef);
                }
                else
                {
                    revoluteDef.@base.bodyIdA = baseId2;
                    revoluteDef.@base.bodyIdB = bodyId2;
                    revoluteDef.@base.localFrameA.p = baseAnchor2;
                    revoluteDef.@base.localFrameB.p = new B2Vec2(2.5f, 0.0f);
                    revoluteDef.@base.collideConnected = false;
                    revoluteDef.@base.constraintDampingRatio = constraintDampingRatio;
                    revoluteDef.@base.constraintHertz = constraintHertz;

                    b2CreateRevoluteJoint(m_worldId, ref revoluteDef);
                }

                // middle pin
                revoluteDef.@base.bodyIdA = bodyId1;
                revoluteDef.@base.bodyIdB = bodyId2;
                revoluteDef.@base.localFrameA.p = new B2Vec2(0.0f, 0.0f);
                revoluteDef.@base.localFrameB.p = new B2Vec2(0.0f, 0.0f);
                revoluteDef.@base.collideConnected = false;
                revoluteDef.@base.constraintDampingRatio = constraintDampingRatio;
                revoluteDef.@base.constraintHertz = constraintHertz;

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
            b2CreatePolygonShape(platformId, ref shapeDef, ref box);

            // left pin
            {
                B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
                revoluteDef.@base.bodyIdA = platformId;
                revoluteDef.@base.bodyIdB = baseId1;
                revoluteDef.@base.localFrameA.p = new B2Vec2(-2.5f, -0.4f);
                revoluteDef.@base.localFrameB.p = baseAnchor1;
                revoluteDef.@base.collideConnected = true;
                revoluteDef.@base.constraintDampingRatio = constraintDampingRatio;
                revoluteDef.@base.constraintHertz = constraintHertz;
                b2CreateRevoluteJoint(m_worldId, ref revoluteDef);
            }

            // right pin
            {
                B2WheelJointDef wheelDef = b2DefaultWheelJointDef();
                wheelDef.@base.bodyIdA = platformId;
                wheelDef.@base.bodyIdB = baseId2;
                wheelDef.@base.localFrameA.p = new B2Vec2(2.5f, -0.4f);
                wheelDef.@base.localFrameB.p = baseAnchor2;
                wheelDef.enableSpring = false;
                wheelDef.@base.collideConnected = true;
                wheelDef.@base.constraintDampingRatio = constraintDampingRatio;
                wheelDef.@base.constraintHertz = constraintHertz;
                b2CreateWheelJoint(m_worldId, ref wheelDef);
            }

            m_enableMotor = false;
            m_motorSpeed = 0.25f;
            m_motorForce = 2000.0f;

            B2DistanceJointDef distanceDef = b2DefaultDistanceJointDef();
            distanceDef.@base.bodyIdA = groundId;
            distanceDef.@base.bodyIdB = linkId1;
            distanceDef.@base.localFrameA.p = new B2Vec2(-2.5f, 0.2f);
            distanceDef.@base.localFrameB.p = new B2Vec2(0.5f, 0.0f);
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

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 140.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.m_height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Scissor Lift", ImGuiWindowFlags.NoResize);

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
}