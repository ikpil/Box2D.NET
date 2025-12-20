// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples.Samples.Continuous;

public class BounceHouse : Sample
{
    private static readonly int SampleBounceHouse = SampleFactory.Shared.RegisterSample("Continuous", "Bounce House", Create);

    enum ShapeType
    {
        e_circleShape = 0,
        e_capsuleShape,
        e_boxShape
    };


    private HitEvent[] m_hitEvents = new HitEvent[4];
    private B2BodyId m_bodyId;
    private ShapeType m_shapeType;
    private bool m_enableHitEvents;

    private static Sample Create(SampleContext context)
    {
        return new BounceHouse(context);
    }

    public BounceHouse(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new B2Vec2(0.0f, 0.0f);
            m_camera.zoom = 25.0f * 0.45f;
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        {
            B2Segment segment = new B2Segment(new B2Vec2(-10.0f, -10.0f), new B2Vec2(10.0f, -10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2Segment segment = new B2Segment(new B2Vec2(10.0f, -10.0f), new B2Vec2(10.0f, 10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2Segment segment = new B2Segment(new B2Vec2(10.0f, 10.0f), new B2Vec2(-10.0f, 10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2Segment segment = new B2Segment(new B2Vec2(-10.0f, 10.0f), new B2Vec2(-10.0f, -10.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        m_shapeType = ShapeType.e_circleShape;
        m_bodyId = b2_nullBodyId;
        m_enableHitEvents = true;

        //memset( m_hitEvents, 0, sizeof( m_hitEvents ) );
        for (int i = 0; i < m_hitEvents.Length; ++i)
        {
            m_hitEvents[i].Clear();
        }

        Launch();
    }

    void Launch()
    {
        if (B2_IS_NON_NULL(m_bodyId))
        {
            b2DestroyBody(m_bodyId);
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.linearVelocity = new B2Vec2(10.0f, 20.0f);
        bodyDef.position = new B2Vec2(0.0f, 0.0f);
        bodyDef.gravityScale = 0.0f;
        bodyDef.isBullet = true;

        // Circle shapes centered on the body can spin fast without risk of tunnelling.
        bodyDef.allowFastRotation = m_shapeType == ShapeType.e_circleShape;

        m_bodyId = b2CreateBody(m_worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.restitution = 1.0f;
        shapeDef.material.friction = 0.0f;
        shapeDef.enableHitEvents = m_enableHitEvents;

        if (m_shapeType == ShapeType.e_circleShape)
        {
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            b2CreateCircleShape(m_bodyId, shapeDef, circle);
        }
        else if (m_shapeType == ShapeType.e_capsuleShape)
        {
            B2Capsule capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);
            b2CreateCapsuleShape(m_bodyId, shapeDef, capsule);
        }
        else
        {
            float h = 0.1f;
            B2Polygon box = b2MakeBox(20.0f * h, h);
            b2CreatePolygonShape(m_bodyId, shapeDef, box);
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float fontSize = ImGui.GetFontSize();
        float height = 100.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Bounce House", ImGuiWindowFlags.NoResize);

        string[] shapeTypes = { "Circle", "Capsule", "Box" };
        int shapeType = (int)m_shapeType;
        if (ImGui.Combo("Shape", ref shapeType, shapeTypes, shapeTypes.Length))
        {
            m_shapeType = (ShapeType)shapeType;
            Launch();
        }

        if (ImGui.Checkbox("hit events", ref m_enableHitEvents))
        {
            b2Body_EnableHitEvents(m_bodyId, m_enableHitEvents);
        }

        ImGui.End();
    }

    public override void Step()
    {
        base.Step();

        B2ContactEvents events = b2World_GetContactEvents(m_worldId);
        for (int i = 0; i < events.hitCount; ++i)
        {
            ref B2ContactHitEvent @event = ref events.hitEvents[i];

            ref HitEvent e = ref m_hitEvents[0];
            for (int j = 1; j < 4; ++j)
            {
                if (m_hitEvents[j].stepIndex < e.stepIndex)
                {
                    e = ref m_hitEvents[j];
                }
            }

            e.point = @event.point;
            e.speed = @event.approachSpeed;
            e.stepIndex = m_stepCount;
        }


        if (m_stepCount == 1000)
        {
            m_stepCount += 0;
        }
    }

    public override void Draw()
    {
        base.Draw();

        for (int i = 0; i < 4; ++i)
        {
            ref HitEvent e = ref m_hitEvents[i];
            if (e.stepIndex > 0 && m_stepCount <= e.stepIndex + 30)
            {
                DrawCircle(m_draw, e.point, 0.1f, B2HexColor.b2_colorOrangeRed);
                DrawWorldString(m_draw, m_camera, e.point, B2HexColor.b2_colorWhite, $"{e.speed:F1}");
            }
        }
    }
}