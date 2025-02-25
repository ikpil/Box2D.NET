// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.id;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.world;
using static Box2D.NET.Shared.random;

namespace Box2D.NET.Samples.Samples.Collisions;

public class RayCastWorld : Sample
{
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

    int m_bodyIndex;
    b2BodyId[] m_bodyIds = new b2BodyId[e_maxCount];
    ShapeUserData[] m_userData = new ShapeUserData[e_maxCount];
    b2Polygon[] m_polygons = new b2Polygon[4];
    b2Capsule m_capsule;
    b2Circle m_circle;
    b2Segment m_segment;

    bool m_simple;

    int m_mode;
    int m_ignoreIndex;

    CastType m_castType;
    float m_castRadius;

    b2Vec2 m_angleAnchor;
    float m_baseAngle;
    float m_angle;
    bool m_rotating;

    b2Vec2 m_rayStart;
    b2Vec2 m_rayEnd;
    bool m_dragging;

    static int sampleRayCastWorld = RegisterSample("Collision", "Ray Cast World", Create);

    static Sample Create(Settings settings)
    {
        return new RayCastWorld(settings);
    }


    public RayCastWorld(Settings settings) : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(2.0f, 14.0f);
            Draw.g_camera.m_zoom = 25.0f * 0.75f;
        }

        // Ground body
        {
            b2BodyDef bodyDef = b2DefaultBodyDef();
            b2BodyId groundId = b2CreateBody(m_worldId, bodyDef);

            b2ShapeDef shapeDef = b2DefaultShapeDef();
            b2Segment segment = new b2Segment(new b2Vec2(-40.0f, 0.0f), new b2Vec2(40.0f, 0.0f));
            b2CreateSegmentShape(groundId, shapeDef, segment);
        }

        {
            b2Vec2[] vertices = new b2Vec2[3] { new b2Vec2(-0.5f, 0.0f), new b2Vec2(0.5f, 0.0f), new b2Vec2(0.0f, 1.5f) };
            b2Hull hull = b2ComputeHull(vertices, 3);
            m_polygons[0] = b2MakePolygon(hull, 0.0f);
        }

        {
            b2Vec2[] vertices = new b2Vec2[3] { new b2Vec2(-0.1f, 0.0f), new b2Vec2(0.1f, 0.0f), new b2Vec2(0.0f, 1.5f) };
            b2Hull hull = b2ComputeHull(vertices, 3);
            m_polygons[1] = b2MakePolygon(hull, 0.0f);
            m_polygons[1].radius = 0.5f;
        }

        {
            float w = 1.0f;
            float b = w / (2.0f + MathF.Sqrt(2.0f));
            float s = MathF.Sqrt(2.0f) * b;

            b2Vec2[] vertices = new b2Vec2[8]
            {
                new b2Vec2(0.5f * s, 0.0f),
                new b2Vec2(0.5f * w, b),
                new b2Vec2(0.5f * w, b + s),
                new b2Vec2(0.5f * s, w),
                new b2Vec2(-0.5f * s, w),
                new b2Vec2(-0.5f * w, b + s),
                new b2Vec2(-0.5f * w, b),
                new b2Vec2(-0.5f * s, 0.0f),
            };

            b2Hull hull = b2ComputeHull(vertices, 8);
            m_polygons[2] = b2MakePolygon(hull, 0.0f);
        }

        m_polygons[3] = b2MakeBox(0.5f, 0.5f);
        m_capsule = new b2Capsule(new b2Vec2(-0.5f, 0.0f), new b2Vec2(0.5f, 0.0f), 0.25f);
        m_circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.5f);
        m_segment = new b2Segment(new b2Vec2(-1.0f, 0.0f), new b2Vec2(1.0f, 0.0f));

        m_bodyIndex = 0;

        for (int i = 0; i < e_maxCount; ++i)
        {
            m_bodyIds[i] = b2_nullBodyId;
        }

        m_mode = (int)Mode.e_closest;
        m_ignoreIndex = 7;

        m_castType = CastType.e_rayCast;
        m_castRadius = 0.5f;

        m_rayStart = new b2Vec2(-20.0f, 10.0f);
        m_rayEnd = new b2Vec2(20.0f, 10.0f);
        m_dragging = false;

        m_angle = 0.0f;
        m_baseAngle = 0.0f;
        m_angleAnchor = new b2Vec2(0.0f, 0.0f);
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

        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = new b2Vec2(x, y);
        bodyDef.rotation = b2MakeRot(RandomFloatRange(-B2_PI, B2_PI));

        int mod = m_bodyIndex % 3;
        if (mod == 0)
        {
            bodyDef.type = b2BodyType.b2_staticBody;
        }
        else if (mod == 1)
        {
            bodyDef.type = b2BodyType.b2_kinematicBody;
        }
        else if (mod == 2)
        {
            bodyDef.type = b2BodyType.b2_dynamicBody;
            bodyDef.gravityScale = 0.0f;
        }

        m_bodyIds[m_bodyIndex] = b2CreateBody(m_worldId, bodyDef);

        b2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.userData = m_userData[m_bodyIndex];
        m_userData[m_bodyIndex].ignore = false;
        if (m_bodyIndex == m_ignoreIndex)
        {
            m_userData[m_bodyIndex].ignore = true;
        }

        if (index < 4)
        {
            b2CreatePolygonShape(m_bodyIds[m_bodyIndex], shapeDef, m_polygons[index]);
        }
        else if (index == 4)
        {
            b2CreateCircleShape(m_bodyIds[m_bodyIndex], shapeDef, m_circle);
        }
        else if (index == 5)
        {
            b2CreateCapsuleShape(m_bodyIds[m_bodyIndex], shapeDef, m_capsule);
        }
        else
        {
            b2CreateSegmentShape(m_bodyIds[m_bodyIndex], shapeDef, m_segment);
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

    public override void MouseDown(b2Vec2 p, int button, int mods)
    {
        if (button == (int)MouseButton.Left)
        {
            if (mods == 0 && m_rotating == false)
            {
                m_rayStart = p;
                m_rayEnd = p;
                m_dragging = true;
            }
            else if (0 != (mods & (uint)Keys.ShiftLeft) && m_dragging == false)
            {
                m_rotating = true;
                m_angleAnchor = p;
                m_baseAngle = m_angle;
            }
        }
    }

    public override void MouseUp(b2Vec2 _, int button)
    {
        if (button == (int)MouseButton.Left)
        {
            m_dragging = false;
            m_rotating = false;
        }
    }

    public override void MouseMove(b2Vec2 p)
    {
        if (m_dragging)
        {
            m_rayEnd = p;
        }
        else if (m_rotating)
        {
            float dx = p.x - m_angleAnchor.x;
            m_angle = m_baseAngle + 1.0f * dx;
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 300.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(200.0f, height));

        ImGui.Begin("Ray-cast World", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

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

    public override void Step(Settings settings)
    {
        base.Step(settings);

        Draw.g_draw.DrawString(5, m_textLine, "Click left mouse button and drag to modify ray cast");
        m_textLine += m_textIncrement;
        Draw.g_draw.DrawString(5, m_textLine, "Shape 7 is intentionally ignored by the ray");
        m_textLine += m_textIncrement;

        m_textLine += m_textIncrement;

        b2HexColor color1 = b2HexColor.b2_colorGreen;
        b2HexColor color2 = b2HexColor.b2_colorLightGray;
        b2HexColor color3 = b2HexColor.b2_colorMagenta;

        b2Vec2 rayTranslation = b2Sub(m_rayEnd, m_rayStart);

        if (m_simple)
        {
            Draw.g_draw.DrawString(5, m_textLine, "Simple closest point ray cast");
            m_textLine += m_textIncrement;

            // This version doesn't have a callback, but it doesn't skip the ignored shape
            b2RayResult result = b2World_CastRayClosest(m_worldId, m_rayStart, rayTranslation, b2DefaultQueryFilter());

            if (result.hit == true)
            {
                b2Vec2 c = b2MulAdd(m_rayStart, result.fraction, rayTranslation);
                Draw.g_draw.DrawPoint(result.point, 5.0f, color1);
                Draw.g_draw.DrawSegment(m_rayStart, c, color2);
                b2Vec2 head = b2MulAdd(result.point, 0.5f, result.normal);
                Draw.g_draw.DrawSegment(result.point, head, color3);
            }
            else
            {
                Draw.g_draw.DrawSegment(m_rayStart, m_rayEnd, color2);
            }
        }
        else
        {
            switch ((Mode)m_mode)
            {
                case Mode.e_any:
                    Draw.g_draw.DrawString(5, m_textLine, "Cast mode: any - check for obstruction - unsorted");
                    break;

                case Mode.e_closest:
                    Draw.g_draw.DrawString(5, m_textLine, "Cast mode: closest - find closest shape along the cast");
                    break;

                case Mode.e_multiple:
                    Draw.g_draw.DrawString(5, m_textLine, "Cast mode: multiple - gather up to 3 shapes - unsorted");
                    break;

                case Mode.e_sorted:
                    Draw.g_draw.DrawString(5, m_textLine, "Cast mode: sorted - gather up to 3 shapes sorted by closeness");
                    break;
            }

            m_textLine += m_textIncrement;

            b2CastResultFcn[] fcns = [RayCastAnyCallback, RayCastClosestCallback, RayCastMultipleCallback, RayCastSortedCallback];
            b2CastResultFcn modeFcn = fcns[m_mode];

            RayCastContext context = new RayCastContext();

            // Must initialize fractions for sorting
            context.fractions[0] = float.MaxValue;
            context.fractions[1] = float.MaxValue;
            context.fractions[2] = float.MaxValue;

            b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), m_castRadius);
            b2Capsule capsule = new b2Capsule(new b2Vec2(-0.25f, 0.0f), new b2Vec2(0.25f, 0.0f), m_castRadius);
            b2Polygon box = b2MakeRoundedBox(0.25f, 0.5f, m_castRadius);
            b2Transform transform = new b2Transform(m_rayStart, b2MakeRot(m_angle));

            switch (m_castType)
            {
                case CastType.e_rayCast:
                    b2World_CastRay(m_worldId, m_rayStart, rayTranslation, b2DefaultQueryFilter(), modeFcn, context);
                    break;

                case CastType.e_circleCast:
                    b2World_CastCircle(m_worldId, circle, transform, rayTranslation, b2DefaultQueryFilter(), modeFcn, context);
                    break;

                case CastType.e_capsuleCast:
                    b2World_CastCapsule(m_worldId, capsule, transform, rayTranslation, b2DefaultQueryFilter(), modeFcn, context);
                    break;

                case CastType.e_polygonCast:
                    b2World_CastPolygon(m_worldId, box, transform, rayTranslation, b2DefaultQueryFilter(), modeFcn, context);
                    break;
            }

            if (context.count > 0)
            {
                Debug.Assert(context.count <= 3);
                b2HexColor[] colors = new b2HexColor[3] { b2HexColor.b2_colorRed, b2HexColor.b2_colorGreen, b2HexColor.b2_colorBlue };
                for (int i = 0; i < context.count; ++i)
                {
                    b2Vec2 c = b2MulAdd(m_rayStart, context.fractions[i], rayTranslation);
                    b2Vec2 p = context.points[i];
                    b2Vec2 n = context.normals[i];
                    Draw.g_draw.DrawPoint(p, 5.0f, colors[i]);
                    Draw.g_draw.DrawSegment(m_rayStart, c, color2);
                    b2Vec2 head = b2MulAdd(p, 0.5f, n);
                    Draw.g_draw.DrawSegment(p, head, color3);

                    b2Vec2 t = b2MulSV(context.fractions[i], rayTranslation);
                    b2Transform shiftedTransform = new b2Transform(b2Add(transform.p, t), transform.q);

                    if (m_castType == CastType.e_circleCast)
                    {
                        Draw.g_draw.DrawSolidCircle(ref shiftedTransform, b2Vec2_zero, m_castRadius, b2HexColor.b2_colorYellow);
                    }
                    else if (m_castType == CastType.e_capsuleCast)
                    {
                        b2Vec2 p1 = b2Add(b2TransformPoint(ref transform, capsule.center1), t);
                        b2Vec2 p2 = b2Add(b2TransformPoint(ref transform, capsule.center2), t);
                        Draw.g_draw.DrawSolidCapsule(p1, p2, m_castRadius, b2HexColor.b2_colorYellow);
                    }
                    else if (m_castType == CastType.e_polygonCast)
                    {
                        Draw.g_draw.DrawSolidPolygon(ref shiftedTransform, box.vertices, box.count, box.radius, b2HexColor.b2_colorYellow);
                    }
                }
            }
            else
            {
                b2Transform shiftedTransform = new b2Transform(b2Add(transform.p, rayTranslation), transform.q);
                Draw.g_draw.DrawSegment(m_rayStart, m_rayEnd, color2);

                if (m_castType == CastType.e_circleCast)
                {
                    Draw.g_draw.DrawSolidCircle(ref shiftedTransform, b2Vec2_zero, m_castRadius, b2HexColor.b2_colorGray);
                }
                else if (m_castType == CastType.e_capsuleCast)
                {
                    b2Vec2 p1 = b2Add(b2TransformPoint(ref transform, capsule.center1), rayTranslation);
                    b2Vec2 p2 = b2Add(b2TransformPoint(ref transform, capsule.center2), rayTranslation);
                    Draw.g_draw.DrawSolidCapsule(p1, p2, m_castRadius, b2HexColor.b2_colorYellow);
                }
                else if (m_castType == CastType.e_polygonCast)
                {
                    Draw.g_draw.DrawSolidPolygon(ref shiftedTransform, box.vertices, box.count, box.radius, b2HexColor.b2_colorYellow);
                }
            }
        }

        Draw.g_draw.DrawPoint(m_rayStart, 5.0f, b2HexColor.b2_colorGreen);

        if (B2_IS_NON_NULL(m_bodyIds[m_ignoreIndex]))
        {
            b2Vec2 p = b2Body_GetPosition(m_bodyIds[m_ignoreIndex]);
            p.x -= 0.2f;
            Draw.g_draw.DrawString(p, "ign");
        }
    }


// This callback finds the closest hit. This is the most common callback used in games.
    static float RayCastClosestCallback(b2ShapeId shapeId, b2Vec2 point, b2Vec2 normal, float fraction, object context)
    {
        RayCastContext rayContext = (RayCastContext)context;

        ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(shapeId);
        if (userData != null && userData.ignore)
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
    static float RayCastAnyCallback(b2ShapeId shapeId, b2Vec2 point, b2Vec2 normal, float fraction, object context)
    {
        RayCastContext rayContext = (RayCastContext)context;

        ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(shapeId);
        if (userData != null && userData.ignore)
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
    static float RayCastMultipleCallback(b2ShapeId shapeId, b2Vec2 point, b2Vec2 normal, float fraction, object context)
    {
        RayCastContext rayContext = (RayCastContext)context;

        ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(shapeId);
        if (userData != null && userData.ignore)
        {
            // By returning -1, we instruct the calling code to ignore this shape and
            // continue the ray-cast to the next shape.
            return -1.0f;
        }

        int count = rayContext.count;
        Debug.Assert(count < 3);

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
    static float RayCastSortedCallback(b2ShapeId shapeId, b2Vec2 point, b2Vec2 normal, float fraction, object context)
    {
        RayCastContext rayContext = (RayCastContext)context;

        ShapeUserData userData = (ShapeUserData)b2Shape_GetUserData(shapeId);
        if (userData != null && userData.ignore)
        {
            // By returning -1, we instruct the calling code to ignore this shape and
            // continue the ray-cast to the next shape.
            return -1.0f;
        }

        int count = rayContext.count;
        Debug.Assert(count <= 3);

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
            Debug.Assert(rayContext.count == 3);
            Debug.Assert(rayContext.fractions[2] <= 1.0f);
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
