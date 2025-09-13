// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.Shared.RandomSupports;

namespace Box2D.NET.Samples.Samples.Shapes;

public class Wind : Sample
{
    private static readonly int SampleWind = SampleFactory.Shared.RegisterSample("Shapes", "Wind", Create);

    private const int m_maxCount = 60;

    private ShapeType m_shapeType;
    private B2Vec2 m_wind;
    private float m_drag;
    private float m_lift;
    private B2Vec2 m_noise;
    private B2BodyId m_groundId;
    private B2BodyId[] m_bodyIds = new B2BodyId[m_maxCount];
    private int m_count;

    private B2Vec2 m_current_wind;

    public enum ShapeType
    {
        e_circleShape = 0,
        e_capsuleShape,
        e_boxShape
    };

    private static Sample Create(SampleContext context)
    {
        return new Wind(context);
    }

    public Wind(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(0.0f, 1.0f);
            m_context.camera.m_zoom = 2.0f;
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            m_groundId = b2CreateBody(m_worldId, ref bodyDef);
        }

        m_shapeType = ShapeType.e_capsuleShape;
        m_wind = new B2Vec2(6.0f, 0.0f);
        m_drag = 1.0f;
        m_lift = 0.75f;
        m_count = 10;
        m_noise = new B2Vec2(0.0f, 0.0f);

        CreateScene();
    }

    private void CreateScene()
    {
        for (int i = 0; i < m_maxCount; ++i)
        {
            if (B2_IS_NON_NULL(m_bodyIds[i]))
            {
                b2DestroyBody(m_bodyIds[i]);
                m_bodyIds[i] = b2_nullBodyId;
            }
        }

        float radius = 0.1f;
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), radius);
        B2Capsule capsule = new B2Capsule(new B2Vec2(0.0f, -radius), new B2Vec2(0.0f, radius), 0.25f * radius);
        B2Polygon box = b2MakeBox(0.25f * radius, 1.25f * radius);

        B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
        jointDef.@base.bodyIdA = m_groundId;
        jointDef.@base.localFrameA.p = new B2Vec2(0.0f, 2.0f + radius);
        jointDef.@base.drawScale = 0.1f;
        jointDef.hertz = 0.1f;
        jointDef.dampingRatio = 0.0f;
        jointDef.enableSpring = true;

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.density = 20.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.gravityScale = 0.5f;
        bodyDef.enableSleep = false;

        for (int i = 0; i < m_count; ++i)
        {
            bodyDef.position = new B2Vec2(0.0f, 2.0f - 2.0f * radius * i);
            m_bodyIds[i] = b2CreateBody(m_worldId, ref bodyDef);

            if (m_shapeType == ShapeType.e_circleShape)
            {
                b2CreateCircleShape(m_bodyIds[i], ref shapeDef, ref circle);
            }
            else if (m_shapeType == ShapeType.e_capsuleShape)
            {
                b2CreateCapsuleShape(m_bodyIds[i], ref shapeDef, ref capsule);
            }
            else
            {
                b2CreatePolygonShape(m_bodyIds[i], ref shapeDef, ref box);
            }

            jointDef.@base.bodyIdB = m_bodyIds[i];
            jointDef.@base.localFrameB.p = new B2Vec2(0.0f, radius);
            b2CreateRevoluteJoint(m_worldId, ref jointDef);

            jointDef.@base.bodyIdA = m_bodyIds[i];
            jointDef.@base.localFrameA.p = new B2Vec2(0.0f, -radius);
        }
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 15.0f * fontSize;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.m_height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(24.0f * fontSize, height));

        ImGui.Begin("Wind", ImGuiWindowFlags.NoResize);
        ImGui.PushItemWidth(18.0f * fontSize);

        string[] shapeTypes = { "Circle", "Capsule", "Box" };
        int shapeType = (int)m_shapeType;
        if (ImGui.Combo("Shape", ref shapeType, shapeTypes, shapeTypes.Length))
        {
            m_shapeType = (ShapeType)shapeType;
            CreateScene();
        }

        Vector2 tmpWind = new Vector2(m_wind.X, m_wind.Y);
        ImGui.SliderFloat2("Wind", ref tmpWind, -20.0f, 20.0f, "%.1f");
        m_wind = new B2Vec2(tmpWind.X, tmpWind.Y);
        ImGui.SliderFloat("Drag", ref m_drag, 0.0f, 1.0f, "%.2f");
        ImGui.SliderFloat("Lift", ref m_lift, 0.0f, 4.0f, "%.2f");
        if (ImGui.SliderInt("Count", ref m_count, 1, m_maxCount, "%d"))
        {
            CreateScene();
        }

        ImGui.PopItemWidth();
        ImGui.End();
    }

    public override void Step()
    {
        if (m_context.settings.pause == false || m_context.settings.singleStep == true)
        {
            float speed = 0.0f;
            B2Vec2 direction = b2GetLengthAndNormalize(ref speed, m_wind);
            B2Vec2 wind = b2MulSV(speed, b2Add(direction, m_noise));

            for (int i = 0; i < m_count; ++i)
            {
                B2FixedArray1<B2ShapeId> tempShapeIds = new B2FixedArray1<B2ShapeId>();
                Span<B2ShapeId> shapeIds = tempShapeIds.AsSpan();
                int count = b2Body_GetShapes(m_bodyIds[i], shapeIds, 1);
                for (int j = 0; j < count; ++j)
                {
                    b2Shape_ApplyWindForce(shapeIds[j], wind, m_drag, m_lift, true);
                }
            }

            B2Vec2 rand = RandomVec2(-0.3f, 0.3f);
            m_noise = b2Lerp(m_noise, rand, 0.05f);

            m_current_wind = wind;
        }


        base.Step();
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);
        m_draw.DrawLine(b2Vec2_zero, b2MulSV(0.2f, m_current_wind), B2HexColor.b2_colorFuchsia);
    }
}