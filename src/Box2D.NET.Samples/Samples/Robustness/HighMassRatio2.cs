// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Robustness;

// Big box on small boxes
public class HighMassRatio2 : Sample
{
    private static Sample Create(Settings settings)
    {
        return new HighMassRatio2(settings);
    }

    private static readonly int SampleIndex2 = SampleFactory.Shared.RegisterSample("Robustness", "HighMassRatio2", Create);

    public HighMassRatio2(Settings settings) : base(settings)
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
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            float extent = 1.0f;
            B2Polygon smallBox = b2MakeBox(0.5f * extent, 0.5f * extent);
            B2Polygon bigBox = b2MakeBox(10.0f * extent, 10.0f * extent);

            {
                bodyDef.position = new B2Vec2(-9.0f * extent, 0.5f * extent);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref smallBox);
            }

            {
                bodyDef.position = new B2Vec2(9.0f * extent, 0.5f * extent);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref smallBox);
            }

            {
                bodyDef.position = new B2Vec2(0.0f, (10.0f + 16.0f) * extent);
                B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
                b2CreatePolygonShape(bodyId, ref shapeDef, ref bigBox);
            }
        }
    }
}
