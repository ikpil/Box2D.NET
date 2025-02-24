using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Joints;

public class SoftBody : Sample
{
    Donut m_donut;
    static int sampleDonut = RegisterSample("Joints", "Soft Body", Create);

    static Sample Create(Settings settings)
    {
        return new SoftBody(settings);
    }

    public SoftBody(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 5.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.25f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment = new b2Segment(new b2Vec2(-20.0f, 0.0f), new b2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        m_donut = new();
        m_donut.Spawn(m_worldId, new b2Vec2(0.0f, 10.0f), 2.0f, 0, null);
    }
}