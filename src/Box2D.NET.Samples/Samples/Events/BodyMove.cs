// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Samples.Extensions;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.Shared.RandomSupports;

namespace Box2D.NET.Samples.Samples.Events;

// This shows how to process body events.
public class BodyMove : Sample
{
    private static readonly int SampleBodyMove = SampleFactory.Shared.RegisterSample("Events", "Body Move", Create);

    public const int e_count = 50;

    private B2BodyId[] m_bodyIds = new B2BodyId[e_count];
    private bool[] m_sleeping = new bool[e_count];
    private int m_count;
    private int m_sleepCount;
    private B2Vec2 m_explosionPosition;
    private float m_explosionRadius;
    private float m_explosionMagnitude;


    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new BodyMove(ctx, settings);
    }

    public BodyMove(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.g_camera.m_center = new B2Vec2(2.0f, 8.0f);
            m_context.g_camera.m_zoom = 25.0f * 0.55f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.1f;

            B2Polygon box = b2MakeOffsetBox(12.0f, 0.1f, new B2Vec2(-10.0f, -0.1f), b2MakeRot(-0.15f * B2_PI));
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            box = b2MakeOffsetBox(12.0f, 0.1f, new B2Vec2(10.0f, -0.1f), b2MakeRot(0.15f * B2_PI));
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            shapeDef.restitution = 0.8f;

            box = b2MakeOffsetBox(0.1f, 10.0f, new B2Vec2(19.9f, 10.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            box = b2MakeOffsetBox(0.1f, 10.0f, new B2Vec2(-19.9f, 10.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);

            box = b2MakeOffsetBox(20.0f, 0.1f, new B2Vec2(0.0f, 20.1f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        m_sleepCount = 0;
        m_count = 0;

        m_explosionPosition = new B2Vec2(0.0f, -5.0f);
        m_explosionRadius = 10.0f;
        m_explosionMagnitude = 10.0f;
    }

    void CreateBodies()
    {
        B2Capsule capsule = new B2Capsule(new B2Vec2(-0.25f, 0.0f), new B2Vec2(0.25f, 0.0f), 0.25f);
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.35f);
        B2Polygon square = b2MakeSquare(0.35f);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        float x = -5.0f, y = 10.0f;
        for (int i = 0; i < 10 && m_count < e_count; ++i)
        {
            bodyDef.position = new B2Vec2(x, y);
            bodyDef.userData = BodyUserData.Create(m_count);
            m_bodyIds[m_count] = b2CreateBody(m_worldId, ref bodyDef);
            m_sleeping[m_count] = false;

            int remainder = m_count % 4;
            if (remainder == 0)
            {
                b2CreateCapsuleShape(m_bodyIds[m_count], ref shapeDef, ref capsule);
            }
            else if (remainder == 1)
            {
                b2CreateCircleShape(m_bodyIds[m_count], ref shapeDef, ref circle);
            }
            else if (remainder == 2)
            {
                b2CreatePolygonShape(m_bodyIds[m_count], ref shapeDef, ref square);
            }
            else
            {
                B2Polygon poly = RandomPolygon(0.75f);
                poly.radius = 0.1f;
                b2CreatePolygonShape(m_bodyIds[m_count], ref shapeDef, ref poly);
            }

            m_count += 1;
            x += 1.0f;
        }
    }

    public override void UpdateUI()
    {
        bool open = true;
        float height = 100.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Body Move", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (ImGui.Button("Explode"))
        {
            B2ExplosionDef def = b2DefaultExplosionDef();
            def.position = m_explosionPosition;
            def.radius = m_explosionRadius;
            def.falloff = 0.1f;
            def.impulsePerLength = m_explosionMagnitude;
            b2World_Explode(m_worldId, ref def);
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
        B2BodyEvents events = b2World_GetBodyEvents(m_worldId);
        for (int i = 0; i < events.moveCount; ++i)
        {
            // draw the transform of every body that moved (not sleeping)
            B2BodyMoveEvent @event = events.moveEvents[i];
            m_context.g_draw.DrawTransform(@event.transform);

            // this shows a somewhat contrived way to track body sleeping
            //B2BodyId bodyId = (B2BodyId)@event.userData; // todo: @ikpil check struct casting
            var diff = (BodyUserData<int>)@event.userData;
            //ptrdiff_t diff = bodyId - m_bodyIds;
            ref bool sleeping = ref m_sleeping[diff.Value];

            if (@event.fellAsleep)
            {
                sleeping = true;
                m_sleepCount += 1;
            }
            else
            {
                if (sleeping)
                {
                    sleeping = false;
                    m_sleepCount -= 1;
                }
            }
        }

        m_context.g_draw.DrawCircle(m_explosionPosition, m_explosionRadius, B2HexColor.b2_colorAzure);

        m_context.g_draw.DrawString(5, m_textLine, $"sleep count: {m_sleepCount}");
        m_textLine += m_textIncrement;
    }
}