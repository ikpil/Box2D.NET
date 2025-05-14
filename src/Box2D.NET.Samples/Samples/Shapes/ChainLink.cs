// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Shapes;

// Shows how to link to chain shapes together. This is a useful technique for building large game levels with smooth collision.
public class ChainLink : Sample
{
    private static readonly int SampleChainLink = SampleFactory.Shared.RegisterSample("Shapes", "Chain Link", Create);

    private static Sample Create(SampleContext context)
    {
        return new ChainLink(context);
    }

    public ChainLink(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 5.0f);
            m_context.camera.m_zoom = 25.0f * 0.5f;
        }

        B2Vec2[] points1 = new B2Vec2[]
        {
            new B2Vec2(40.0f, 1.0f), new B2Vec2(0.0f, 0.0f), new B2Vec2(-40.0f, 0.0f),
            new B2Vec2(-40.0f, -1.0f), new B2Vec2(0.0f, -1.0f), new B2Vec2(40.0f, -1.0f),
        };
        B2Vec2[] points2 = new B2Vec2[]
        {
            new B2Vec2(-40.0f, -1.0f), new B2Vec2(0.0f, -1.0f), new B2Vec2(40.0f, -1.0f),
            new B2Vec2(40.0f, 0.0f), new B2Vec2(0.0f, 0.0f), new B2Vec2(-40.0f, 0.0f),
        };

        int count1 = points1.Length;
        int count2 = points2.Length;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

        {
            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points1;
            chainDef.count = count1;
            chainDef.isLoop = false;
            b2CreateChain(groundId, ref chainDef);
        }

        {
            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points2;
            chainDef.count = count2;
            chainDef.isLoop = false;
            b2CreateChain(groundId, ref chainDef);
        }

        bodyDef.type = B2BodyType.b2_dynamicBody;
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        {
            bodyDef.position = new B2Vec2(-5.0f, 2.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
        }

        {
            bodyDef.position = new B2Vec2(0.0f, 2.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            B2Capsule capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);
            b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);
        }

        {
            bodyDef.position = new B2Vec2(5.0f, 2.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            float h = 0.5f;
            B2Polygon box = b2MakeBox(h, h);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }
    }

    public override void Step()
    {
        base.Step();
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        DrawTextLine("This shows how to link together two chain shapes");
        
    }
}