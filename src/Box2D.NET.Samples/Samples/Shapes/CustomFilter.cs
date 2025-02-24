using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;

namespace Box2D.NET.Samples.Samples.Shapes;

// This shows how to use custom filtering
public class CustomFilter : Sample
{
    public const int e_count = 10;

    b2BodyId[] m_bodyIds = new b2BodyId[e_count];
    b2ShapeId[] m_shapeIds = new b2ShapeId[e_count];
    static int sampleCustomFilter = RegisterSample("Shapes", "Custom Filter", Create);

    static Sample Create(Settings settings)
    {
        return new CustomFilter(settings);
    }

    public CustomFilter(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 5.0f);
            Draw.g_camera.m_zoom = 10.0f;
        }

        // Register custom filter
        b2World_SetCustomFilterCallback(m_worldId, CustomFilterStatic, this);

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);
            b2Segment segment = new b2Segment(new b2Vec2(-40.0f, 0.0f), new b2Vec2(40.0f, 0.0f));

            b2ShapeDef shapeDef = b2DefaultShapeDef();

            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = b2BodyType.b2_dynamicBody;
            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Polygon box = b2MakeSquare(1.0f);
            float x = -e_count;

            for (int i = 0; i < e_count; ++i)
            {
                bodyDef.position = new b2Vec2(x, 5.0f);
                m_bodyIds[i] = b2CreateBody(m_worldId, bodyDef);

                shapeDef.userData = i + 1;
                m_shapeIds[i] = b2CreatePolygonShape(m_bodyIds[i], shapeDef, box);
                x += 2.0f;
            }
        }
    }

    public override void Step(Settings settings)
    {
        Draw.g_draw.DrawString(5, m_textLine, "Custom filter disables collision between odd and even shapes");
        m_textLine += m_textIncrement;

        base.Step(settings);

        for (int i = 0; i < e_count; ++i)
        {
            b2Vec2 p = b2Body_GetPosition(m_bodyIds[i]);
            Draw.g_draw.DrawString(new b2Vec2(p.x, p.y), "%d", i);
        }
    }

    bool ShouldCollide(b2ShapeId shapeIdA, b2ShapeId shapeIdB)
    {
        object userDataA = b2Shape_GetUserData(shapeIdA);
        object userDataB = b2Shape_GetUserData(shapeIdB);

        if (userDataA == null || userDataB == null)
        {
            return true;
        }

        int indexA = (int)userDataA;
        int indexB = (int)userDataB;

        return ((indexA & 1) + (indexB & 1)) != 1;
    }

    static bool CustomFilterStatic(b2ShapeId shapeIdA, b2ShapeId shapeIdB, object context)
    {
        CustomFilter customFilter = context as CustomFilter;

        return customFilter.ShouldCollide(shapeIdA, shapeIdB);
    }
}