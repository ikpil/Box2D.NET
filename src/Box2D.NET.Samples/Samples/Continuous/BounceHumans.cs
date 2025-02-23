using Box2D.NET.Primitives;
using Box2D.NET.Shared.Primitives;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;
using static Box2D.NET.Shared.human;

namespace Box2D.NET.Samples.Samples.Continuous;

public class BounceHumans : Sample
{
    Human[] m_humans = new Human[5];
    int m_humanCount = 0;
    float m_countDown = 0.0f;
    float m_time = 0.0f;

    static int sampleBounceHumans = RegisterSample("Continuous", "Bounce Humans", Create);

    static Sample Create(Settings settings)
    {
        return new BounceHumans(settings);
    }


    public BounceHumans(Settings settings) : base(settings)
    {
        Draw.g_camera.m_center = new b2Vec2(0.0f, 0.0f);
        Draw.g_camera.m_zoom = 12.0f;

        for (int i = 0; i < m_humans.Length; ++i)
        {
            m_humans[i] = new Human();
        }

        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.restitution = 1.3f;
        shapeDef.friction = 0.1f;

        {
            b2Segment segment = new b2Segment(new b2Vec2(-10.0f, -10.0f), new b2Vec2(10.0f, -10.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        {
            b2Segment segment = new b2Segment(new b2Vec2(10.0f, -10.0f), new b2Vec2(10.0f, 10.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        {
            b2Segment segment = new b2Segment(new b2Vec2(10.0f, 10.0f), new b2Vec2(-10.0f, 10.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        {
            b2Segment segment = new b2Segment(new b2Vec2(-10.0f, 10.0f), new b2Vec2(-10.0f, -10.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 2.0f);
        shapeDef.restitution = 2.0f;
        b2CreateCircleShape(groundId, shapeDef, circle);
    }

    public override void Step(Settings settings)
    {
        if (m_humanCount < 5 && m_countDown <= 0.0f)
        {
            float jointFrictionTorque = 0.0f;
            float jointHertz = 1.0f;
            float jointDampingRatio = 0.1f;

            CreateHuman(m_humans[m_humanCount], m_worldId, new b2Vec2(0.0f, 5.0f), 1.0f, jointFrictionTorque, jointHertz,
                jointDampingRatio, 1, null, true);
            // Human_SetVelocity( m_humans + m_humanCount, { 10.0f - 5.0f * m_humanCount, -20.0f + 5.0f * m_humanCount } );

            m_countDown = 2.0f;
            m_humanCount += 1;
        }

        float timeStep = 1.0f / 60.0f;
        b2CosSin cs1 = b2ComputeCosSin(0.5f * m_time);
        b2CosSin cs2 = b2ComputeCosSin(m_time);
        float gravity = 10.0f;
        b2Vec2 gravityVec = new b2Vec2(gravity * cs1.sine, gravity * cs2.cosine);
        Draw.g_draw.DrawSegment(b2Vec2_zero, new b2Vec2(3.0f * cs1.sine, 3.0f * cs2.cosine), b2HexColor.b2_colorWhite);
        m_time += timeStep;
        m_countDown -= timeStep;
        b2World_SetGravity(m_worldId, gravityVec);

        base.Step(settings);
    }
}