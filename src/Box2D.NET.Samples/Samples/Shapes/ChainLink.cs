// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Shapes;

// Shows how to link to chain shapes together. This is a useful technique for building large game levels with smooth collision.
public class ChainLink : Sample
{
    static int sampleChainLink = RegisterSample("Shapes", "Chain Link", Create);

    static Sample Create(Settings settings)
    {
        return new ChainLink(settings);
    }

    public ChainLink(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new B2Vec2(0.0f, 5.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.5f;
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
        B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        {
            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points1;
            chainDef.count = count1;
            chainDef.isLoop = false;
            b2CreateChain(groundId, chainDef);
        }

        {
            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points2;
            chainDef.count = count2;
            chainDef.isLoop = false;
            b2CreateChain(groundId, chainDef);
        }

        bodyDef.type = B2BodyType.b2_dynamicBody;
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        {
            bodyDef.position = new B2Vec2(-5.0f, 2.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            b2CreateCircleShape(bodyId, shapeDef, circle);
        }

        {
            bodyDef.position = new B2Vec2(0.0f, 2.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            B2Capsule capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);
            b2CreateCapsuleShape(bodyId, shapeDef, capsule);
        }

        {
            bodyDef.position = new B2Vec2(5.0f, 2.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
            float h = 0.5f;
            B2Polygon box = b2MakeBox(h, h);
            b2CreatePolygonShape(bodyId, shapeDef, box);
        }
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        Draw.g_draw.DrawString(5, m_textLine, "This shows how to link together two chain shapes");
        m_textLine += m_textIncrement;
    }
}
