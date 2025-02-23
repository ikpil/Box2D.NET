using Box2D.NET.Primitives;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.distance;

namespace Box2D.NET.Samples.Samples.Continuous;

public class ChainSlide : Sample
{
    static int sampleChainSlide = RegisterSample("Continuous", "Chain Slide", Create);

    static Sample Create(Settings settings)
    {
        return new ChainSlide(settings);
    }

    public ChainSlide(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 10.0f);
            Draw.g_camera.m_zoom = 15.0f;
        }

#if DEBUG
        b2_toiHitCount = 0;
#endif

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            const int count = 80;
            b2Vec2[] points = new b2Vec2[count];

            float w = 2.0f;
            float h = 1.0f;
            float x = 20.0f, y = 0.0f;
            for (int i = 0; i < 20; ++i)
            {
                points[i] = new b2Vec2(x, y);
                x -= w;
            }

            for (int i = 20; i < 40; ++i)
            {
                points[i] = new b2Vec2(x, y);
                y += h;
            }

            for (int i = 40; i < 60; ++i)
            {
                points[i] = new b2Vec2(x, y);
                x += w;
            }

            for (int i = 60; i < 80; ++i)
            {
                points[i] = new b2Vec2(x, y);
                y -= h;
            }

            b2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = count;
            chainDef.isLoop = true;

            b2CreateChain(groundId, chainDef);
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.linearVelocity = new b2Vec2(100.0f, 0.0f);
            bodyDef.position = new b2Vec2(-19.5f, 0.0f + 0.5f);
            b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.0f;
            b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.5f);
            b2CreateCircleShape(bodyId, shapeDef, circle);
        }
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

#if DEBUG
        Draw.g_draw.DrawString(5, m_textLine, "toi hits = %d", b2_toiHitCount);
        m_textLine += m_textIncrement;
#endif
    }
}