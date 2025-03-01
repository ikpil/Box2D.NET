// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Silk.NET.GLFW;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Shapes;

public class RollingResistance : Sample
{
    float m_resistScale;
    float m_lift;
    private static readonly int SampleRollingResistance = SampleFactory.Shared.RegisterSample("Shapes", "Rolling Resistance", Create);

    private static Sample Create(Settings settings)
    {
        return new RollingResistance(settings);
    }

    public RollingResistance(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(5.0f, 20.0f);
            B2.g_camera.m_zoom = 27.5f;
        }

        m_lift = 0.0f;
        m_resistScale = 0.02f;
        CreateScene();
    }

    void CreateScene()
    {
        B2Circle circle = new B2Circle(b2Vec2_zero, 0.5f);

        B2ShapeDef shapeDef = b2DefaultShapeDef();

        for (int i = 0; i < 20; ++i)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 2.0f * i), new B2Vec2(40.0f, 2.0f * i + m_lift));
            b2CreateSegmentShape(groundId, shapeDef, segment);

            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-39.5f, 2.0f * i + 0.75f);
            bodyDef.angularVelocity = -10.0f;
            bodyDef.linearVelocity = new B2Vec2(5.0f, 0.0f);

            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            shapeDef.rollingResistance = m_resistScale * i;
            b2CreateCircleShape(bodyId, shapeDef, circle);
        }
    }

    public override void Keyboard(Keys key)
    {
        switch ((Keys)key)
        {
            case Keys.Number1:
                m_lift = 0.0f;
                CreateWorld();
                CreateScene();
                break;

            case Keys.Number2:
                m_lift = 5.0f;
                CreateWorld();
                CreateScene();
                break;

            case Keys.Number3:
                m_lift = -5.0f;
                CreateWorld();
                CreateScene();
                break;

            default:
                base.Keyboard(key);
                break;
        }
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        for (int i = 0; i < 20; ++i)
        {
            B2.g_draw.DrawString(new B2Vec2(-41.5f, 2.0f * i + 1.0f), "%.2f", m_resistScale * i);
        }
    }
}
