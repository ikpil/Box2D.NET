// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.Shared.random;

namespace Box2D.NET.Samples.Samples.Shapes;

public class RoundedShapes : Sample
{
    static int sampleRoundedShapes = RegisterSample("Shapes", "Rounded", Create);

    static Sample Create(Settings settings)
    {
        return new RoundedShapes(settings);
    }

    public RoundedShapes(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_zoom = 25.0f * 0.55f;
            Draw.g_camera.m_center = new B2Vec2(2.0f, 8.0f);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(20.0f, 1.0f, new B2Vec2(0.0f, -1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            box = b2MakeOffsetBox(1.0f, 5.0f, new B2Vec2(19.0f, 5.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            box = b2MakeOffsetBox(1.0f, 5.0f, new B2Vec2(-19.0f, 5.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        {
            // b2Capsule capsule = {{-0.25f, 0.0f}, {0.25f, 0.0f}, 0.25f};
            // b2Circle circle = {{0.0f, 0.0f}, 0.35f};
            // b2Polygon square = b2MakeSquare(0.35f);

            // b2Vec2 points[3] = {{-0.1f, -0.5f}, {0.1f, -0.5f}, {0.0f, 0.5f}};
            // b2Hull wedgeHull = b2ComputeHull(points, 3);
            // b2Polygon wedge = b2MakePolygon(wedgeHull, 0.0f);

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            float y = 2.0f;
            int xcount = 10, ycount = 10;

            for (int i = 0; i < ycount; ++i)
            {
                float x = -5.0f;
                for (int j = 0; j < xcount; ++j)
                {
                    bodyDef.position = new B2Vec2(x, y);
                    B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

                    B2Polygon poly = RandomPolygon(0.5f);
                    poly.radius = RandomFloatRange(0.05f, 0.25f);
                    b2CreatePolygonShape(bodyId, shapeDef, poly);

                    x += 1.0f;
                }

                y += 1.0f;
            }
        }
    }
}
