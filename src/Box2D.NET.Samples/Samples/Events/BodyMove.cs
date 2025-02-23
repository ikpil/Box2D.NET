using System.Numerics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples;
using ImGuiNET;
using static Box2D.NET.joint;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;
using static Box2D.NET.Shared.random;

namespace Box2D.NET.Samples.Samples.Events;

// This shows how to process body events.
public class BodyMove : Sample
{
    public const int e_count = 50;

    b2BodyId[] m_bodyIds = new b2BodyId[e_count];
    bool[] m_sleeping = new bool[e_count];
    int m_count;
    int m_sleepCount;
    b2Vec2 m_explosionPosition;
    float m_explosionRadius;
    float m_explosionMagnitude;

    static int sampleBodyMove = RegisterSample("Events", "Body Move", Create);

    static Sample Create(Settings settings)
    {
        return new BodyMove(settings);
    }

    public BodyMove(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(2.0f, 8.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.55f;
        }

        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.1f;

            b2Polygon box = b2MakeOffsetBox(12.0f, 0.1f, new b2Vec2(-10.0f, -0.1f), b2MakeRot(-0.15f * B2_PI));
            b2CreatePolygonShape(groundId, shapeDef, box);

            box = b2MakeOffsetBox(12.0f, 0.1f, new b2Vec2(10.0f, -0.1f), b2MakeRot(0.15f * B2_PI));
            b2CreatePolygonShape(groundId, shapeDef, box);

            shapeDef.restitution = 0.8f;

            box = b2MakeOffsetBox(0.1f, 10.0f, new b2Vec2(19.9f, 10.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            box = b2MakeOffsetBox(0.1f, 10.0f, new b2Vec2(-19.9f, 10.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);

            box = b2MakeOffsetBox(20.0f, 0.1f, new b2Vec2(0.0f, 20.1f), b2Rot_identity);
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        m_sleepCount = 0;
        m_count = 0;

        m_explosionPosition = new b2Vec2(0.0f, -5.0f);
        m_explosionRadius = 10.0f;
        m_explosionMagnitude = 10.0f;
    }

    void CreateBodies()
    {
        b2Capsule capsule = new b2Capsule(new b2Vec2(-0.25f, 0.0f), new b2Vec2(0.25f, 0.0f), 0.25f);
        b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.35f);
        b2Polygon square = b2MakeSquare(0.35f);

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        b2ShapeDef shapeDef = b2DefaultShapeDef();

        float x = -5.0f, y = 10.0f;
        for (int i = 0; i < 10 && m_count < e_count; ++i)
        {
            bodyDef.position = new b2Vec2(x, y);
            bodyDef.userData = m_bodyIds[m_count];
            m_bodyIds[m_count] = b2CreateBody(m_worldId, bodyDef);
            m_sleeping[m_count] = false;

            int remainder = m_count % 4;
            if (remainder == 0)
            {
                b2CreateCapsuleShape(m_bodyIds[m_count], shapeDef, capsule);
            }
            else if (remainder == 1)
            {
                b2CreateCircleShape(m_bodyIds[m_count], shapeDef, circle);
            }
            else if (remainder == 2)
            {
                b2CreatePolygonShape(m_bodyIds[m_count], shapeDef, square);
            }
            else
            {
                b2Polygon poly = RandomPolygon(0.75f);
                poly.radius = 0.1f;
                b2CreatePolygonShape(m_bodyIds[m_count], shapeDef, poly);
            }

            m_count += 1;
            x += 1.0f;
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 100.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Body Move", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (ImGui.Button("Explode"))
        {
            b2ExplosionDef def = b2DefaultExplosionDef();
            def.position = m_explosionPosition;
            def.radius = m_explosionRadius;
            def.falloff = 0.1f;
            def.impulsePerLength = m_explosionMagnitude;
            b2World_Explode(m_worldId, def);
        }

        ImGui.SliderFloat("Magnitude", ref m_explosionMagnitude, -20.0f, 20.0f, "%.1f");

        ImGui.End();
    }

    public override void Step(Settings settings)
    {
        if (settings.pause == false && (m_stepCount & 15) == 15 && m_count < e_count)
        {
            CreateBodies();
        }

        base.Step(settings);

        // Process body events
        b2BodyEvents events = b2World_GetBodyEvents(m_worldId);
        for (int i = 0; i < events.moveCount; ++i)
        {
            // draw the transform of every body that moved (not sleeping)
            b2BodyMoveEvent @event = events.moveEvents[i];
            Draw.g_draw.DrawTransform(@event.transform);

            // this shows a somewhat contrived way to track body sleeping
            b2BodyId bodyId = (b2BodyId)@event.userData; // todo: @ikpil check struct casting
            ptrdiff_t diff = bodyId - m_bodyIds;
            bool* sleeping = m_sleeping + diff;

            if (@event.fellAsleep)
            {
                *sleeping = true;
                m_sleepCount += 1;
            }
            else
            {
                if (*sleeping)
                {
                    *sleeping = false;
                    m_sleepCount -= 1;
                }
            }
        }

        Draw.g_draw.DrawCircle(m_explosionPosition, m_explosionRadius, b2HexColor.b2_colorAzure);

        Draw.g_draw.DrawString(5, m_textLine, "sleep count: %d", m_sleepCount);
        m_textLine += m_textIncrement;
    }
}