// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Shared.Primitives;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.Shared.Humans;

namespace Box2D.NET.Samples.Samples.Continuous;

public class BounceHumans : Sample
{
    private static readonly int SampleBounceHumans = SampleFactory.Shared.RegisterSample("Continuous", "Bounce Humans", Create);
    
    private Human[] m_humans = new Human[5];
    private int m_humanCount = 0;
    private float m_countDown = 0.0f;
    private float m_time = 0.0f;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BounceHumans(ctx, settings);
    }


    public BounceHumans(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        B2.g_camera.m_center = new B2Vec2(0.0f, 0.0f);
        B2.g_camera.m_zoom = 12.0f;

        for (int i = 0; i < m_humans.Length; ++i)
        {
            m_humans[i] = new Human();
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.restitution = 1.3f;
        shapeDef.friction = 0.1f;

        {
            B2Segment segment = new B2Segment(new B2Vec2(-10.0f, -10.0f), new B2Vec2(10.0f, -10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2Segment segment = new B2Segment(new B2Vec2(10.0f, -10.0f), new B2Vec2(10.0f, 10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2Segment segment = new B2Segment(new B2Vec2(10.0f, 10.0f), new B2Vec2(-10.0f, 10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2Segment segment = new B2Segment(new B2Vec2(-10.0f, 10.0f), new B2Vec2(-10.0f, -10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 2.0f);
        shapeDef.restitution = 2.0f;
        b2CreateCircleShape(groundId, ref shapeDef, ref circle);
    }

    public override void Step(Settings settings)
    {
        if (m_humanCount < 5 && m_countDown <= 0.0f)
        {
            float jointFrictionTorque = 0.0f;
            float jointHertz = 1.0f;
            float jointDampingRatio = 0.1f;

            CreateHuman(ref m_humans[m_humanCount], m_worldId, new B2Vec2(0.0f, 5.0f), 1.0f, jointFrictionTorque, jointHertz,
                jointDampingRatio, 1, null, true);
            // Human_SetVelocity( m_humans + m_humanCount, { 10.0f - 5.0f * m_humanCount, -20.0f + 5.0f * m_humanCount } );

            m_countDown = 2.0f;
            m_humanCount += 1;
        }

        float timeStep = 1.0f / 60.0f;
        B2CosSin cs1 = b2ComputeCosSin(0.5f * m_time);
        B2CosSin cs2 = b2ComputeCosSin(m_time);
        float gravity = 10.0f;
        B2Vec2 gravityVec = new B2Vec2(gravity * cs1.sine, gravity * cs2.cosine);
        B2.g_draw.DrawSegment(b2Vec2_zero, new B2Vec2(3.0f * cs1.sine, 3.0f * cs2.cosine), B2HexColor.b2_colorWhite);
        m_time += timeStep;
        m_countDown -= timeStep;
        b2World_SetGravity(m_worldId, gravityVec);

        base.Step(settings);
    }
}
