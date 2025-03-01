// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Robustness;

// Big box on small triangles
public class HighMassRatio3 : Sample
{
    private static Sample Create(Settings settings)
    {
        return new HighMassRatio3(settings);
    }

    private static readonly int SampleIndex3 = SampleFactory.Shared.RegisterSample("Robustness", "HighMassRatio3", Create);

    public HighMassRatio3(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            B2.g_camera.m_center = new B2Vec2(0.0f, 16.5f);
            B2.g_camera.m_zoom = 25.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(50.0f, 1.0f, new B2Vec2(0.0f, -1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            float extent = 1.0f;
            B2Vec2[] points = new B2Vec2[3] { new B2Vec2(-0.5f * extent, 0.0f), new B2Vec2(0.5f * extent, 0.0f), new B2Vec2(0.0f, 1.0f * extent) };
            B2Hull hull = b2ComputeHull(points, 3);
            B2Polygon smallTriangle = b2MakePolygon(hull, 0.0f);
            B2Polygon bigBox = b2MakeBox(10.0f * extent, 10.0f * extent);

            {
                bodyDef.position = new B2Vec2(-9.0f * extent, 0.5f * extent);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, shapeDef, smallTriangle);
            }

            {
                bodyDef.position = new B2Vec2(9.0f * extent, 0.5f * extent);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, shapeDef, smallTriangle);
            }

            {
                bodyDef.position = new B2Vec2(0.0f, (10.0f + 4.0f) * extent);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, shapeDef, bigBox);
            }
        }
    }
}
