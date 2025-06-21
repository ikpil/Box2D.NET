// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Robustness;

// Pyramid with heavy box on top
public class HighMassRatio1 : Sample
{
    private static readonly int SampleHighMassRatio1 = SampleFactory.Shared.RegisterSample("Robustness", "HighMassRatio1", Create);

    private static Sample Create(SampleContext context)
    {
        return new HighMassRatio1(context);
    }


    public HighMassRatio1(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(3.0f, 14.0f);
            m_camera.m_zoom = 25.0f;
        }

        float extent = 1.0f;

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
            B2Polygon box = b2MakeBox(extent, extent);
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            for (int j = 0; j < 3; ++j)
            {
                int count = 10;
                float offset = -20.0f * extent + 2.0f * (count + 1.0f) * extent * j;
                float y = extent;
                while (count > 0)
                {
                    for (int i = 0; i < count; ++i)
                    {
                        float coeff = i - 0.5f * count;

                        float yy = count == 1 ? y + 2.0f : y;
                        bodyDef.position = new B2Vec2(2.0f * coeff * extent + offset, yy);
                        B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

                        shapeDef.density = count == 1 ? (j + 1.0f) * 100.0f : 1.0f;
                        b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
                    }

                    --count;
                    y += 2.0f * extent;
                }
            }
        }
    }
}