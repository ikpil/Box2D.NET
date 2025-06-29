// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Hulls;

namespace Box2D.NET.Samples.Samples.Issues;

public class BadSteiner : Sample
{
    private static readonly int SampleBadSteiner = SampleFactory.Shared.RegisterSample("Issues", "Bad Steiner", Create);

    private static Sample Create(SampleContext context)
    {
        return new BadSteiner(context);
    }

    public BadSteiner(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 1.75f);
            m_context.camera.m_zoom = 2.5f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-100.0f, 0.0f), new B2Vec2(100.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(-48.0f, 62.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();

            B2Vec2[] points =
            [
                new B2Vec2(48.7599983f, -60.5699997f),
                new B2Vec2(48.7400017f, -60.5400009f),
                new B2Vec2(48.6800003f, -60.5600014f)
            ];

            B2Hull hull = b2ComputeHull(points, 3);
            B2Polygon poly = b2MakePolygon(ref hull, 0.0f);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref poly);
        }
    }
}