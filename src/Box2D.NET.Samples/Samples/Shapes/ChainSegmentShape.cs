// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Box2D.NET.Samples.Samples.Shapes;

public class ChainSegmentShape : Sample
{
    private static readonly int sampleChainSegmentShape = SampleFactory.Shared.RegisterSample("Shapes", "Chain Segment", Create);

    public enum ShapeType
    {
        e_circleShape = 0,
        e_capsuleShape,
        e_boxShape
    }

    public const int m_segmentCount = 32;
    public const int m_pointCount = m_segmentCount + 3;
    public B2BodyId m_bodyId;
    public ShapeType m_shapeType;
    public B2ShapeId[] m_segmentShapes = new B2ShapeId[m_segmentCount];
    public B2Vec2[] m_points = new B2Vec2[m_pointCount];
    public int m_mutateIndex;


    public static Sample Create(SampleContext context)
    {
        return new ChainSegmentShape(context);
    }

    public ChainSegmentShape(SampleContext context)
        : base(context)
    {
        if (m_context.restart == false)
        {
            m_context.camera.center = new B2Vec2(0.0f, 0.0f);
            m_context.camera.zoom = 25.0f * 1.0f;
        }

        m_bodyId = b2_nullBodyId;
        m_shapeType = ShapeType.e_circleShape;
        m_mutateIndex = 0;

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            // Walk right-to-left so the right-perpendicular normal of (point2 - point1) points up.
            for (int i = 0; i < m_pointCount; ++i)
            {
                float x = 25.0f - 50.0f * i / (m_pointCount - 1);
                float y = 1.5f * MathF.Sin(0.18f * x);
                m_points[i] = new B2Vec2(x, y);
            }

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            for (int i = 0; i < m_segmentCount; ++i)
            {
                B2ChainSegment chainSegment;
                chainSegment.ghost1 = m_points[i];
                chainSegment.segment.point1 = m_points[i + 1];
                chainSegment.segment.point2 = m_points[i + 2];
                chainSegment.ghost2 = m_points[i + 3];
                chainSegment.chainId = -1;
                m_segmentShapes[i] = b2CreateChainSegmentShape(groundId, shapeDef, chainSegment);
            }
        }

        Launch();
    }

    public void Launch()
    {
        if (B2_IS_NON_NULL(m_bodyId))
        {
            b2DestroyBody(m_bodyId);
        }

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Vec2(-18.0f, 5.0f);
        m_bodyId = b2CreateBody(m_worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        if (m_shapeType == ShapeType.e_circleShape)
        {
            B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.25f);
            b2CreateCircleShape(m_bodyId, shapeDef, circle);
        }
        else if (m_shapeType == ShapeType.e_capsuleShape)
        {
            B2Capsule capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);
            b2CreateCapsuleShape(m_bodyId, shapeDef, capsule);
        }
        else
        {
            B2Polygon box = b2MakeSquare(0.5f);
            b2CreatePolygonShape(m_bodyId, shapeDef, box);
        }
    }

    public void Mutate()
    {
        // Get an index in [1,pointCount - 2]
        // index 0 and pointCount-1 are ghost vertices and are not mutated
        int index = m_mutateIndex + 1;
        B2_ASSERT(1 <= index && index <= m_pointCount - 2);

        m_mutateIndex += 1;
        if (m_mutateIndex == m_segmentCount)
        {
            m_mutateIndex = 0;
        }

        m_points[index].Y += 0.25f;

        B2ChainSegment cs;
        cs.ghost1 = m_points[index - 1];
        cs.segment.point1 = m_points[index];
        cs.segment.point2 = m_points[index + 1];
        cs.ghost2 = m_points[index + 2];
        cs.chainId = -1;

        B2_ASSERT(0 <= index - 1 && index - 1 < m_segmentCount);
        b2Shape_SetChainSegment(m_segmentShapes[index - 1], cs);

        if (index - 1 > 0)
        {
            B2_ASSERT(0 <= index - 2);
            B2ChainSegment cs2;
            cs2.ghost1 = m_points[index - 2];
            cs2.segment.point1 = m_points[index - 1];
            cs2.segment.point2 = m_points[index];
            cs2.ghost2 = m_points[index + 1];
            cs2.chainId = -1;
            B2_ASSERT(0 <= index - 2 && index - 2 < m_segmentCount);
            b2Shape_SetChainSegment(m_segmentShapes[index - 2], cs2);
        }

        if (index + 1 < m_pointCount - 2)
        {
            B2_ASSERT(index + 3 < m_pointCount);
            B2ChainSegment cs3;
            cs3.ghost1 = m_points[index];
            cs3.segment.point1 = m_points[index + 1];
            cs3.segment.point2 = m_points[index + 2];
            cs3.ghost2 = m_points[index + 3];
            cs3.chainId = -1;
            B2_ASSERT(0 <= index && index < m_segmentCount);
            b2Shape_SetChainSegment(m_segmentShapes[index], cs3);
        }
    }

    public override void UpdateGui()
    {
        float fontSize = ImGui.GetFontSize();
        float height = 130.0f;
        ImGui.SetNextWindowPos(new Vector2(0.5f * fontSize, m_camera.height - height - 2.0f * fontSize), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(240.0f, height));

        ImGui.Begin("Chain Segment Shape", ImGuiWindowFlags.NoResize);

        string[] shapeTypes = { "Circle", "Capsule", "Box" };
        int shapeType = (int)m_shapeType;
        if (ImGui.Combo("Shape", ref shapeType, shapeTypes, shapeTypes.Length))
        {
            m_shapeType = (ShapeType)shapeType;
            Launch();
        }

        if (ImGui.Button("Launch"))
        {
            Launch();
        }

        if (ImGui.Button("Mutate"))
        {
            Mutate();
        }

        ImGui.End();
    }

    public override void Step()
    {
        base.Step();
    }

}
