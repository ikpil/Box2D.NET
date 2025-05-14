﻿// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkCompound : Sample
{
    private static readonly int SampleCompound = SampleFactory.Shared.RegisterSample("Benchmark", "Compound", Create);

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkCompound(context);
    }

    public BenchmarkCompound(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(18.0f, 115.0f);
            m_context.camera.m_zoom = 25.0f * 5.5f;
        }

        float grid = 1.0f;
#if DEBUG
        int height = 100;
        int width = 100;
#else
        int height = 200;
        int width = 200;
#endif
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();

            for (int i = 0; i < height; ++i)
            {
                float y = grid * i;
                for (int j = i; j < width; ++j)
                {
                    float x = grid * j;
                    B2Polygon square = b2MakeOffsetBox(0.5f * grid, 0.5f * grid, new B2Vec2(x, y), b2Rot_identity);
                    b2CreatePolygonShape(groundId, ref shapeDef, ref square);
                }
            }

            for (int i = 0; i < height; ++i)
            {
                float y = grid * i;
                for (int j = i; j < width; ++j)
                {
                    float x = -grid * j;
                    B2Polygon square = b2MakeOffsetBox(0.5f * grid, 0.5f * grid, new B2Vec2(x, y), b2Rot_identity);
                    b2CreatePolygonShape(groundId, ref shapeDef, ref square);
                }
            }
        }

        {
#if DEBUG
            int span = 5;
            int count = 5;
#else
            int span = 20;
            int count = 5;
#endif

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            // defer mass properties to avoid n-squared mass computations
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.updateBodyMass = false;

            for (int m = 0; m < count; ++m)
            {
                float ybody = (100.0f + m * span) * grid;

                for (int n = 0; n < count; ++n)
                {
                    float xbody = -0.5f * grid * count * span + n * span * grid;
                    bodyDef.position = new B2Vec2(xbody, ybody);
                    B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);

                    for (int i = 0; i < span; ++i)
                    {
                        float y = i * grid;
                        for (int j = 0; j < span; ++j)
                        {
                            float x = j * grid;
                            B2Polygon square = b2MakeOffsetBox(0.5f * grid, 0.5f * grid, new B2Vec2(x, y), b2Rot_identity);
                            b2CreatePolygonShape(bodyId, ref shapeDef, ref square);
                        }
                    }

                    // All shapes have been added so I can efficiently compute the mass properties.
                    b2Body_ApplyMassFromShapes(bodyId);
                }
            }
        }
    }
}