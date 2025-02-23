using System.Numerics;
using ImGuiNET;
using Box2D.NET.Primitives;
using Box2D.NET.Shared.Primitives;
using static Box2D.NET.id;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.Shared.human;
using static Box2D.NET.Shared.random;

namespace Box2D.NET.Samples.Samples.Benchmarks;

// Note: resetting the scene is non-deterministic because the world uses freelists
public class BenchmarkBarrel : Sample
{
    public enum ShapeType
    {
        e_circleShape = 0,
        e_capsuleShape,
        e_mixShape,
        e_compoundShape,
        e_humanShape,
    }

    private const int e_maxColumns = 26;
    private const int e_maxRows = 150;


    b2BodyId[] m_bodies = new b2BodyId[e_maxRows * e_maxColumns];
    Human[] m_humans = new Human[e_maxRows * e_maxColumns];
    int m_columnCount;
    int m_rowCount;

    ShapeType m_shapeType;

    private static int benchmarkBarrel = RegisterSample("Benchmark", "Barrel", Create);

    private static Sample Create(Settings settings)
    {
        return new BenchmarkBarrel(settings);
    }

    public BenchmarkBarrel(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(8.0f, 53.0f);
            Draw.g_camera.m_zoom = 25.0f * 2.35f;
        }

        settings.drawJoints = false;

        {
            float gridSize = 1.0f;

            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();

            float y = 0.0f;
            float x = -40.0f * gridSize;
            for (int i = 0; i < 81; ++i)
            {
                b2Polygon box = b2MakeOffsetBox(0.5f * gridSize, 0.5f * gridSize, new b2Vec2(x, y), b2Rot_identity);
                b2CreatePolygonShape(groundId, shapeDef, box);
                x += gridSize;
            }

            y = gridSize;
            x = -40.0f * gridSize;
            for (int i = 0; i < 100; ++i)
            {
                b2Polygon box = b2MakeOffsetBox(0.5f * gridSize, 0.5f * gridSize, new b2Vec2(x, y), b2Rot_identity);
                b2CreatePolygonShape(groundId, shapeDef, box);
                y += gridSize;
            }

            y = gridSize;
            x = 40.0f * gridSize;
            for (int i = 0; i < 100; ++i)
            {
                b2Polygon box = b2MakeOffsetBox(0.5f * gridSize, 0.5f * gridSize, new b2Vec2(x, y), b2Rot_identity);
                b2CreatePolygonShape(groundId, shapeDef, box);
                y += gridSize;
            }

            b2Segment segment = new b2Segment(new b2Vec2(-800.0f, -80.0f), new b2Vec2(800.0f, -80.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        for (int i = 0; i < e_maxRows * e_maxColumns; ++i)
        {
            m_bodies[i] = b2_nullBodyId;
        }

        //memset( m_humans, 0, sizeof( m_humans ) );
        for (int i = 0; i < m_humans.Length; ++i)
        {
            m_humans[i].Clear();
        }

        m_shapeType = ShapeType.e_compoundShape;

        CreateScene();
    }

    void CreateScene()
    {
        g_seed = 42;

        for (int i = 0; i < e_maxRows * e_maxColumns; ++i)
        {
            if (B2_IS_NON_NULL(m_bodies[i]))
            {
                b2DestroyBody(m_bodies[i]);
                m_bodies[i] = b2_nullBodyId;
            }

            if (m_humans[i].isSpawned)
            {
                DestroyHuman(m_humans[i]);
            }
        }

        m_columnCount = g_sampleDebug ? 10 : e_maxColumns;
        m_rowCount = g_sampleDebug ? 40 : e_maxRows;

        if (m_shapeType == ShapeType.e_compoundShape)
        {
            if (g_sampleDebug == false)
            {
                m_columnCount = 20;
            }
        }
        else if (m_shapeType == ShapeType.e_humanShape)
        {
            if (g_sampleDebug)
            {
                m_rowCount = 5;
                m_columnCount = 10;
            }
            else
            {
                m_rowCount = 30;
            }
        }

        float rad = 0.5f;

        float shift = 1.15f;
        float centerx = shift * m_columnCount / 2.0f;
        float centery = shift / 2.0f;

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;

        // todo eliminate this once rolling resistance is added
        if (m_shapeType == ShapeType.e_mixShape)
        {
            bodyDef.angularDamping = 0.3f;
        }

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.friction = 0.5f;

        b2Capsule capsule = new b2Capsule(new b2Vec2(0.0f, -0.25f), new b2Vec2(0.0f, 0.25f), rad);
        b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), rad);

        b2Vec2[] points = new b2Vec2[3] { new b2Vec2(-0.1f, -0.5f), new b2Vec2(0.1f, -0.5f), new b2Vec2(0.0f, 0.5f) };
        b2Hull wedgeHull = b2ComputeHull(points, 3);
        b2Polygon wedge = b2MakePolygon(wedgeHull, 0.0f);

        b2Vec2[] vertices = new b2Vec2[3];
        vertices[0] = new b2Vec2(-1.0f, 0.0f);
        vertices[1] = new b2Vec2(0.5f, 1.0f);
        vertices[2] = new b2Vec2(0.0f, 2.0f);
        b2Hull hull = b2ComputeHull(vertices, 3);
        b2Polygon left = b2MakePolygon(hull, 0.0f);

        vertices[0] = new b2Vec2(1.0f, 0.0f);
        vertices[1] = new b2Vec2(-0.5f, 1.0f);
        vertices[2] = new b2Vec2(0.0f, 2.0f);
        hull = b2ComputeHull(vertices, 3);
        b2Polygon right = b2MakePolygon(hull, 0.0f);

        // b2Polygon top = b2MakeOffsetBox(0.8f, 0.2f, {0.0f, 0.8f}, 0.0f);
        // b2Polygon leftLeg = b2MakeOffsetBox(0.2f, 0.5f, {-0.6f, 0.5f}, 0.0f);
        // b2Polygon rightLeg = b2MakeOffsetBox(0.2f, 0.5f, {0.6f, 0.5f}, 0.0f);

        float side = -0.1f;
        float extray = 0.5f;

        if (m_shapeType == ShapeType.e_compoundShape)
        {
            extray = 0.25f;
            side = 0.25f;
            shift = 2.0f;
            centerx = shift * m_columnCount / 2.0f - 1.0f;
        }
        else if (m_shapeType == ShapeType.e_humanShape)
        {
            extray = 0.5f;
            side = 0.55f;
            shift = 2.5f;
            centerx = shift * m_columnCount / 2.0f;
        }

        int index = 0;
        float yStart = m_shapeType == ShapeType.e_humanShape ? 2.0f : 100.0f;

        for (int i = 0; i < m_columnCount; ++i)
        {
            float x = i * shift - centerx;

            for (int j = 0; j < m_rowCount; ++j)
            {
                float y = j * (shift + extray) + centery + yStart;

                bodyDef.position = new b2Vec2(x + side, y);
                side = -side;

                if (m_shapeType == ShapeType.e_circleShape)
                {
                    m_bodies[index] = b2CreateBody(m_worldId, bodyDef);
                    circle.radius = RandomFloatRange(0.25f, 0.75f);
                    shapeDef.rollingResistance = 0.2f;
                    b2CreateCircleShape(m_bodies[index], shapeDef, circle);
                }
                else if (m_shapeType == ShapeType.e_capsuleShape)
                {
                    m_bodies[index] = b2CreateBody(m_worldId, bodyDef);
                    capsule.radius = RandomFloatRange(0.25f, 0.5f);
                    float length = RandomFloatRange(0.25f, 1.0f);
                    capsule.center1 = new b2Vec2(0.0f, -0.5f * length);
                    capsule.center2 = new b2Vec2(0.0f, 0.5f * length);
                    shapeDef.rollingResistance = 0.2f;
                    b2CreateCapsuleShape(m_bodies[index], shapeDef, capsule);
                }
                else if (m_shapeType == ShapeType.e_mixShape)
                {
                    m_bodies[index] = b2CreateBody(m_worldId, bodyDef);

                    int mod = index % 3;
                    if (mod == 0)
                    {
                        circle.radius = RandomFloatRange(0.25f, 0.75f);
                        b2CreateCircleShape(m_bodies[index], shapeDef, circle);
                    }
                    else if (mod == 1)
                    {
                        capsule.radius = RandomFloatRange(0.25f, 0.5f);
                        float length = RandomFloatRange(0.25f, 1.0f);
                        capsule.center1 = new b2Vec2(0.0f, -0.5f * length);
                        capsule.center2 = new b2Vec2(0.0f, 0.5f * length);
                        b2CreateCapsuleShape(m_bodies[index], shapeDef, capsule);
                    }
                    else if (mod == 2)
                    {
                        float width = RandomFloatRange(0.1f, 0.5f);
                        float height = RandomFloatRange(0.5f, 0.75f);
                        b2Polygon box = b2MakeBox(width, height);

                        // Don't put a function call into a macro.
                        float value = RandomFloatRange(-1.0f, 1.0f);
                        box.radius = 0.25f * b2MaxFloat(0.0f, value);
                        b2CreatePolygonShape(m_bodies[index], shapeDef, box);
                    }
                    else
                    {
                        wedge.radius = RandomFloatRange(0.1f, 0.25f);
                        b2CreatePolygonShape(m_bodies[index], shapeDef, wedge);
                    }
                }
                else if (m_shapeType == ShapeType.e_compoundShape)
                {
                    m_bodies[index] = b2CreateBody(m_worldId, bodyDef);

                    b2CreatePolygonShape(m_bodies[index], shapeDef, left);
                    b2CreatePolygonShape(m_bodies[index], shapeDef, right);
                    // b2CreatePolygonShape(m_bodies[index], &shapeDef, &top);
                    // b2CreatePolygonShape(m_bodies[index], &shapeDef, &leftLeg);
                    // b2CreatePolygonShape(m_bodies[index], &shapeDef, &rightLeg);
                }
                else if (m_shapeType == ShapeType.e_humanShape)
                {
                    float scale = 3.5f;
                    float jointFriction = 0.05f;
                    float jointHertz = 5.0f;
                    float jointDamping = 0.5f;
                    CreateHuman(m_humans[index], m_worldId, bodyDef.position, scale, jointFriction, jointHertz, jointDamping, index + 1, null, false);
                }

                index += 1;
            }
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 80.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(220.0f, height));
        ImGui.Begin("Benchmark: Barrel", ref open, ImGuiWindowFlags.NoResize);

        bool changed = false;
        string[] shapeTypes = ["Circle", "Capsule", "Mix", "Compound", "Human"];

        int shapeType = (int)m_shapeType;
        changed = changed || ImGui.Combo("Shape", ref shapeType, shapeTypes, shapeTypes.Length);
        m_shapeType = (ShapeType)shapeType;

        changed = changed || ImGui.Button("Reset Scene");

        if (changed)
        {
            CreateScene();
        }

        ImGui.End();
    }
}