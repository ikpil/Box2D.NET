// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using Box2D.NET.Samples.Helpers;
using Silk.NET.GLFW;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.Shared.RandomSupports;
using static Box2D.NET.B2RevoluteJoints;


namespace Box2D.NET.Samples.Samples.Joints;

public class GearLift : Sample
{
    private static readonly int SampleGearLift = SampleFactory.Shared.RegisterSample("Joints", "Gear Lift", Create);

    private B2JointId m_driverId;
    private float m_motorTorque;
    private float m_motorSpeed;
    private bool m_enableMotor;

    private static Sample Create(SampleContext context)
    {
        return new GearLift(context);
    }

    public GearLift(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 6.0f);
            m_camera.zoom = 7.0f;
            m_context.debugDraw.drawJoints = false;
        }

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, bodyDef);

            string path =
                "m 63.500002,201.08333 103.187498,0 1e-5,-37.04166 h -2.64584 l 0,34.39583 h -42.33333 v -2.64583 l "
                + "-2.64584,-1e-5 v -2.64583 h -2.64583 v -2.64584 h -2.64584 v -2.64583 H 111.125 v -2.64583 h -2.64583 v "
                + "-2.64583 h -2.64583 v -2.64584 l -2.64584,1e-5 v -2.64583 l -2.64583,-1e-5 V 174.625 h -2.645834 v -2.64584 l "
                + "-2.645833,1e-5 v -2.64584 H 92.60417 v -2.64583 h -2.645834 v -2.64583 l -26.458334,0 0,37.04166";

            B2Vec2[] points = new B2Vec2[128];

            B2Vec2 offset = new B2Vec2(-120.0f, -200.0f);
            float scale = 0.2f;
            int count = SvgParser.ParsePath(path, offset, points, 64, scale, false);

            B2SurfaceMaterial material = b2DefaultSurfaceMaterial();
            material.customColor = (uint)B2HexColor.b2_colorDarkSeaGreen;

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = count;
            chainDef.isLoop = true;
            chainDef.materials = [material];
            chainDef.materialCount = 1;

            b2CreateChain(groundId, chainDef);
        }

        float gearRadius = 1.0f;
        float toothHalfWidth = 0.09f;
        float toothHalfHeight = 0.06f;
        float toothRadius = 0.03f;
        float linkHalfLength = 0.07f;
        float linkRadius = 0.05f;
        float linkCount = 40;
        float doorHalfHeight = 1.5f;

        B2Vec2 gearPosition1 = new B2Vec2(-4.25f, 9.75f);
        B2Vec2 gearPosition2 = gearPosition1 + new B2Vec2(2.0f, 1.0f);
        B2Vec2 linkAttachPosition = gearPosition2 + new B2Vec2(gearRadius + 2.0f * toothHalfWidth + toothRadius, 0.0f);
        B2Vec2 doorPosition = linkAttachPosition - new B2Vec2(0.0f, 2.0f * linkCount * linkHalfLength + doorHalfHeight);

        {
            B2Vec2 position = gearPosition1;
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = position;

            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.1f;
            shapeDef.material.customColor = (uint)B2HexColor.b2_colorSaddleBrown;
            B2Circle circle = new B2Circle(b2Vec2_zero, gearRadius);
            b2CreateCircleShape(bodyId, shapeDef, circle);

            int count = 16;
            float deltaAngle = 2.0f * B2_PI / 16;
            B2Rot dq = b2MakeRot(deltaAngle);
            B2Vec2 center = new B2Vec2(gearRadius + toothHalfHeight, 0.0f);
            B2Rot rotation = b2Rot_identity;

            for (int i = 0; i < count; ++i)
            {
                B2Polygon tooth = b2MakeOffsetRoundedBox(toothHalfWidth, toothHalfHeight, center, rotation, toothRadius);
                shapeDef.material.customColor = (uint)B2HexColor.b2_colorGray;
                b2CreatePolygonShape(bodyId, shapeDef, tooth);

                rotation = b2MulRot(dq, rotation);
                center = b2RotateVector(rotation, new B2Vec2(gearRadius + toothHalfHeight, 0.0f));
            }

            B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();

            m_motorTorque = 80.0f;
            m_motorSpeed = 0.0f;
            m_enableMotor = true;

            revoluteDef.@base.bodyIdA = groundId;
            revoluteDef.@base.bodyIdB = bodyId;
            revoluteDef.@base.localFrameA.p = b2Body_GetLocalPoint(groundId, position);
            revoluteDef.@base.localFrameB.p = b2Vec2_zero;
            revoluteDef.enableMotor = m_enableMotor;
            revoluteDef.maxMotorTorque = m_motorTorque;
            revoluteDef.motorSpeed = m_motorSpeed;
            m_driverId = b2CreateRevoluteJoint(m_worldId, revoluteDef);
        }

        B2BodyId followerId;

        {
            B2Vec2 position = gearPosition2;
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = position;

            followerId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.1f;
            shapeDef.material.customColor = (uint)B2HexColor.b2_colorSaddleBrown;
            B2Circle circle = new B2Circle(b2Vec2_zero, gearRadius);
            b2CreateCircleShape(followerId, shapeDef, circle);

            int count = 16;
            float deltaAngle = 2.0f * B2_PI / 16;
            B2Rot dq = b2MakeRot(deltaAngle);
            B2Vec2 center = new B2Vec2(gearRadius + toothHalfWidth, 0.0f);
            B2Rot rotation = b2Rot_identity;

            for (int i = 0; i < count; ++i)
            {
                B2Polygon tooth = b2MakeOffsetRoundedBox(toothHalfWidth, toothHalfHeight, center, rotation, toothRadius);
                shapeDef.material.customColor = (uint)B2HexColor.b2_colorGray;
                b2CreatePolygonShape(followerId, shapeDef, tooth);

                rotation = b2MulRot(dq, rotation);
                center = b2RotateVector(rotation, new B2Vec2(gearRadius + toothHalfWidth, 0.0f));
            }

            B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();

            revoluteDef.@base.bodyIdA = groundId;
            revoluteDef.@base.bodyIdB = followerId;
            revoluteDef.@base.localFrameA.p = b2Body_GetLocalPoint(groundId, position);
            revoluteDef.@base.localFrameA.q = b2MakeRot(0.25f * B2_PI);
            revoluteDef.@base.localFrameB.p = b2Vec2_zero;
            revoluteDef.enableMotor = true;
            revoluteDef.maxMotorTorque = 0.5f;
            revoluteDef.lowerAngle = -0.3f * B2_PI;
            revoluteDef.upperAngle = 0.8f * B2_PI;
            revoluteDef.enableLimit = true;
            b2CreateRevoluteJoint(m_worldId, revoluteDef);
        }

        B2BodyId lastLinkId;
        {
            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -linkHalfLength), new B2Vec2(0.0f, linkHalfLength), linkRadius);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 2.0f;
            shapeDef.material.customColor = (uint)B2HexColor.b2_colorLightSteelBlue;

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.maxMotorTorque = 0.05f;
            jointDef.enableMotor = true;

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2Vec2 position = linkAttachPosition + new B2Vec2(0.0f, -linkHalfLength);

            int count = 40;
            B2BodyId prevBodyId = followerId;
            for (int i = 0; i < count; ++i)
            {
                bodyDef.position = position;

                B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                b2CreateCapsuleShape(bodyId, shapeDef, capsule);

                B2Vec2 pivot = new B2Vec2(position.X, position.Y + linkHalfLength);
                jointDef.@base.bodyIdA = prevBodyId;
                jointDef.@base.bodyIdB = bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.@base.drawScale = 0.2f;
                b2CreateRevoluteJoint(m_worldId, jointDef);

                position.Y -= 2.0f * linkHalfLength;
                prevBodyId = bodyId;
            }

            lastLinkId = prevBodyId;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = doorPosition;

            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeBox(0.15f, doorHalfHeight);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.1f;
            shapeDef.material.customColor = (uint)B2HexColor.b2_colorDarkCyan;
            b2CreatePolygonShape(bodyId, shapeDef, box);

            {
                B2Vec2 pivot = doorPosition + new B2Vec2(0.0f, doorHalfHeight);
                B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
                revoluteDef.@base.bodyIdA = lastLinkId;
                revoluteDef.@base.bodyIdB = bodyId;
                revoluteDef.@base.localFrameA.p = b2Body_GetLocalPoint(lastLinkId, pivot);
                revoluteDef.@base.localFrameB.p = new B2Vec2(0.0f, doorHalfHeight);
                revoluteDef.enableMotor = true;
                revoluteDef.maxMotorTorque = 0.05f;
                b2CreateRevoluteJoint(m_worldId, revoluteDef);
            }

            {
                B2Vec2 localAxis = new B2Vec2(0.0f, 1.0f);
                B2PrismaticJointDef jointDef = b2DefaultPrismaticJointDef();
                jointDef.@base.bodyIdA = groundId;
                jointDef.@base.bodyIdB = bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(groundId, doorPosition);
                jointDef.@base.localFrameA.q = b2MakeRotFromUnitVector(localAxis);
                jointDef.@base.localFrameB.p = b2Vec2_zero;
                jointDef.@base.localFrameB.q = b2MakeRotFromUnitVector(localAxis);
                jointDef.maxMotorForce = 0.2f;
                jointDef.enableMotor = true;
                jointDef.@base.collideConnected = true;
                b2CreatePrismaticJoint(m_worldId, jointDef);
            }
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.rollingResistance = 0.3f;

            B2HexColor[] colors =
            [
                B2HexColor.b2_colorGray, B2HexColor.b2_colorGainsboro, B2HexColor.b2_colorLightGray, B2HexColor.b2_colorLightSlateGray, B2HexColor.b2_colorDarkGray,
            ];

            float y = 4.25f;
            int xCount = 10, yCount = 20;
            for (int i = 0; i < yCount; ++i)
            {
                float x = -3.15f;
                for (int j = 0; j < xCount; ++j)
                {
                    bodyDef.position = new B2Vec2(x, y);
                    B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                    B2Polygon poly = RandomPolygon(0.1f);
                    poly.radius = RandomFloatRange(0.01f, 0.02f);

                    int colorIndex = RandomIntRange(0, 4);
                    shapeDef.material.customColor = (uint)colors[colorIndex];

                    b2CreatePolygonShape(bodyId, shapeDef, poly);
                    x += 0.2f;
                }

                y += 0.2f;
            }
        }
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 120.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 25.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Gear Lift", ImGuiWindowFlags.NoResize);

        if (ImGui.Checkbox("Motor", ref m_enableMotor))
        {
            b2RevoluteJoint_EnableMotor(m_driverId, m_enableMotor);
            b2Joint_WakeBodies(m_driverId);
        }

        if (ImGui.SliderFloat("Max Torque", ref m_motorTorque, 0.0f, 100.0f, "%.0f"))
        {
            b2RevoluteJoint_SetMaxMotorTorque(m_driverId, m_motorTorque);
            b2Joint_WakeBodies(m_driverId);
        }

        if (ImGui.SliderFloat("Speed", ref m_motorSpeed, -0.3f, 0.3f, "%.2f"))
        {
            b2RevoluteJoint_SetMotorSpeed(m_driverId, m_motorSpeed);
            b2Joint_WakeBodies(m_driverId);
        }

        ImGui.End();
    }

    public override void Step()
    {
        if (InputAction.Release == GetKey(Keys.A))
        {
            m_motorSpeed = b2MaxFloat(-0.3f, m_motorSpeed - 0.01f);
            b2RevoluteJoint_SetMotorSpeed(m_driverId, m_motorSpeed);
            b2Joint_WakeBodies(m_driverId);
        }

        if (InputAction.Release == GetKey(Keys.D))
        {
            m_motorSpeed = b2MinFloat(0.3f, m_motorSpeed + 0.01f);
            b2RevoluteJoint_SetMotorSpeed(m_driverId, m_motorSpeed);
            b2Joint_WakeBodies(m_driverId);
        }

        base.Step();
    }
}