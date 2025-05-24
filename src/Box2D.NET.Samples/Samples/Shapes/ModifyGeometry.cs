// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples.Shapes;

// This sample shows how to modify the geometry on an existing shape. This is only supported on
// dynamic and kinematic shapes because static shapes don't look for new collisions.
public class ModifyGeometry : Sample
{
    private static readonly int SampleModifyGeometry = SampleFactory.Shared.RegisterSample("Shapes", "Modify Geometry", Create);
    
    private B2ShapeId m_shapeId;
    private B2ShapeType m_shapeType;
    private object m_shape;
    private float m_scale;

    private B2ShapeUnion m_us;

    private static Sample Create(SampleContext context)
    {
        return new ModifyGeometry(context);
    }

    private void SetCircle(B2Circle circle)
    {
    }

    public ModifyGeometry(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_context.camera.m_zoom = 25.0f * 0.25f;
            m_context.camera.m_center = new B2Vec2(0.0f, 5.0f);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeOffsetBox(10.0f, 1.0f, new B2Vec2(0.0f, -1.0f), b2Rot_identity);
            b2CreatePolygonShape(groundId, ref shapeDef, ref box);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Vec2(0.0f, 4.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Polygon box = b2MakeBox(1.0f, 1.0f);
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
        }

        {
            m_shapeType = B2ShapeType.b2_circleShape;
            m_scale = 1.0f;
            var circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
            m_us.circle = circle; // todo : @ikpil, fix it!!
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_kinematicBody;
            bodyDef.position = new B2Vec2(0.0f, 1.0f);
            B2BodyId bodyId = b2CreateBody(m_worldId, ref bodyDef);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            m_shapeId = b2CreateCircleShape(bodyId, ref shapeDef, ref circle); // todo : @ikpil, fix it!!
        }
    }

    void UpdateShape()
    {
        switch (m_shapeType)
        {
            case B2ShapeType.b2_circleShape:
                m_us.circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f * m_scale);
                b2Shape_SetCircle(m_shapeId, ref m_us.circle);
                break;

            case B2ShapeType.b2_capsuleShape:
                m_us.capsule = new B2Capsule(new B2Vec2(-0.5f * m_scale, 0.0f), new B2Vec2(0.0f, 0.5f * m_scale), 0.5f * m_scale);
                b2Shape_SetCapsule(m_shapeId, ref m_us.capsule);
                break;

            case B2ShapeType.b2_segmentShape:
                m_us.segment = new B2Segment(new B2Vec2(-0.5f * m_scale, 0.0f), new B2Vec2(0.75f * m_scale, 0.0f));
                b2Shape_SetSegment(m_shapeId, ref m_us.segment);
                break;

            case B2ShapeType.b2_polygonShape:
                m_us.polygon = b2MakeBox(0.5f * m_scale, 0.75f * m_scale);
                b2Shape_SetPolygon(m_shapeId, ref m_us.polygon);
                break;

            default:
                B2_ASSERT(false);
                break;
        }

        B2BodyId bodyId = b2Shape_GetBody(m_shapeId);
        b2Body_ApplyMassFromShapes(bodyId);
    }

    public override void UpdateGui()
    {
        base.UpdateGui();
        
        float height = 230.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_context.camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));

        ImGui.Begin("Modify Geometry", ImGuiWindowFlags.NoResize);

        if (ImGui.RadioButton("Circle", m_shapeType == B2ShapeType.b2_circleShape))
        {
            m_shapeType = B2ShapeType.b2_circleShape;
            UpdateShape();
        }

        if (ImGui.RadioButton("Capsule", m_shapeType == B2ShapeType.b2_capsuleShape))
        {
            m_shapeType = B2ShapeType.b2_capsuleShape;
            UpdateShape();
        }

        if (ImGui.RadioButton("Segment", m_shapeType == B2ShapeType.b2_segmentShape))
        {
            m_shapeType = B2ShapeType.b2_segmentShape;
            UpdateShape();
        }

        if (ImGui.RadioButton("Polygon", m_shapeType == B2ShapeType.b2_polygonShape))
        {
            m_shapeType = B2ShapeType.b2_polygonShape;
            UpdateShape();
        }

        if (ImGui.SliderFloat("Scale", ref m_scale, 0.1f, 10.0f, "%.2f"))
        {
            UpdateShape();
        }

        B2BodyId bodyId = b2Shape_GetBody(m_shapeId);
        B2BodyType bodyType = b2Body_GetType(bodyId);

        if (ImGui.RadioButton("Static", bodyType == B2BodyType.b2_staticBody))
        {
            b2Body_SetType(bodyId, B2BodyType.b2_staticBody);
        }

        if (ImGui.RadioButton("Kinematic", bodyType == B2BodyType.b2_kinematicBody))
        {
            b2Body_SetType(bodyId, B2BodyType.b2_kinematicBody);
        }

        if (ImGui.RadioButton("Dynamic", bodyType == B2BodyType.b2_dynamicBody))
        {
            b2Body_SetType(bodyId, B2BodyType.b2_dynamicBody);
        }

        ImGui.End();
    }
}
