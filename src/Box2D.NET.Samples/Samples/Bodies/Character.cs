using System.Numerics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.wheel_joint;
using static Box2D.NET.world;
using static Box2D.NET.mouse_joint;

namespace Box2D.NET.Samples.Primitives;

/// This is a test of typical character collision scenarios. This does not
/// show how you should implement a character in your application.
/// Instead this is used to test smooth collision on chain shapes.
class Character : Sample
{
    private static int sampleCharacter = SampleRegister.RegisterSample("Bodies", "Character", Create);

    b2BodyId m_circleCharacterId;
    b2BodyId m_capsuleCharacterId;
    b2BodyId m_boxCharacterId;

    private static Sample Create(Settings settings)
    {
        return new Character(settings);
    }

    public Character(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(-2.0f, 7.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.4f;
        }

        // Ground body
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment = new b2Segment(new b2Vec2(-20.0f, 0.0f), new b2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        // Collinear edges with no adjacency information.
        // This shows the problematic case where a box shape can hit
        // an internal vertex.
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment1 = new b2Segment(new b2Vec2(-8.0f, 1.0f), new b2Vec2(-6.0f, 1.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment1);

            b2Segment segment2 = new b2Segment(new b2Vec2(-6.0f, 1.0f), new b2Vec2(-4.0f, 1.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment2);

            b2Segment segment3 = new b2Segment(new b2Vec2(-4.0f, 1.0f), new b2Vec2(-2.0f, 1.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment3);
        }

        // Chain shape
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.rotation = b2MakeRot(0.25f * B2_PI);
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2Vec2[] points = new b2Vec2[] { new(8.0f, 7.0f), new(7.0f, 8.0f), new b2Vec2(6.0f, 8.0f), new b2Vec2(5.0f, 7.0f) };
            b2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = 4;
            chainDef.isLoop = true;

            b2CreateChain(groundId, chainDef);
        }

        // Square tiles. This shows that adjacency shapes may have non-smooth collision. Box2D has no solution
        // to this problem.
        // todo_erin try this: https://briansemrau.github.io/dealing-with-ghost-collisions/
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Polygon box = b2MakeOffsetBox(1.0f, 1.0f, new b2Vec2(4.0f, 3.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            box = b2MakeOffsetBox(1.0f, 1.0f, new b2Vec2(6.0f, 3.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            box = b2MakeOffsetBox(1.0f, 1.0f, new b2Vec2(8.0f, 3.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        // Square made from a chain loop. Collision should be smooth.
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2Vec2[] points = new b2Vec2[] { new(-1.0f, 3.0f), new(1.0f, 3.0f), new(1.0f, 5.0f), new(-1.0f, 5.0f) };
            b2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = 4;
            chainDef.isLoop = true;
            b2CreateChain(groundId, chainDef);
        }

        // Chain loop. Collision should be smooth.
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new b2Vec2(-10.0f, 4.0f);
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2Vec2[] points = new b2Vec2[10]
            {
                new b2Vec2(0.0f, 0.0f),
                new b2Vec2(6.0f, 0.0f),
                new b2Vec2(6.0f, 2.0f),
                new b2Vec2(4.0f, 1.0f),
                new b2Vec2(2.0f, 2.0f),
                new b2Vec2(0.0f, 2.0f),
                new b2Vec2(-2.0f, 2.0f),
                new b2Vec2(-4.0f, 3.0f),
                new b2Vec2(-6.0f, 2.0f),
                new b2Vec2(-6.0f, 0.0f),
            };
            b2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = 10;
            chainDef.isLoop = true;
            b2CreateChain(groundId, chainDef);
        }

        // Circle character
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new b2Vec2(-7.0f, 6.0f);
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.fixedRotation = true;
            bodyDef.enableSleep = false;

            m_circleCharacterId = b2CreateBody(m_worldId, bodyDef);

            b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.25f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;
            shapeDef.friction = 0.2f;
            b2CreateCircleShape(m_circleCharacterId, shapeDef, circle);
        }

        // Capsule character
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new b2Vec2(3.0f, 5.0f);
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.fixedRotation = true;
            bodyDef.enableSleep = false;

            m_capsuleCharacterId = b2CreateBody(m_worldId, bodyDef);

            b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, 0.25f), new b2Vec2(0.0f, 0.75f), 0.25f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;
            shapeDef.friction = 0.2f;
            b2CreateCapsuleShape(m_capsuleCharacterId, shapeDef, capsule);
        }

        // Square character
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new b2Vec2(-3.0f, 8.0f);
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.fixedRotation = true;
            bodyDef.enableSleep = false;

            m_boxCharacterId = b2CreateBody(m_worldId, bodyDef);

            b2Polygon box = b2MakeBox(0.4f, 0.4f);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;
            shapeDef.friction = 0.2f;
            b2CreatePolygonShape(m_boxCharacterId, shapeDef, box);
        }
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        Draw.g_draw.DrawString(5, m_textLine, "This tests various character collision shapes.");
        m_textLine += m_textIncrement;
        Draw.g_draw.DrawString(5, m_textLine, "Limitation: square and hexagon can snag on aligned boxes.");
        m_textLine += m_textIncrement;
    }
}