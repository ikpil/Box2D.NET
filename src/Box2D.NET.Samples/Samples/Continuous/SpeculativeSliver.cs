// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;

namespace Box2D.NET.Samples.Samples.Continuous;

public class SpeculativeSliver : Sample
{
    private static readonly int sampleSpeculativeSliver = SampleFactory.Shared.RegisterSample("Continuous", "Speculative Sliver", Create);

    public static Sample Create(SampleContext context)
    {
        return new SpeculativeSliver(context);
    }

    public SpeculativeSliver(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 1.75f);
            m_camera.m_zoom = 2.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-10.0f, 0.0f), new B2Vec2(10.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 12.0f);
            bodyDef.linearVelocity = new B2Vec2(0.0f, -100.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Vec2[] points = [new B2Vec2(-2.0f, 0.0f), new B2Vec2(-1.0f, 0.0f), new B2Vec2(2.0f, 0.5f)];
            B2Hull hull = b2ComputeHull(points, 3);
            B2Polygon poly = b2MakePolygon(ref hull, 0.0f);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref poly);
        }
    }
}
