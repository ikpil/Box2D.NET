// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Hulls;

namespace Box2D.NET.Samples.Samples.Shapes;

public class EllipseShape : Sample
{
    private static readonly int SampleEllipseShape = SampleFactory.Shared.RegisterSample("Shapes", "Ellipse", Create);

    private static Sample Create(SampleContext context)
    {
        return new EllipseShape(context);
    }

    public EllipseShape(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.zoom = 25.0f * 0.55f;
            m_camera.center = new B2Vec2(2.0f, 8.0f);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(20.0f, 1.0f, new B2Vec2(0.0f, -1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            box = b2MakeOffsetBox(1.0f, 5.0f, new B2Vec2(19.0f, 5.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            box = b2MakeOffsetBox(1.0f, 5.0f, new B2Vec2(-19.0f, 5.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        B2Vec2[] points =
        [
            new B2Vec2(0.0f, -0.25f), new B2Vec2(0.0f, 0.25f), new B2Vec2(0.05f, 0.075f),
            new B2Vec2(-0.05f, 0.075f), new B2Vec2(0.05f, -0.075f), new B2Vec2(-0.05f, -0.075f),
        ];

        B2Hull diamondHull = b2ComputeHull(points, 6);
        B2Polygon poly = b2MakePolygon(ref diamondHull, 0.2f);

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.rollingResistance = 0.2f;

            float y = 2.0f;
            int xCount = 10, yCount = 10;

            for (int i = 0; i < yCount; ++i)
            {
                float x = -5.0f;
                for (int j = 0; j < xCount; ++j)
                {
                    bodyDef.position = new B2Vec2(x, y);
                    B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                    b2CreatePolygonShape(bodyId, ref shapeDef, ref poly);

                    x += 1.0f;
                }

                y += 1.0f;
            }
        }
    }
}
