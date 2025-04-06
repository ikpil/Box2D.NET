﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Bodies;

/// This is a test of typical character collision scenarios. This does not
/// show how you should implement a character in your application.
/// Instead this is used to test smooth collision on chain shapes.
public class Character : Sample
{
    private static readonly int SampleCharacter = SampleFactory.Shared.RegisterSample("Bodies", "Character", Create);

    private B2BodyId m_circleCharacterId;
    private B2BodyId m_capsuleCharacterId;
    private B2BodyId m_boxCharacterId;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new Character(ctx, settings);
    }

    public Character(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(-2.0f, 7.0f);
            m_context.camera.m_zoom = 25.0f * 0.4f;
        }

        // Ground body
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-20.0f, 0.0f), new B2Vec2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        // Collinear edges with no adjacency information.
        // This shows the problematic case where a box shape can hit
        // an internal vertex.
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment1 = new B2Segment(new B2Vec2(-8.0f, 1.0f), new B2Vec2(-6.0f, 1.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment1);

            B2Segment segment2 = new B2Segment(new B2Vec2(-6.0f, 1.0f), new B2Vec2(-4.0f, 1.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment2);

            B2Segment segment3 = new B2Segment(new B2Vec2(-4.0f, 1.0f), new B2Vec2(-2.0f, 1.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment3);
        }

        // Chain shape
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.rotation = b2MakeRot(0.25f * B2_PI);
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Vec2[] points = new B2Vec2[] { new(8.0f, 7.0f), new(7.0f, 8.0f), new B2Vec2(6.0f, 8.0f), new B2Vec2(5.0f, 7.0f) };
            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = 4;
            chainDef.isLoop = true;

            b2CreateChain(groundId, ref chainDef);
        }

        // Square tiles. This shows that adjacency shapes may have non-smooth collision. Box2D has no solution
        // to this problem.
        // todo_erin try this: https://briansemrau.github.io/dealing-with-ghost-collisions/
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(1.0f, 1.0f, new B2Vec2(4.0f, 3.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            box = b2MakeOffsetBox(1.0f, 1.0f, new B2Vec2(6.0f, 3.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            box = b2MakeOffsetBox(1.0f, 1.0f, new B2Vec2(8.0f, 3.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        // Square made from a chain loop. Collision should be smooth.
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Vec2[] points = new B2Vec2[] { new(-1.0f, 3.0f), new(1.0f, 3.0f), new(1.0f, 5.0f), new(-1.0f, 5.0f) };
            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = 4;
            chainDef.isLoop = true;
            b2CreateChain(groundId, ref chainDef);
        }

        // Chain loop. Collision should be smooth.
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(-10.0f, 4.0f);
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2Vec2[] points = new B2Vec2[10]
            {
                new B2Vec2(0.0f, 0.0f),
                new B2Vec2(6.0f, 0.0f),
                new B2Vec2(6.0f, 2.0f),
                new B2Vec2(4.0f, 1.0f),
                new B2Vec2(2.0f, 2.0f),
                new B2Vec2(0.0f, 2.0f),
                new B2Vec2(-2.0f, 2.0f),
                new B2Vec2(-4.0f, 3.0f),
                new B2Vec2(-6.0f, 2.0f),
                new B2Vec2(-6.0f, 0.0f),
            };
            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.count = 10;
            chainDef.isLoop = true;
            b2CreateChain(groundId, ref chainDef);
        }

        // Circle character
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(-7.0f, 6.0f);
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.fixedRotation = true;
            bodyDef.enableSleep = false;

            m_circleCharacterId = b2CreateBody(m_worldId, ref bodyDef);

            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.25f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;
            shapeDef.material.friction = 0.2f;
            b2CreateCircleShape(m_circleCharacterId, ref shapeDef, ref circle);
        }

        // Capsule character
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(3.0f, 5.0f);
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.fixedRotation = true;
            bodyDef.enableSleep = false;

            m_capsuleCharacterId = b2CreateBody(m_worldId, ref bodyDef);

            B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, 0.25f), new B2Vec2(0.0f, 0.75f), 0.25f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;
            shapeDef.material.friction = 0.2f;
            b2CreateCapsuleShape(m_capsuleCharacterId, ref shapeDef, ref capsule);
        }

        // Square character
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(-3.0f, 8.0f);
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.fixedRotation = true;
            bodyDef.enableSleep = false;

            m_boxCharacterId = b2CreateBody(m_worldId, ref bodyDef);

            B2Polygon box = b2MakeBox(0.4f, 0.4f);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 20.0f;
            shapeDef.material.friction = 0.2f;
            b2CreatePolygonShape(m_boxCharacterId, ref shapeDef, ref box);
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);
        
        m_context.draw.DrawString(5, m_textLine, "This tests various character collision shapes.");
        m_textLine += m_textIncrement;
        m_context.draw.DrawString(5, m_textLine, "Limitation: square and hexagon can snag on aligned boxes.");
        m_textLine += m_textIncrement;
    }
}