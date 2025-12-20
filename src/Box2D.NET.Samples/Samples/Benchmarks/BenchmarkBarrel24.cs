// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Benchmarks;

// This is used to compare performance with Box2D v2.4
public class BenchmarkBarrel24 : Sample
{
    private static readonly int benchmarkBarrel24 = SampleFactory.Shared.RegisterSample("Benchmark", "Barrel 2.4", Create);

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkBarrel24(context);
    }

    public BenchmarkBarrel24(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(8.0f, 53.0f);
            m_context.camera.zoom = 25.0f * 2.35f;
        }

        float groundSize = 25.0f;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            B2Polygon box = b2MakeBox(groundSize, 1.2f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            bodyDef.rotation = b2MakeRot(0.5f * B2_PI);
            bodyDef.position = new B2Vec2(groundSize, 2.0f * groundSize);
            groundId = b2CreateBody(m_worldId, bodyDef);

            box = b2MakeBox(2.0f * groundSize, 1.2f);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            bodyDef.position = new B2Vec2(-groundSize, 2.0f * groundSize);
            groundId = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        int num = 26;
        float rad = 0.5f;

        float shift = rad * 2.0f;
        float centerx = shift * num / 2.0f;
        float centery = shift / 2.0f;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.density = 1.0f;
            shapeDef.material.friction = 0.5f;

            B2Polygon cuboid = b2MakeSquare(0.5f);

            // b2Polygon top = b2MakeOffsetBox(0.8f, 0.2f, {0.0f, 0.8f}, 0.0f);
            // b2Polygon leftLeg = b2MakeOffsetBox(0.2f, 0.5f, {-0.6f, 0.5f}, 0.0f);
            // b2Polygon rightLeg = b2MakeOffsetBox(0.2f, 0.5f, {0.6f, 0.5f}, 0.0f);

#if DEBUG
            int numj = 5;
#else
		int numj = 5 * num;
#endif
            for (int i = 0; i < num; ++i)
            {
                float x = i * shift - centerx;

                for (int j = 0; j < numj; ++j)
                {
                    float y = j * shift + centery + 2.0f;

                    bodyDef.position = new B2Vec2(x, y);

                    B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);
                    b2CreatePolygonShape(bodyId, ref shapeDef, ref cuboid);
                }
            }
        }
    }
}
