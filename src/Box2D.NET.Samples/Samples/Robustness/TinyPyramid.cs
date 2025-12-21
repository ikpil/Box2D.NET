// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Robustness;

public class TinyPyramid : Sample
{
    private static readonly int SampleTinyPyramid = SampleFactory.Shared.RegisterSample("Robustness", "Tiny Pyramid", Create);

    private float m_extent;

    private static Sample Create(SampleContext context)
    {
        return new TinyPyramid(context);
    }

    public TinyPyramid(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 0.8f);
            m_camera.zoom = 1.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(5.0f, 1.0f, new B2Vec2(0.0f, -1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        {
            m_extent = 0.025f;
            int baseCount = 30;

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            B2ShapeDef shapeDef = b2DefaultShapeDef();

            B2Polygon box = b2MakeSquare(m_extent);

            for (int i = 0; i < baseCount; ++i)
            {
                float y = (2.0f * i + 1.0f) * m_extent;

                for (int j = i; j < baseCount; ++j)
                {
                    float x = (i + 1.0f) * m_extent + 2.0f * (j - i) * m_extent - baseCount * m_extent;
                    bodyDef.position = new B2Vec2(x, y);

                    B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                    b2CreatePolygonShape(bodyId, shapeDef, box);
                }
            }
        }
    }

    public override void Draw()
    {
        base.Draw();

        DrawTextLine($"{200.0f * m_extent:F1}cm squares");
    }
}
