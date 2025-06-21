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

// This sample shows how to use the ray and shape cast functions on a b2World. This
// sample is configured to ignore initial overlap.
public class CastWorld : Sample
{
    private static readonly int SampleRayCastWorld = SampleFactory.Shared.RegisterSample("Collision", "Cast World", Create);

    enum Mode
    {
        e_any = 0,
        e_closest = 1,
        e_multiple = 2,
        e_sorted = 3
    };

    enum CastType
    {
        e_rayCast = 0,
        e_circleCast = 1,
        e_capsuleCast = 2,
        e_polygonCast = 3
    };

    public const int e_maxCount = 64;

    private int m_bodyIndex;
    private B2BodyId[] m_bodyIds = new B2BodyId[e_maxCount];
    private ShapeUserData[] m_userData = new ShapeUserData[e_maxCount];
    private B2Polygon[] m_polygons = new B2Polygon[4];
    private B2Capsule m_capsule;
    private B2Circle m_circle;
    private B2Segment m_segment;

    private bool m_simple;

    private int m_mode;
    private int m_ignoreIndex;

    private CastType m_castType;
    private float m_castRadius;

    private B2Vec2 m_angleAnchor;
    private float m_baseAngle;
    private float m_angle;
    private bool m_rotating;

    private B2Vec2 m_rayStart;
    private B2Vec2 m_rayEnd;
    private bool m_dragging;


    private static Sample Create(SampleContext context)
    {
        return new CastWorld(context);
    }


    public CastWorld(SampleContext context) : base(context)
    {
        if (m_context.settings.restart == false)
        {
            m_camera.m_center = new B2Vec2(2.0f, 14.0f);
            m_camera.m_zoom = 25.0f * 0.75f;
        }

        // Ground body
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(m_worldId, ref bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
            b2CreateSegmentShape(groundId, ref shapeDef, ref segment);
        }

        {
            B2Vec2[] vertices = new B2Vec2[3] { new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), new B2Vec2(0.0f, 1.5f) };
            B2Hull hull = b2ComputeHull(vertices, 3);
            m_polygons[0] = b2MakePolygon(ref hull, 0.0f);
        }

        {
            B2Vec2[] vertices = new B2Vec2[3] { new B2Vec2(-0.1f, 0.0f), new B2Vec2(0.1f, 0.0f), new B2Vec2(0.0f, 1.5f) };
            B2Hull hull = b2ComputeHull(vertices, 3);
            m_polygons[1] = b2MakePolygon(ref hull, 0.0f);
            m_polygons[1].radius = 0.5f;
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
        for (int i = 0; i < m_userData.Length; ++i)
        {
            m_userData[i] = new ShapeUserData();
        }

        m_capsule = new B2Capsule(new B2Vec2(-0.5f, 0.0f), new B2Vec2(0.5f, 0.0f), 0.25f);
        m_circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        m_segment = new B2Segment(new B2Vec2(-1.0f, 0.0f), new B2Vec2(1.0f, 0.0f));

        m_bodyIndex = 0;

        for (int i = 0; i < e_maxCount; ++i)
        {
            m_bodyIds[i] = b2_nullBodyId;
        }

        m_mode = (int)Mode.e_closest;
        m_ignoreIndex = 7;

        m_castType = CastType.e_rayCast;
        m_castRadius = 0.5f;

        m_rayStart = new B2Vec2(-20.0f, 10.0f);
        m_rayEnd = new B2Vec2(20.0f, 10.0f);
        m_dragging = false;

        m_angle = 0.0f;
        m_baseAngle = 0.0f;
        m_angleAnchor = new B2Vec2(0.0f, 0.0f);
        m_rotating = false;

        m_simple = false;
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

        int mod = m_bodyIndex % 3;
        if (mod == 0)
        {
            bodyDef.type = B2BodyType.b2_staticBody;
        }
        else if (mod == 1)
        {
            bodyDef.type = B2BodyType.b2_kinematicBody;
        }
        else if (mod == 2)
        {
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.gravityScale = 0.0f;
        }

        m_bodyIds[m_bodyIndex] = b2CreateBody(m_worldId, ref bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.userData = m_userData[m_bodyIndex];
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
                m_rayStart = p;
                m_rayEnd = p;
                m_dragging = true;
            }
            else if (0 != ((uint)mods & (uint)KeyModifiers.Shift) && m_dragging == false)
            {
                m_rotating = true;
                m_angleAnchor = p;
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
            m_rayEnd = p;
        }
        else if (m_rotating)
        {
            float dx = p.X - m_angleAnchor.X;
            m_angle = m_baseAngle + 1.0f * dx;
        }
    }

    public override void UpdateGui()
    {
        base.UpdateGui();

        float height = 320.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, m_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));

        ImGui.Begin("Ray-cast World", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        ImGui.Checkbox("Simple", ref m_simple);

        if (m_simple == false)
        {
            string[] castTypes = ["Ray", "Circle", "Capsule", "Polygon"];
            int castType = (int)m_castType;
            if (ImGui.Combo("Type", ref castType, castTypes, castTypes.Length))
            {
                m_castType = (CastType)castType;
            }

            if (m_castType != CastType.e_rayCast)
            {
                ImGui.SliderFloat("Radius", ref m_castRadius, 0.0f, 2.0f, "%.1f");
            }

            string[] modes = ["Any", "Closest", "Multiple", "Sorted"];
            int mode = m_mode;
            if (ImGui.Combo("Mode", ref mode, modes, modes.Length))
            {
                m_mode = mode;
            }
        }

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

        ImGui.End();
    }

    public override void Step()
    {
        base.Step();

        B2HexColor color1 = B2HexColor.b2_colorGreen;
        B2HexColor color2 = B2HexColor.b2_colorLightGray;
        B2HexColor color3 = B2HexColor.b2_colorMagenta;

        B2Vec2 rayTranslation = b2Sub(m_rayEnd, m_rayStart);

        if (m_simple)
        {
            // This version doesn't have a callback, but it doesn't skip the ignored shape
            B2RayResult result = b2World_CastRayClosest(m_worldId, m_rayStart, rayTranslation, b2DefaultQueryFilter());

            if (result.hit == true && result.fraction > 0.0f)
            {
                B2Vec2 c = b2MulAdd(m_rayStart, result.fraction, rayTranslation);
                m_draw.DrawPoint(result.point, 5.0f, color1);
                m_draw.DrawLine(m_rayStart, c, color2);
                B2Vec2 head = b2MulAdd(result.point, 0.5f, result.normal);
                m_draw.DrawLine(result.point, head, color3);
            }
            else
            {
                m_draw.DrawLine(m_rayStart, m_rayEnd, color2);
            }
        }
        else
        {
            b2CastResultFcn[] functions =
            [
                RayCastAnyCallback,
                RayCastClosestCallback,
                RayCastMultipleCallback,
                RayCastSortedCallback
            ];
            b2CastResultFcn modeFcn = functions[m_mode];

            CastContext context = new CastContext();

            // Must initialize fractions for sorting
            context.fractions[0] = float.MaxValue;
            context.fractions[1] = float.MaxValue;
            context.fractions[2] = float.MaxValue;

            B2Transform transform = new B2Transform(m_rayStart, b2MakeRot(m_angle));
            B2Circle circle = new B2Circle(m_rayStart, m_castRadius);
            B2Capsule capsule = new B2Capsule(b2TransformPoint(ref transform, new B2Vec2(-0.25f, 0.0f)), b2TransformPoint(ref transform, new B2Vec2(0.25f, 0.0f)), m_castRadius);
            B2Polygon box = b2MakeOffsetRoundedBox(0.25f, 0.5f, transform.p, transform.q, m_castRadius);

            B2ShapeProxy proxy = new B2ShapeProxy();
            if (m_castType == CastType.e_rayCast)
            {
                b2World_CastRay(m_worldId, m_rayStart, rayTranslation, b2DefaultQueryFilter(), modeFcn, context);
            }
            else
            {
                if (m_castType == CastType.e_circleCast)
                {
                    proxy = b2MakeProxy(circle.center, 1, circle.radius);
                }
                else if (m_castType == CastType.e_capsuleCast)
                {
                    proxy = b2MakeProxy(capsule.center1, capsule.center2, 2, capsule.radius);
                }
                else
                {
                    proxy = b2MakeProxy(box.vertices.AsSpan(), box.count, box.radius);
                }

                b2World_CastShape(m_worldId, ref proxy, rayTranslation, b2DefaultQueryFilter(), modeFcn, context);
            }

            if (context.count > 0)
            {
                B2_ASSERT(context.count <= 3);
                B2HexColor[] colors = new B2HexColor[3] { B2HexColor.b2_colorRed, B2HexColor.b2_colorGreen, B2HexColor.b2_colorBlue };
                for (int i = 0; i < context.count; ++i)
                {
                    B2Vec2 c = b2MulAdd(m_rayStart, context.fractions[i], rayTranslation);
                    B2Vec2 p = context.points[i];
                    B2Vec2 n = context.normals[i];
                    m_draw.DrawPoint(p, 5.0f, colors[i]);
                    m_draw.DrawLine(m_rayStart, c, color2);
                    B2Vec2 head = b2MulAdd(p, 1.0f, n);
                    m_draw.DrawLine(p, head, color3);

                    B2Vec2 t = b2MulSV(context.fractions[i], rayTranslation);
                    B2Transform shiftedTransform = new B2Transform(t, b2Rot_identity);

                    if (m_castType == CastType.e_circleCast)
                    {
                        m_draw.DrawSolidCircle(ref shiftedTransform, circle.center, m_castRadius, B2HexColor.b2_colorYellow);
                    }
                    else if (m_castType == CastType.e_capsuleCast)
                    {
                        B2Vec2 p1 = capsule.center1 + t;
                        B2Vec2 p2 = capsule.center2 + t;
                        m_draw.DrawSolidCapsule(p1, p2, m_castRadius, B2HexColor.b2_colorYellow);
                    }
                    else if (m_castType == CastType.e_polygonCast)
                    {
                        m_draw.DrawSolidPolygon(ref shiftedTransform, box.vertices.AsSpan(), box.count, box.radius, B2HexColor.b2_colorYellow);
                    }
                }
            }
            else
            {
                B2Transform shiftedTransform = new B2Transform(b2Add(transform.p, rayTranslation), transform.q);
                m_draw.DrawLine(m_rayStart, m_rayEnd, color2);

                if (m_castType == CastType.e_circleCast)
                {
                    m_draw.DrawSolidCircle(ref shiftedTransform, b2Vec2_zero, m_castRadius, B2HexColor.b2_colorGray);
                }
                else if (m_castType == CastType.e_capsuleCast)
                {
                    B2Vec2 p1 = b2Add(b2TransformPoint(ref transform, capsule.center1), rayTranslation);
                    B2Vec2 p2 = b2Add(b2TransformPoint(ref transform, capsule.center2), rayTranslation);
                    m_draw.DrawSolidCapsule(p1, p2, m_castRadius, B2HexColor.b2_colorYellow);
                }
                else if (m_castType == CastType.e_polygonCast)
                {
                    m_draw.DrawSolidPolygon(ref shiftedTransform, box.vertices.AsSpan(), box.count, box.radius, B2HexColor.b2_colorYellow);
                }
            }
        }

        m_draw.DrawPoint(m_rayStart, 5.0f, B2HexColor.b2_colorGreen);
    }

    public override void Draw(Settings settings)
    {
        base.Draw(settings);

        DrawTextLine("Click left mouse button and drag to modify ray cast");
        DrawTextLine("Shape 7 is intentionally ignored by the ray");

        if (m_simple)
        {
            DrawTextLine("Simple closest point ray cast");
        }
        else
        {
            switch ((Mode)m_mode)
            {
                case Mode.e_any:
                    DrawTextLine("Cast mode: any - check for obstruction - unsorted");
                    break;

                case Mode.e_closest:
                    DrawTextLine("Cast mode: closest - find closest shape along the cast");
                    break;

                case Mode.e_multiple:
                    DrawTextLine("Cast mode: multiple - gather up to 3 shapes - unsorted");
                    break;

                case Mode.e_sorted:
                    DrawTextLine("Cast mode: sorted - gather up to 3 shapes sorted by closeness");
                    break;

                default:
                    B2_ASSERT(false);
                    break;
            }
        }

        if (B2_IS_NON_NULL(m_bodyIds[m_ignoreIndex]))
        {
            B2Vec2 p = b2Body_GetPosition(m_bodyIds[m_ignoreIndex]);
            p.X -= 0.2f;
            m_draw.DrawString(p, "ign");
        }
    }


    // This callback finds the closest hit. This is the most common callback used in games.
    static float RayCastClosestCallback(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context)
    {
        CastContext rayContext = (CastContext)context;

        ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(shapeId);

        // Ignore a specific shape. Also ignore initial overlap.
        if ((userData != null && userData.ignore) || fraction == 0.0f)
        {
            // By returning -1, we instruct the calling code to ignore this shape and
            // continue the ray-cast to the next shape.
            return -1.0f;
        }

        rayContext.points[0] = point;
        rayContext.normals[0] = normal;
        rayContext.fractions[0] = fraction;
        rayContext.count = 1;

        // By returning the current fraction, we instruct the calling code to clip the ray and
        // continue the ray-cast to the next shape. WARNING: do not assume that shapes
        // are reported in order. However, by clipping, we can always get the closest shape.
        return fraction;
    }

    // This callback finds any hit. For this type of query we are usually just checking for obstruction,
    // so the hit data is not relevant.
    // NOTE: shape hits are not ordered, so this may not return the closest hit
    static float RayCastAnyCallback(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context)
    {
        CastContext rayContext = (CastContext)context;

        ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(shapeId);

        // Ignore a specific shape. Also ignore initial overlap.
        if ((userData != null && userData.ignore) || fraction == 0.0f)
        {
            // By returning -1, we instruct the calling code to ignore this shape and
            // continue the ray-cast to the next shape.
            return -1.0f;
        }

        rayContext.points[0] = point;
        rayContext.normals[0] = normal;
        rayContext.fractions[0] = fraction;
        rayContext.count = 1;

        // At this point we have a hit, so we know the ray is obstructed.
        // By returning 0, we instruct the calling code to terminate the ray-cast.
        return 0.0f;
    }

    // This ray cast collects multiple hits along the ray.
    // The shapes are not necessary reported in order, so we might not capture
    // the closest shape.
    // NOTE: shape hits are not ordered, so this may return hits in any order. This means that
    // if you limit the number of results, you may discard the closest hit. You can see this
    // behavior in the sample.
    static float RayCastMultipleCallback(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context)
    {
        CastContext rayContext = (CastContext)context;

        ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(shapeId);

        // Ignore a specific shape. Also ignore initial overlap.
        if ((userData != null && userData.ignore) || fraction == 0.0f)
        {
            // By returning -1, we instruct the calling code to ignore this shape and
            // continue the ray-cast to the next shape.
            return -1.0f;
        }

        int count = rayContext.count;
        B2_ASSERT(count < 3);

        rayContext.points[count] = point;
        rayContext.normals[count] = normal;
        rayContext.fractions[count] = fraction;
        rayContext.count = count + 1;

        if (rayContext.count == 3)
        {
            // At this point the buffer is full.
            // By returning 0, we instruct the calling code to terminate the ray-cast.
            return 0.0f;
        }

        // By returning 1, we instruct the caller to continue without clipping the ray.
        return 1.0f;
    }

    // This ray cast collects multiple hits along the ray and sorts them.
    static float RayCastSortedCallback(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context)
    {
        CastContext rayContext = (CastContext)context;

        ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(shapeId);

        // Ignore a specific shape. Also ignore initial overlap.
        if ((userData != null && userData.ignore) || fraction == 0.0f)
        {
            // By returning -1, we instruct the calling code to ignore this shape and
            // continue the ray-cast to the next shape.
            return -1.0f;
        }

        int count = rayContext.count;
        B2_ASSERT(count <= 3);

        int index = 3;
        while (fraction < rayContext.fractions[index - 1])
        {
            index -= 1;

            if (index == 0)
            {
                break;
            }
        }

        if (index == 3)
        {
            // not closer, continue but tell the caller not to consider fractions further than the largest fraction acquired
            // this only happens once the buffer is full
            B2_ASSERT(rayContext.count == 3);
            B2_ASSERT(rayContext.fractions[2] <= 1.0f);
            return rayContext.fractions[2];
        }

        for (int j = 2; j > index; --j)
        {
            rayContext.points[j] = rayContext.points[j - 1];
            rayContext.normals[j] = rayContext.normals[j - 1];
            rayContext.fractions[j] = rayContext.fractions[j - 1];
        }

        rayContext.points[index] = point;
        rayContext.normals[index] = normal;
        rayContext.fractions[index] = fraction;
        rayContext.count = count < 3 ? count + 1 : 3;

        if (rayContext.count == 3)
        {
            return rayContext.fractions[2];
        }

        // By returning 1, we instruct the caller to continue without clipping the ray.
        return 1.0f;
    }
}