using System.Numerics;
using Box2D.NET.Primitives;
using ImGuiNET;
using Silk.NET.GLFW;
using static Box2D.NET.geometry;
using static Box2D.NET.math_function;
using static Box2D.NET.manifold;

namespace Box2D.NET.Samples.Samples.Collisions;

public class SmoothManifold : Sample
{
    enum ShapeType
    {
        e_circleShape = 0,
        e_boxShape
    }

    ShapeType m_shapeType;

    b2ChainSegment[] m_segments;
    int m_count;

    b2Transform m_transform;
    float m_angle;
    float m_round;

    b2Vec2 m_basePosition;
    b2Vec2 m_startPoint;
    float m_baseAngle;

    bool m_dragging;
    bool m_rotating;
    bool m_showIds;
    bool m_showAnchors;
    bool m_showSeparation;

    static int sampleSmoothManifoldIndex = RegisterSample("Collision", "Smooth Manifold", Create);

    static Sample Create(Settings settings)
    {
        return new SmoothManifold(settings);
    }


    SmoothManifold(Settings settings)
        : base(settings)
    {
        if (settings.restart == false)
        {
            Draw.g_camera.m_center = new b2Vec2(2.0f, 20.0f);
            Draw.g_camera.m_zoom = 21.0f;
        }

        m_shapeType = ShapeType.e_boxShape;
        m_transform = new b2Transform(new b2Vec2(0.0f, 20.0f), b2Rot_identity);
        m_angle = 0.0f;
        m_round = 0.0f;

        m_startPoint = new b2Vec2(0.0f, 00.0f);
        m_basePosition = new b2Vec2(0.0f, 0.0f);
        m_baseAngle = 0.0f;

        m_dragging = false;
        m_rotating = false;
        m_showIds = false;
        m_showAnchors = false;
        m_showSeparation = false;

        // https://betravis.github.io/shape-tools/path-to-polygon/
        m_count = 36;

        b2Vec2[] points = new b2Vec2[36];
        points[0] = new b2Vec2(-20.58325f, 14.54175f);
        points[1] = new b2Vec2(-21.90625f, 15.8645f);
        points[2] = new b2Vec2(-24.552f, 17.1875f);
        points[3] = new b2Vec2(-27.198f, 11.89575f);
        points[4] = new b2Vec2(-29.84375f, 15.8645f);
        points[5] = new b2Vec2(-29.84375f, 21.15625f);
        points[6] = new b2Vec2(-25.875f, 23.802f);
        points[7] = new b2Vec2(-20.58325f, 25.125f);
        points[8] = new b2Vec2(-25.875f, 29.09375f);
        points[9] = new b2Vec2(-20.58325f, 31.7395f);
        points[10] = new b2Vec2(-11.0089998f, 23.2290001f);
        points[11] = new b2Vec2(-8.67700005f, 21.15625f);
        points[12] = new b2Vec2(-6.03125f, 21.15625f);
        points[13] = new b2Vec2(-7.35424995f, 29.09375f);
        points[14] = new b2Vec2(-3.38549995f, 29.09375f);
        points[15] = new b2Vec2(1.90625f, 30.41675f);
        points[16] = new b2Vec2(5.875f, 17.1875f);
        points[17] = new b2Vec2(11.16675f, 25.125f);
        points[18] = new b2Vec2(9.84375f, 29.09375f);
        points[19] = new b2Vec2(13.8125f, 31.7395f);
        points[20] = new b2Vec2(21.75f, 30.41675f);
        points[21] = new b2Vec2(28.3644981f, 26.448f);
        points[22] = new b2Vec2(25.71875f, 18.5105f);
        points[23] = new b2Vec2(24.3957481f, 13.21875f);
        points[24] = new b2Vec2(17.78125f, 11.89575f);
        points[25] = new b2Vec2(15.1355f, 7.92700005f);
        points[26] = new b2Vec2(5.875f, 9.25f);
        points[27] = new b2Vec2(1.90625f, 11.89575f);
        points[28] = new b2Vec2(-3.25f, 11.89575f);
        points[29] = new b2Vec2(-3.25f, 9.9375f);
        points[30] = new b2Vec2(-4.70825005f, 9.25f);
        points[31] = new b2Vec2(-8.67700005f, 9.25f);
        points[32] = new b2Vec2(-11.323f, 11.89575f);
        points[33] = new b2Vec2(-13.96875f, 11.89575f);
        points[34] = new b2Vec2(-15.29175f, 14.54175f);
        points[35] = new b2Vec2(-19.2605f, 14.54175f);

        m_segments = new b2ChainSegment[m_count];
        for (int i = 0; i < m_segments.Length; ++i)
        {
            m_segments[i] = new b2ChainSegment();
        }

        for (int i = 0; i < m_count; ++i)
        {
            int i0 = i > 0 ? i - 1 : m_count - 1;
            int i1 = i;
            int i2 = i1 < m_count - 1 ? i1 + 1 : 0;
            int i3 = i2 < m_count - 1 ? i2 + 1 : 0;

            b2Vec2 g1 = points[i0];
            b2Vec2 p1 = points[i1];
            b2Vec2 p2 = points[i2];
            b2Vec2 g2 = points[i3];

            // { g1, { p1, p2 }, g2, -1 };
            m_segments[i].ghost1 = g1;
            m_segments[i].segment.point1 = p1;
            m_segments[i].segment.point2 = p2;
            m_segments[i].ghost2 = g2;
            m_segments[i].chainId = -1;
        }
    }

    public override void UpdateUI()
    {
        bool open = false;
        float height = 290.0f;
        ImGui.SetNextWindowPos(new Vector2(10.0f, Draw.g_camera.m_height - height - 50.0f), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(180.0f, height));

        ImGui.Begin("Smooth Manifold", ref open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        ImGui.PushItemWidth(100.0f);

        {
            string[] shapeTypes = { "Circle", "Box" };
            int shapeType = (int)m_shapeType;
            ImGui.Combo("Shape", ref shapeType, shapeTypes, shapeTypes.Length);
            m_shapeType = (ShapeType)shapeType;
        }

        ImGui.SliderFloat("x Offset", ref m_transform.p.x, -2.0f, 2.0f, "%.2f");
        ImGui.SliderFloat("y Offset", ref m_transform.p.y, -2.0f, 2.0f, "%.2f");

        if (ImGui.SliderFloat("Angle", ref m_angle, -B2_PI, B2_PI, "%.2f"))
        {
            m_transform.q = b2MakeRot(m_angle);
        }

        ImGui.SliderFloat("Round", ref m_round, 0.0f, 0.4f, "%.1f");
        ImGui.Checkbox("Show Ids", ref m_showIds);
        ImGui.Checkbox("Show Separation", ref m_showSeparation);
        ImGui.Checkbox("Show Anchors", ref m_showAnchors);

        if (ImGui.Button("Reset"))
        {
            m_transform = b2Transform_identity;
            m_angle = 0.0f;
        }

        ImGui.Separator();

        ImGui.Text("mouse button 1: drag");
        ImGui.Text("mouse button 1 + shift: rotate");

        ImGui.PopItemWidth();
        ImGui.End();
    }

    public override void MouseDown(b2Vec2 p, int button, int mods)
    {
        if (button == (int)MouseButton.Left)
        {
            if (mods == 0 && m_rotating == false)
            {
                m_dragging = true;
                m_startPoint = p;
                m_basePosition = m_transform.p;
            }
            else if (0 != (mods & (uint)Keys.ShiftLeft) && m_dragging == false)
            {
                m_rotating = true;
                m_startPoint = p;
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
            m_transform.p.x = m_basePosition.x + (p.x - m_startPoint.x);
            m_transform.p.y = m_basePosition.y + (p.y - m_startPoint.y);
        }
        else if (m_rotating)
        {
            float dx = p.x - m_startPoint.x;
            m_angle = b2ClampFloat(m_baseAngle + 1.0f * dx, -B2_PI, B2_PI);
            m_transform.q = b2MakeRot(m_angle);
        }
    }

    void DrawManifold(ref b2Manifold manifold)
    {
        for (int i = 0; i < manifold.pointCount; ++i)
        {
            ref b2ManifoldPoint mp = ref manifold.points[i];

            b2Vec2 p1 = mp.point;
            b2Vec2 p2 = b2MulAdd(p1, 0.5f, manifold.normal);
            Draw.g_draw.DrawSegment(p1, p2, b2HexColor.b2_colorWhite);

            if (m_showAnchors)
            {
                Draw.g_draw.DrawPoint(p1, 5.0f, b2HexColor.b2_colorGreen);
            }
            else
            {
                Draw.g_draw.DrawPoint(p1, 5.0f, b2HexColor.b2_colorGreen);
            }

            if (m_showIds)
            {
                // uint indexA = mp.id >> 8;
                // uint indexB = 0xFF & mp.id;
                b2Vec2 p = new b2Vec2(p1.x + 0.05f, p1.y - 0.02f);
                Draw.g_draw.DrawString(p, "0x%04x", mp.id);
            }

            if (m_showSeparation)
            {
                b2Vec2 p = new b2Vec2(p1.x + 0.05f, p1.y + 0.03f);
                Draw.g_draw.DrawString(p, "%.3f", mp.separation);
            }
        }
    }

    public override void Step(Settings _)
    {
        b2HexColor color1 = b2HexColor.b2_colorYellow;
        b2HexColor color2 = b2HexColor.b2_colorMagenta;

        b2Transform transform1 = b2Transform_identity;
        b2Transform transform2 = m_transform;

        for (int i = 0; i < m_count; ++i)
        {
            b2ChainSegment segment = m_segments[i];
            b2Vec2 p1 = b2TransformPoint(ref transform1, segment.segment.point1);
            b2Vec2 p2 = b2TransformPoint(ref transform1, segment.segment.point2);
            Draw.g_draw.DrawSegment(p1, p2, color1);
            Draw.g_draw.DrawPoint(p1, 4.0f, color1);
        }

        // chain-segment vs circle
        if (m_shapeType == ShapeType.e_circleShape)
        {
            b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.5f);
            Draw.g_draw.DrawSolidCircle(ref transform2, circle.center, circle.radius, color2);

            for (int i = 0; i < m_count; ++i)
            {
                b2ChainSegment segment = m_segments[i];
                b2Manifold m = b2CollideChainSegmentAndCircle(segment, transform1, circle, transform2);
                DrawManifold(ref m);
            }
        }
        else if (m_shapeType == ShapeType.e_boxShape)
        {
            float h = 0.5f - m_round;
            b2Polygon rox = b2MakeRoundedBox(h, h, m_round);
            Draw.g_draw.DrawSolidPolygon(ref transform2, rox.vertices, rox.count, rox.radius, color2);

            for (int i = 0; i < m_count; ++i)
            {
                b2ChainSegment segment = m_segments[i];
                b2SimplexCache cache = new b2SimplexCache();
                b2Manifold m = b2CollideChainSegmentAndPolygon(segment, transform1, rox, transform2, ref cache);
                DrawManifold(ref m);
            }
        }
    }
}