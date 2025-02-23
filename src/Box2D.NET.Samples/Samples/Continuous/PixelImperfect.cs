using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Continuous;

// This shows that Box2D does not have pixel perfect collision.
public class PixelImperfect : Sample
{
    b2BodyId m_ballId;

    static int samplePixelImperfect = RegisterSample("Continuous", "Pixel Imperfect", Create);

    static Sample Create(Settings settings)
    {
        return new PixelImperfect(settings);
    }

    public PixelImperfect(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(7.0f, 5.0f);
            Draw.g_camera.m_zoom = 6.0f;
        }

        float pixelsPerMeter = 30.0f;

        {
            b2BodyDef block4BodyDef = b2DefaultBodyDef();
            block4BodyDef.type = b2BodyType.b2_staticBody;
            block4BodyDef.position = new b2Vec2(175.0f / pixelsPerMeter, 150.0f / pixelsPerMeter);
            b2BodyId block4BodyId = b2CreateBody(m_worldId, block4BodyDef);
            b2Polygon block4Shape = b2MakeBox(20.0f / pixelsPerMeter, 10.0f / pixelsPerMeter);
            b2ShapeDef block4ShapeDef = b2DefaultShapeDef();
            block4ShapeDef.friction = 0.0f;
            b2CreatePolygonShape(block4BodyId, block4ShapeDef, block4Shape);
        }

        {
            b2BodyDef ballBodyDef = b2DefaultBodyDef();
            ballBodyDef.type = b2BodyType.b2_dynamicBody;
            ballBodyDef.position = new b2Vec2(200.0f / pixelsPerMeter, 275.0f / pixelsPerMeter);
            ballBodyDef.gravityScale = 0.0f;

            m_ballId = b2CreateBody(m_worldId, ballBodyDef);
            // Ball shape
            //b2Polygon ballShape = b2MakeBox( 5.f / pixelsPerMeter, 5.f / pixelsPerMeter );
            b2Polygon ballShape = b2MakeRoundedBox(4.0f / pixelsPerMeter, 4.0f / pixelsPerMeter, 0.9f / pixelsPerMeter);
            b2ShapeDef ballShapeDef = b2DefaultShapeDef();
            ballShapeDef.friction = 0.0f;
            //ballShapeDef.restitution = 1.f;
            b2CreatePolygonShape(m_ballId, ballShapeDef, ballShape);
            b2Body_SetLinearVelocity(m_ballId, new b2Vec2(0.0f, -5.0f));
            b2Body_SetFixedRotation(m_ballId, true);
        }
    }

    public override void Step(Settings settings)
    {
        b2ContactData data = new b2ContactData();
        b2Body_GetContactData(m_ballId, [data], 1);

        b2Vec2 p = b2Body_GetPosition(m_ballId);
        b2Vec2 v = b2Body_GetLinearVelocity(m_ballId);
        Draw.g_draw.DrawString(5, m_textLine, "p.x = %.9f, v.y = %.9f", p.x, v.y);
        m_textLine += m_textIncrement;

        base.Step(settings);
    }
}