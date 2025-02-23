using Box2D.NET.Primitives;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;

namespace Box2D.NET.Samples.Samples.Benchmarks;

public class BenchmarkKinematic : Sample
{
    static int sampleKinematic = RegisterSample("Benchmark", "Kinematic", Create);

    static Sample Create(Settings settings)
    {
        return new BenchmarkKinematic(settings);
    }

    public BenchmarkKinematic(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(0.0f, 0.0f);
            Draw.g_camera.m_zoom = 150.0f;
        }

        float grid = 1.0f;

#if NDEBUG
        int span = 100;
#else
        int span = 20;
#endif

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_kinematicBody;
        bodyDef.angularVelocity = 1.0f;

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.filter.categoryBits = 1;
        shapeDef.filter.maskBits = 2;

        // defer mass properties to avoid n-squared mass computations
        shapeDef.updateBodyMass = false;

        b2BodyId bodyId = b2CreateBody(m_worldId, &bodyDef);

        for (int i = -span; i < span; ++i)
        {
            float y = i * grid;
            for (int j = -span; j < span; ++j)
            {
                float x = j * grid;
                b2Polygon square = b2MakeOffsetBox(0.5f * grid, 0.5f * grid, new b2Vec2(x, y), b2Rot_identity);
                b2CreatePolygonShape(bodyId, shapeDef, square);
            }
        }

        // All shapes have been added so I can efficiently compute the mass properties.
        b2Body_ApplyMassFromShapes(bodyId);
    }
}