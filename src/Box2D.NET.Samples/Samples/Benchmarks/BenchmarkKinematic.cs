// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkKinematic : Sample
{
    private static readonly int SampleKinematic = SampleFactory.Shared.RegisterSample("Benchmark", "Kinematic", Create);

    private static Sample Create(SampleContext context)
    {
        return new BenchmarkKinematic(context);
    }

    public BenchmarkKinematic(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 0.0f);
            m_camera.zoom = 150.0f;
        }

        float grid = 1.0f;

#if DEBUG
        int span = 20;
#else
        int span = 100;
#endif

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_kinematicBody;
        bodyDef.angularVelocity = 1.0f;

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.filter.categoryBits = 1;
        shapeDef.filter.maskBits = 2;

        // defer mass properties to avoid n-squared mass computations
        shapeDef.updateBodyMass = false;

        B2BodyId bodyId = b2CreateBody(m_worldId, bodyDef);

        for (int i = -span; i < span; ++i)
        {
            float y = i * grid;
            for (int j = -span; j < span; ++j)
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
