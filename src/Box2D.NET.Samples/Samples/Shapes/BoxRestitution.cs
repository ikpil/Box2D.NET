// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Shapes;

public class BoxRestitution : Sample
{
    private static int sampleBoxRestitution = SampleFactory.Shared.RegisterSample("Shapes", "Box Restitution", Create);

    private const int m_count = 10;

    private static Sample Create(SampleContext context)
    {
        return new BoxRestitution(context);
    }

    public BoxRestitution(SampleContext context)
        : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 5.0f);
            m_context.camera.zoom = 10.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            float h = 2.0f * m_count;
            B2Segment segment = new B2Segment(new B2Vec2(-h, 0.0f), new B2Vec2(h, 0.0f));
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        B2Polygon box = b2MakeBox(0.5f, 0.5f);

        {
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            shapeDef.material.restitution = 0.0f;

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            float dr = 1.0f / (m_count > 1 ? m_count - 1 : 1);
            float x = -1.0f * (m_count - 1);
            float dx = 2.0f;

            for (int i = 0; i < m_count; ++i)
            {
                string buffer = $"{shapeDef.material.restitution:F2}";

                bodyDef.position = new B2Vec2(x, 1.0f);
                bodyDef.name = buffer;
                B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

                b2CreatePolygonShape(bodyId, shapeDef, box);

                bodyDef.position = new B2Vec2(x, 4.0f);
                bodyDef.name = buffer;
                bodyId = b2CreateBody(m_worldId, bodyDef);

                b2CreatePolygonShape(bodyId, shapeDef, box);

                shapeDef.material.restitution += dr;
                x += dx;
            }
        }
    }
}