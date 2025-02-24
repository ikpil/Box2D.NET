using Box2D.NET.Primitives;
using Silk.NET.GLFW;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;

public class RollingResistance : Sample
{
    float m_resistScale;
    float m_lift;
    static int sampleRollingResistance = RegisterSample("Shapes", "Rolling Resistance", Create);

    static Sample Create(Settings settings)
    {
        return new RollingResistance(settings);
    }

    public RollingResistance(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(5.0f, 20.0f);
            Draw.g_camera.m_zoom = 27.5f;
        }

        m_lift = 0.0f;
        m_resistScale = 0.02f;
        CreateScene();
    }

    void CreateScene()
    {
        b2Circle circle = new b2Circle(b2Vec2_zero, 0.5f);

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        for (int i = 0; i < 20; ++i)
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2Segment segment = new b2Segment(new b2Vec2(-40.0f, 2.0f * i), new b2Vec2(40.0f, 2.0f * i + m_lift));
            b2CreateSegmentShape(groundId, shapeDef, segment);

            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.position = new b2Vec2(-39.5f, 2.0f * i + 0.75f);
            bodyDef.angularVelocity = -10.0f;
            bodyDef.linearVelocity = new b2Vec2(5.0f, 0.0f);

            b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            shapeDef.rollingResistance = m_resistScale * i;
            b2CreateCircleShape(bodyId, shapeDef, circle);
        }
    }

    public override void Keyboard(int key)
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
            Draw.g_draw.DrawString(new b2Vec2(-41.5f, 2.0f * i + 1.0f), "%.2f", m_resistScale * i);
        }
    }
}