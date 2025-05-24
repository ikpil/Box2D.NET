// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Distances;
using static Box2D.NET.Shared.RandomSupports;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Samples.Samples.Collisions;

public class OverlapWorld : Sample
{
    private static readonly int SampleOverlapWorld = SampleFactory.Shared.RegisterSample("Collision", "Overlap World", Create);

    public const int e_circleShape = 0;
    public const int e_capsuleShape = 1;
    public const int e_boxShape = 2;
    public const int e_maxCount = 64;
    public const int e_maxDoomed = 16;

    private int m_bodyIndex;
    private B2BodyId[] m_bodyIds = new B2BodyId[e_maxCount];
    private ShapeUserData[] m_userData;
    private B2Polygon[] m_polygons = new B2Polygon[4];
    private B2Capsule m_capsule;
    private B2Circle m_circle;
    private B2Segment m_segment;
    private int m_ignoreIndex;

    private B2ShapeId[] m_doomIds = new B2ShapeId[e_maxDoomed];
    private int m_doomCount;

    private int m_shapeType;
    private B2Transform m_transform;

    private B2Vec2 m_startPosition;

    private B2Vec2 m_position;
    private B2Vec2 m_basePosition;
    private float m_angle;
    private float m_baseAngle;

    private bool m_dragging;
    private bool m_rotating;


    private static Sample Create(SampleContext context)
    {
        return new OverlapWorld(context);
    }


    static bool OverlapResultFcn(B2ShapeId shapeId, object context)
    {
        ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(shapeId);
        if (userData != null && userData.ignore)
        {
            // continue the query
            return true;
        }

        OverlapWorld sample = (OverlapWorld)context;

        if (sample.m_doomCount < e_maxDoomed)
        {
            int index = sample.m_doomCount;
            sample.m_doomIds[index] = shapeId;
            sample.m_doomCount += 1;
        }

        // continue the query
        return true;
    }

    public OverlapWorld(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(0.0f, 10.0f);
            m_camera.m_zoom = 25.0f * 0.7f;
        }

        m_userData = new ShapeUserData[e_maxCount];
        for (int i = 0; i < m_userData.Length; ++i)
        {
            m_userData[i] = new ShapeUserData();
        }

        {
            B2Vec2[] vertices = new B2Vec2[3] { new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), new B2Vec2(0.0f, 1.5f), };
            B2Hull hull = b2ComputeHull(vertices, 3);
            m_polygons[0] = b2MakePolygon(ref hull, 0.0f);
        }

        {
            B2Vec2[] vertices = new B2Vec2[3] { new B2Vec2(-0.1f, 0.0f), new B2Vec2(0.1f, 0.0f), new B2Vec2(0.0f, 1.5f) };
            B2Hull hull = b2ComputeHull(vertices, 3);
            m_polygons[1] = b2MakePolygon(ref hull, 0.0f);
        }

        {
            float w = 1.0f;
            float b = w / (2.0f + MathF.Sqrt(2.0f));
            float s = MathF.Sqrt(2.0f) * b;

            B2Vec2[] vertices = new B2Vec2[8]
            {
                new B2Vec2(0.5f * s, 0.0f),
                new B2Vec2(0.5f * w, b),
                new B2Vec2(0.5f * w, b + s),
                new B2Vec2(0.5f * s, w),
                new B2Vec2(-0.5f * s, w),
                new B2Vec2(-0.5f * w, b + s),
                new B2Vec2(-0.5f * w, b),
                new B2Vec2(-0.5f * s, 0.0f),
            };

            B2Hull hull = b2ComputeHull(vertices, 8);
            m_polygons[2] = b2MakePolygon(ref hull, 0.0f);
        }

        m_polygons[3] = b2MakeBox(0.5f, 0.5f);
        m_capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);
        m_circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        m_segment = new B2Segment(new B2Vec2(-1.0f, 0.0f), new B2Vec2(1.0f, 0.0f));

        m_bodyIndex = 0;

        for (int i = 0; i < e_maxCount; ++i)
        {
            m_bodyIds[i] = b2_nullBodyId;
        }

        m_ignoreIndex = 7;

        m_shapeType = e_circleShape;

        m_position = new B2Vec2(.0f, 10.0f);
        m_angle = 0.0f;
        m_dragging = false;
        m_rotating = false;

        m_doomCount = 0;

        CreateN(0, 10);
    }

    void Create(int index)
    {
        if (B2_IS_NON_NULL(m_bodyIds[m_bodyIndex]))
        {
            b2DestroyBody(m_bodyIds[m_bodyIndex]);
            m_bodyIds[m_bodyIndex] = b2_nullBodyId;
        }

        float x = RandomFloatRange(-20.0f, 20.0f);
        float y = RandomFloatRange(0.0f, 20.0f);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = new B2Vec2(x, y);
        bodyDef.rotation = b2MakeRot(RandomFloatRange(-B2_PI, B2_PI));

        m_bodyIds[m_bodyIndex] = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.userData = m_userData[m_bodyIndex];
        m_userData[m_bodyIndex].index = m_bodyIndex;
        m_userData[m_bodyIndex].ignore = false;
        if (m_bodyIndex == m_ignoreIndex)
        {
            m_userData[m_bodyIndex].ignore = true;
        }

        if (index < 4)
        {
            b2CreatePolygonShape(m_bodyIds[m_bodyIndex], ref shapeDef, ref m_polygons[index]);
        }
        else if (index == 4)
        {
            b2CreateCircleShape(m_bodyIds[m_bodyIndex], ref shapeDef, ref m_circle);
        }
        else if (index == 5)
        {
            b2CreateCapsuleShape(m_bodyIds[m_bodyIndex], ref shapeDef, ref m_capsule);
        }
        else
        {
            b2CreateSegmentShape(m_bodyIds[m_bodyIndex], ref shapeDef, ref m_segment);
        }

        m_bodyIndex = (m_bodyIndex + 1) % e_maxCount;
    }

    void CreateN(int index, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            Create(index);
        }
    }

    void DestroyBody()
    {
        for (int i = 0; i < e_maxCount; ++i)
        {
            if (B2_IS_NON_NULL(m_bodyIds[i]))
            {
                b2DestroyBody(m_bodyIds[i]);
                m_bodyIds[i] = b2_nullBodyId;
                return;
            }
        }
    }

    public override void MouseDown(B2Vec2 p, MouseButton button, KeyModifiers mods)
    {
        if (button == (int)MouseButton.Left)
        {
            if (mods == 0 && m_rotating == false)
            {
                m_dragging = true;
                m_position = p;
            }
            else if (0 != ((uint)mods & (uint)KeyModifiers.Shift) && m_dragging == false)
            {
                m_rotating = true;
                m_startPosition = p;
                m_baseAngle = m_angle;
            }
        }
    }

    public override void MouseUp(B2Vec2 p, MouseButton button)
    {
        if (button == (int)MouseButton.Left)
        {
            m_dragging = false;
            m_rotating = false;
        }
    }

    public override void MouseMove(B2Vec2 p)
    {
        if (m_dragging)
        {
            m_position = p;
        }
        else if (m_rotating)
        {
            float dx = p.X - m_startPosition.X;
            m_angle = m_baseAngle + 1.0f * dx;
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float height = 330.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(140.0f, height));

        ImGui.Begin("Overlap World", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (ImGui.Button("Polygon 1"))
            Create(0);
        ImGui.SameLine();
        if (ImGui.Button("10x##Poly1"))
            CreateN(0, 10);

        if (ImGui.Button("Polygon 2"))
            Create(1);
        ImGui.SameLine();
        if (ImGui.Button("10x##Poly2"))
            CreateN(1, 10);

        if (ImGui.Button("Polygon 3"))
            Create(2);
        ImGui.SameLine();
        if (ImGui.Button("10x##Poly3"))
            CreateN(2, 10);

        if (ImGui.Button("Box"))
            Create(3);
        ImGui.SameLine();
        if (ImGui.Button("10x##Box"))
            CreateN(3, 10);

        if (ImGui.Button("Circle"))
            Create(4);
        ImGui.SameLine();
        if (ImGui.Button("10x##Circle"))
            CreateN(4, 10);

        if (ImGui.Button("Capsule"))
            Create(5);
        ImGui.SameLine();
        if (ImGui.Button("10x##Capsule"))
            CreateN(5, 10);

        if (ImGui.Button("Segment"))
            Create(6);
        ImGui.SameLine();
        if (ImGui.Button("10x##Segment"))
            CreateN(6, 10);

        if (ImGui.Button("Destroy Shape"))
        {
            DestroyBody();
        }

        ImGui.Separator();
        ImGui.Text("Overlap Shape");
        ImGui.RadioButton("Circle##Overlap", ref m_shapeType, e_circleShape);
        ImGui.RadioButton("Capsule##Overlap", ref m_shapeType, e_capsuleShape);
        ImGui.RadioButton("Box##Overlap", ref m_shapeType, e_boxShape);

        ImGui.End();
    }

    public override void Step()
    {
        base.Step();

        m_doomCount = 0;

        B2Transform transform = new B2Transform(m_position, b2MakeRot(m_angle));
        B2ShapeProxy proxy = new B2ShapeProxy();

        if (m_shapeType == e_circleShape)
        {
            B2Circle circle;
            circle.center = transform.p;
            circle.radius = 1.0f;
            proxy = b2MakeProxy(circle.center, 1, circle.radius);
            B2Transform identity = b2Transform_identity;
            m_draw.DrawSolidCircle(ref identity, circle.center, circle.radius, B2HexColor.b2_colorWhite);
        }
        else if (m_shapeType == e_capsuleShape)
        {
            B2Capsule capsule;
            capsule.center1 = b2TransformPoint(ref transform, new B2Vec2(-1.0f, 0.0f));
            capsule.center2 = b2TransformPoint(ref transform, new B2Vec2(1.0f, 0.0f));
            capsule.radius = 0.5f;
            proxy = b2MakeProxy(capsule.center1, capsule.center2, 2, capsule.radius);
            m_draw.DrawSolidCapsule(capsule.center1, capsule.center2, capsule.radius, B2HexColor.b2_colorWhite);
        }
        else if (m_shapeType == e_boxShape)
        {
            B2Polygon box = b2MakeOffsetBox(2.0f, 0.5f, transform.p, transform.q);
            proxy = b2MakeProxy(box.vertices.AsSpan(), box.count, box.radius);
            m_draw.DrawPolygon(box.vertices.AsSpan(), box.count, B2HexColor.b2_colorWhite);
        }

        b2World_OverlapShape(m_worldId, ref proxy, b2DefaultQueryFilter(), OverlapResultFcn, this);


        for (int i = 0; i < m_doomCount; ++i)
        {
            B2ShapeId shapeId = m_doomIds[i];
            ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(shapeId);
            if (userData == null)
            {
                continue;
            }

            int index = userData.index;
            B2_ASSERT(0 <= index && index < e_maxCount);
            B2_ASSERT(B2_IS_NON_NULL(m_bodyIds[index]));

            b2DestroyBody(m_bodyIds[index]);
            m_bodyIds[index] = b2_nullBodyId;
        }
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        DrawTextLine("left mouse button: drag query shape");
        
        DrawTextLine("left mouse button + shift: rotate query shape");
        

        if (B2_IS_NON_NULL(m_bodyIds[m_ignoreIndex]))
        {
            B2Vec2 p = b2Body_GetPosition(m_bodyIds[m_ignoreIndex]);
            p.X -= 0.2f;
            m_draw.DrawString(p, "skip");
        }
    }
}