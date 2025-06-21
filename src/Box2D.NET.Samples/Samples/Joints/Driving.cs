﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples.Joints;

// This is a fun demo that shows off the wheel joint
public class Driving : Sample
{
    private static readonly int SampleDriving = SampleFactory.Shared.RegisterSample("Joints", "Driving", Create);

    private Car m_car;

    private float m_throttle;
    private float m_hertz;
    private float m_dampingRatio;
    private float m_torque;
    private float m_speed;


    private static Sample Create(SampleContext context)
    {
        return new Driving(context);
    }

    public Driving(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center.Y = 5.0f;
            m_camera.m_zoom = 25.0f * 0.4f;
            m_context.settings.drawJoints = false;
        }

        B2BodyId groundId;
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Vec2[] points = new B2Vec2[25];
            int count = 24;

            // fill in reverse to match line list convention
            points[count--] = new B2Vec2(-20.0f, -20.0f);
            points[count--] = new B2Vec2(-20.0f, 0.0f);
            points[count--] = new B2Vec2(20.0f, 0.0f);

            float[] hs = new float[10] { 0.25f, 1.0f, 4.0f, 0.0f, 0.0f, -1.0f, -2.0f, -2.0f, -1.25f, 0.0f };
            float x = 20.0f, dx = 5.0f;

            for (int j = 0; j < 2; ++j)
            {
                for (int i = 0; i < 10; ++i)
                {
                    float y2 = hs[i];
                    points[count--] = new B2Vec2(x + dx, y2);
                    x += dx;
                }
            }

            // flat before bridge
            points[count--] = new B2Vec2(x + 40.0f, 0.0f);
            points[count--] = new B2Vec2(x + 40.0f, -20.0f);

            B2_ASSERT(count == -1);

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = 25;
            chainDef.isLoop = true;
            b2CreateChain(groundId, ref chainDef);

            // flat after bridge
            x += 80.0f;
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(x, 0.0f), new B2Vec2(x + 40.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

            // jump ramp
            x += 40.0f;
            segment = new B2Segment(new B2Vec2(x, 0.0f), new B2Vec2(x + 10.0f, 5.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

            // final corner
            x += 20.0f;
            segment = new B2Segment(new B2Vec2(x, 0.0f), new B2Vec2(x + 40.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);

            x += 40.0f;
            segment = new B2Segment(new B2Vec2(x, 0.0f), new B2Vec2(x, 20.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        // Teeter
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(140.0f, 1.0f);
            bodyDef.angularVelocity = 1.0f;
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeBox(10.0f, 0.25f);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            B2Vec2 pivot = bodyDef.position;
            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = bodyId;
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
            jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
            jointDef.lowerAngle = -8.0f * B2_PI / 180.0f;
            jointDef.upperAngle = 8.0f * B2_PI / 180.0f;
            jointDef.enableLimit = true;
            b2CreateRevoluteJoint(m_worldId, ref jointDef);
        }

        // Bridge
        {
            int N = 20;
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Capsule capsule = new B2Capsule(new B2Vec2(-1.0f, 0.0f), new B2Vec2(1.0f, 0.0f), 0.125f);

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();

            B2BodyId prevBodyId = groundId;
            for (int i = 0; i < N; ++i)
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(161.0f + 2.0f * i, -0.125f);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);

                B2Vec2 pivot = new B2Vec2(160.0f + 2.0f * i, -0.125f);
                jointDef.@base.bodyIdA = prevBodyId;
                jointDef.@base.bodyIdB = bodyId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                b2CreateRevoluteJoint(m_worldId, ref jointDef);

                prevBodyId = bodyId;
            }

            {
                B2Vec2 pivot = new B2Vec2(160.0f + 2.0f * N, -0.125f);
                jointDef.@base.bodyIdA = prevBodyId;
                jointDef.@base.bodyIdB = groundId;
                jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdA, pivot);
                jointDef.@base.localFrameB.p = b2Body_GetLocalPoint(jointDef.@base.bodyIdB, pivot);
                jointDef.enableMotor = true;
                jointDef.maxMotorTorque = 50.0f;
                b2CreateRevoluteJoint(m_worldId, ref jointDef);
            }
        }


        // Boxes
        {
            B2Polygon box = b2MakeBox(0.5f, 0.5f);

            B2BodyId bodyId;
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.friction = 0.25f;
            shapeDef.material.restitution = 0.25f;
            shapeDef.density = 0.25f;

            bodyDef.position = new B2Vec2(230.0f, 0.5f);
            bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            bodyDef.position = new B2Vec2(230.0f, 1.5f);
            bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            bodyDef.position = new B2Vec2(230.0f, 2.5f);
            bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            bodyDef.position = new B2Vec2(230.0f, 3.5f);
            bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);

            bodyDef.position = new B2Vec2(230.0f, 4.5f);
            bodyId = b2CreateBody(m_worldId, ref bodyDef);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        // Car

        m_throttle = 0.0f;
        m_speed = 35.0f;
        m_torque = 5.0f;
        m_hertz = 5.0f;
        m_dampingRatio = 0.7f;

        m_car.Spawn(m_worldId, new B2Vec2(0.0f, 0.0f), 1.0f, m_hertz, m_dampingRatio, m_torque, null);
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float height = 140.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));

        ImGui.Begin("Driving", ImGuiWindowFlags.NoResize);

        ImGui.PushItemWidth(100.0f);
        if (ImGui.SliderFloat("Spring Hertz", ref m_hertz, 0.0f, 20.0f, "%.0f"))
        {
            m_car.SetHertz(m_hertz);
        }

        if (ImGui.SliderFloat("Damping Ratio", ref m_dampingRatio, 0.0f, 10.0f, "%.1f"))
        {
            m_car.SetDampingRadio(m_dampingRatio);
        }

        if (ImGui.SliderFloat("Speed", ref m_speed, 0.0f, 50.0f, "%.0f"))
        {
            m_car.SetSpeed(m_throttle * m_speed);
        }

        if (ImGui.SliderFloat("Torque", ref m_torque, 0.0f, 10.0f, "%.1f"))
        {
            m_car.SetTorque(m_torque);
        }

        ImGui.PopItemWidth();

        ImGui.End();
    }

    public override void Step()
    {
        if (GetKey(Keys.A) == InputAction.Press)
        {
            m_throttle = 1.0f;
            m_car.SetSpeed(m_speed);
        }

        if (GetKey(Keys.S) == InputAction.Press)
        {
            m_throttle = 0.0f;
            m_car.SetSpeed(0.0f);
        }

        if (GetKey(Keys.D) == InputAction.Press)
        {
            m_throttle = -1.0f;
            m_car.SetSpeed(-m_speed);
        }

        base.Step();
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        DrawTextLine("Keys: left = a, brake = s, right = d");


        B2Vec2 linearVelocity = b2Body_GetLinearVelocity(m_car.m_chassisId);
        float kph = linearVelocity.X * 3.6f;
        DrawTextLine($"speed in kph: {kph:G2}");


        B2Vec2 carPosition = b2Body_GetPosition(m_car.m_chassisId);
        m_camera.m_center.X = carPosition.X;
    }
}