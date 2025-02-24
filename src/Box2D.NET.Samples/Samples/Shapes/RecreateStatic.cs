using Box2D.NET.Primitives;
using static Box2D.NET.id;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Shapes;

// This sample tests a static shape being recreated every step.
public class RecreateStatic : Sample
{
    b2BodyId m_groundId;
    static int sampleSingleBox = RegisterSample("Shapes", "Recreate Static", Create);

    static Sample Create(Settings settings)
    {
        return new RecreateStatic(settings);
    }

    public RecreateStatic(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 2.5f);
            Draw.g_camera.m_zoom = 3.5f;
        }

        b2BodyDef bodyDef = b2DefaultBodyDef();
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = new b2Vec2(0.0f, 1.0f);
        b2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

        b2Polygon box = b2MakeBox(1.0f, 1.0f);
        b2CreatePolygonShape(bodyId, shapeDef, box);

        m_groundId = new b2BodyId();
    }

    public override void Step(Settings settings)
    {
        if (B2_IS_NON_NULL(m_groundId))
        {
            b2DestroyBody(m_groundId);
            m_groundId = new b2BodyId();
        }

        b2BodyDef bodyDef = b2DefaultBodyDef();
        m_groundId = b2CreateBody(m_worldId, bodyDef);

        b2ShapeDef shapeDef = b2DefaultShapeDef();

        // Invoke contact creation so that contact points are created immediately
        // on a static body.
        shapeDef.invokeContactCreation = true;

        b2Segment segment = new b2Segment(new b2Vec2(-10.0f, 0.0f), new b2Vec2(10.0f, 0.0f));
        b2CreateSegmentShape(m_groundId, shapeDef, segment);

        base.Step(settings);
    }
}