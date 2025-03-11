// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Box2D.NET.Shared.Primitives;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.Shared.Humans;

namespace Box2D.NET.Samples.Samples.Worlds;

public class LargeWorld : Sample
{
    private static readonly int SampleLargeWorld = SampleFactory.Shared.RegisterSample("World", "Large World", Create);

    private Car m_car;
    private B2Vec2 m_viewPosition;
    private float m_period;
    private int m_cycleCount;
    private int m_cycleIndex;
    private float m_gridCount;
    private float m_gridSize;
    private float m_speed;

    private B2Vec2 m_explosionPosition;
    private bool m_explode;
    private bool m_followCar;


    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new LargeWorld(ctx, settings);
    }

    public LargeWorld(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        m_period = 40.0f;
        float omega = 2.0f * B2_PI / m_period;
        m_cycleCount = m_context.sampleDebug ? 10 : 600;
        m_gridSize = 1.0f;
        m_gridCount = (int)(m_cycleCount * m_period / m_gridSize);

        float xStart = -0.5f * (m_cycleCount * m_period);

        m_viewPosition = new B2Vec2(xStart, 15.0f);

        if (settings.restart == false)
        {
            m_context.camera.m_center = m_viewPosition;
            m_context.camera.m_zoom = 25.0f * 1.0f;
            settings.drawJoints = false;
            settings.useCameraBounds = true;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            // Setting this to false significantly reduces the cost of creating
            // static bodies and shapes.
            shapeDef.invokeContactCreation = false;

            float height = 4.0f;
            float xBody = xStart;
            float xShape = xStart;

            B2BodyId groundId = new B2BodyId();

            for (int i = 0; i < m_gridCount; ++i)
            {
                // Create a new body regularly so that shapes are not too far from the body origin.
                // Most algorithms in Box2D work in local coordinates, but contact points are computed
                // relative to the body origin.
                // This makes a noticeable improvement in stability far from the origin.
                if (i % 10 == 0)
                {
                    bodyDef.position.x = xBody;
                    groundId = b2CreateBody(m_worldId, ref bodyDef);
                    xShape = 0.0f;
                }

                float y = 0.0f;

                int ycount = (int)MathF.Round(height * MathF.Cos(omega * xBody)) + 12;

                for (int j = 0; j < ycount; ++j)
                {
                    B2Polygon square = b2MakeOffsetBox(0.4f * m_gridSize, 0.4f * m_gridSize, new B2Vec2(xShape, y), b2Rot_identity);
                    square.radius = 0.1f;
                    b2CreatePolygonShape(groundId, ref shapeDef, ref square);

                    y += m_gridSize;
                }

                xBody += m_gridSize;
                xShape += m_gridSize;
            }
        }

        int humanIndex = 0;
        for (int cycleIndex = 0; cycleIndex < m_cycleCount; ++cycleIndex)
        {
            float xbase = (0.5f + cycleIndex) * m_period + xStart;

            int remainder = cycleIndex % 3;
            if (remainder == 0)
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Vec2(xbase - 3.0f, 10.0f);

                B2ShapeDef shapeDef = b2DefaultShapeDef();
                B2Polygon box = b2MakeBox(0.3f, 0.2f);

                for (int i = 0; i < 10; ++i)
                {
                    bodyDef.position.y = 10.0f;
                    for (int j = 0; j < 5; ++j)
                    {
                        B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                        b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
                        bodyDef.position.y += 0.5f;
                    }

                    bodyDef.position.x += 0.6f;
                }
            }
            else if (remainder == 1)
            {
                B2Vec2 position = new B2Vec2(xbase - 2.0f, 10.0f);
                for (int i = 0; i < 5; ++i)
                {
                    Human human = new Human();
                    CreateHuman(ref human, m_worldId, position, 1.5f, 0.05f, 0.0f, 0.0f, humanIndex + 1, null, false);
                    humanIndex += 1;
                    position.x += 1.0f;
                }
            }
            else
            {
                B2Vec2 position = new B2Vec2(xbase - 4.0f, 12.0f);

                for (int i = 0; i < 5; ++i)
                {
                    Donut donut = new Donut();
                    donut.Spawn(m_worldId, position, 0.75f, 0, null);
                    position.x += 2.0f;
                }
            }
        }

        m_car.Spawn(m_worldId, new B2Vec2(xStart + 20.0f, 40.0f), 10.0f, 2.0f, 0.7f, 2000.0f, null);

        m_cycleIndex = 0;
        m_speed = 0.0f;
        m_explosionPosition = new B2Vec2((0.5f + m_cycleIndex) * m_period + xStart, 7.0f);
        m_explode = true;
        m_followCar = false;
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 160.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Large World", ref open, ImGuiWindowFlags.NoResize);

        ImGui.SliderFloat("speed", ref m_speed, -400.0f, 400.0f, "%.0f");
        if (ImGui.Button("stop"))
        {
            m_speed = 0.0f;
        }

        ImGui.Checkbox("explode", ref m_explode);
        ImGui.Checkbox("follow car", ref m_followCar);

        ImGui.Text($"world size = {m_gridSize * m_gridCount / 1000.0f} kilometers");
        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        float span = 0.5f * (m_period * m_cycleCount);
        float timeStep = settings.hertz > 0.0f ? 1.0f / settings.hertz : 0.0f;

        if (settings.pause)
        {
            timeStep = 0.0f;
        }

        m_viewPosition.x += timeStep * m_speed;
        m_viewPosition.x = b2ClampFloat(m_viewPosition.x, -span, span);

        if (m_speed != 0.0f)
        {
            m_context.camera.m_center = m_viewPosition;
        }

        if (m_followCar)
        {
            m_context.camera.m_center.x = b2Body_GetPosition(m_car.m_chassisId).x;
        }

        float radius = 2.0f;
        if ((m_stepCount & 0x1) == 0x1 && m_explode)
        {
            m_explosionPosition.x = (0.5f + m_cycleIndex) * m_period - span;

            B2ExplosionDef def = b2DefaultExplosionDef();
            def.position = m_explosionPosition;
            def.radius = radius;
            def.falloff = 0.1f;
            def.impulsePerLength = 1.0f;
            b2World_Explode(m_worldId, ref def);

            m_cycleIndex = (m_cycleIndex + 1) % m_cycleCount;
        }

        if (m_explode)
        {
            m_context.draw.DrawCircle(m_explosionPosition, radius, B2HexColor.b2_colorAzure);
        }

        if (GetKey(Keys.A) == InputAction.Press)
        {
            m_car.SetSpeed(20.0f);
        }

        if (GetKey(Keys.S) == InputAction.Press)
        {
            m_car.SetSpeed(0.0f);
        }

        if (GetKey(Keys.D) == InputAction.Press)
        {
            m_car.SetSpeed(-5.0f);
        }

        base.Step(settings);
    }
}