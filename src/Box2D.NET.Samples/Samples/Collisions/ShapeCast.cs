// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Silk.NET.GLFW;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Distances;

namespace Box2D.NET.Samples.Samples.Collisions;

public class ShapeCast : Sample
{
    private static readonly int SampleShapeCast = SampleFactory.Shared.RegisterSample("Collision", "Shape Cast", Create);

    public const int e_vertexCount = 8;

    private B2Vec2[] m_vAs = new B2Vec2[B2_MAX_POLYGON_VERTICES];
    private int m_countA;
    private float m_radiusA;

    private B2Vec2[] m_vBs = new B2Vec2[B2_MAX_POLYGON_VERTICES];
    private int m_countB;
    private float m_radiusB;

    private B2Transform m_transformA;
    private B2Transform m_transformB;
    private B2Vec2 m_translationB;
    private bool m_rayDrag;

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new ShapeCast(ctx, settings);
    }


    public ShapeCast(SampleAppContext ctx, Settings settings) : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.camera.m_center = new B2Vec2(-1.5f, 1.0f);
            m_context.camera.m_zoom = 25.0f * 0.2f;
        }

#if FALSE
        // box swept against a triangle
        m_vAs[0] = {-0.5f, 1.0f};
        m_vAs[1] = {0.5f, 1.0f};
        m_vAs[2] = {0.0f, 0.0f};
        m_countA = 3;
        m_radiusA = 0.0f;

        m_vBs[0] = {-0.5f, -0.5f};
        m_vBs[1] = {0.5f, -0.5f};
        m_vBs[2] = {0.5f, 0.5f};
        m_vBs[3] = {-0.5f, 0.5f};
        m_countB = 4;
        m_radiusB = 0.0f;

        m_transformA.p = {0.0f, 0.25f};
        m_transformA.q = b2Rot_identity;
        m_transformB.p = {-4.0f, 0.0f};
        m_transformB.q = b2Rot_identity;
        m_translationB = {8.0f, 0.0f};
#elif ENABLED
        // box swept against a segment
        m_vAs[0] = new B2Vec2(-2.0f, 0.0f);
        m_vAs[1] = new B2Vec2(2.0f, 0.0f);
        m_countA = 2;
        m_radiusA = 0.0f;

        m_vBs[0] = new B2Vec2(-0.25f, -0.25f);
        m_vBs[1] = new B2Vec2(0.25f, -0.25f);
        m_vBs[2] = new B2Vec2(0.25f, 0.25f);
        m_vBs[3] = new B2Vec2(-0.25f, 0.25f);
        m_countB = 4;
        m_radiusB = 0.25f;

        m_transformA.p = new B2Vec2(0.0f, 0.0f);
        m_transformA.q = b2MakeRot(0.25f * B2_PI);
        m_transformB.p = new B2Vec2(-8.0f, 0.0f);
        m_transformB.q = b2Rot_identity;
        m_translationB = new B2Vec2(8.0f, 0.0f);
#elif FALSE
        // A point swept against a box
        m_vAs[0] = { -0.5f, -0.5f };
        m_vAs[1] = { 0.5f, -0.5f };
        m_vAs[2] = { 0.5f, 0.5f };
        m_vAs[3] = { -0.5f, 0.5f };
        m_countA = 4;
        m_radiusA = 0.0f;

        m_vBs[0] = { 0.0f, 0.0f };
        m_countB = 1;
        m_radiusB = 0.0f;

        m_transformA.p = { 0.0f, 0.0f };
        m_transformA.q = b2Rot_identity;
        m_transformB.p = { -1.0f, 0.0f };
        m_transformB.q = b2Rot_identity;
        m_translationB = { 1.0f, 0.0f };
#elif FALSE
        m_vAs[0] = { 0.0f, 0.0f };
        m_countA = 1;
        m_radiusA = 0.5f;

        m_vBs[0] = { 0.0f, 0.0f };
        m_countB = 1;
        m_radiusB = 0.5f;

        m_transformA.p = { 0.0f, 0.25f };
        m_transformA.q = b2Rot_identity;
        m_transformB.p = { -4.0f, 0.0f };
        m_transformB.q = b2Rot_identity;
        m_translationB = { 8.0f, 0.0f };
#else
        m_vAs[0] = { 0.0f, 0.0f };
        m_vAs[1] = { 2.0f, 0.0f };
        m_countA = 2;
        m_radiusA = 0.0f;

        m_vBs[0] = { 0.0f, 0.0f };
        m_countB = 1;
        m_radiusB = 0.25f;

        // Initial overlap
        m_transformA.p = b2Vec2_zero;
        m_transformA.q = b2Rot_identity;
        m_transformB.p = { -0.244360745f, 0.05999358f };
        m_transformB.q = b2Rot_identity;
        m_translationB = { 0.0f, 0.0399999991f };
#endif

        m_rayDrag = false;
    }


    public override void MouseDown(B2Vec2 p, MouseButton button, KeyModifiers mods)
    {
        if (button == (int)MouseButton.Left)
        {
            m_transformB.p = p;
            m_rayDrag = true;
        }
    }

    public override void MouseUp(B2Vec2 p, MouseButton button)
    {
        if (button == (int)MouseButton.Left)
        {
            m_rayDrag = false;
        }
    }

    public override void MouseMove(B2Vec2 p)
    {
        if (m_rayDrag)
        {
            m_translationB = b2Sub(p, m_transformB.p);
        }
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);
    }

    public override void Draw(Settings settings)
    {
        B2ShapeCastPairInput input = new B2ShapeCastPairInput();
        input.proxyA = b2MakeProxy(m_vAs, m_countA, m_radiusA);
        input.proxyB = b2MakeProxy(m_vBs, m_countB, m_radiusB);
        input.transformA = m_transformA;
        input.transformB = m_transformB;
        input.translationB = m_translationB;
        input.maxFraction = 1.0f;

        B2CastOutput output = b2ShapeCast(ref input);

        B2Transform transformB2;
        transformB2.q = m_transformB.q;
        transformB2.p = b2MulAdd(m_transformB.p, output.fraction, input.translationB);

        B2DistanceInput distanceInput;
        distanceInput.proxyA = b2MakeProxy(m_vAs, m_countA, m_radiusA);
        distanceInput.proxyB = b2MakeProxy(m_vBs, m_countB, m_radiusB);
        distanceInput.transformA = m_transformA;
        distanceInput.transformB = transformB2;
        distanceInput.useRadii = false;
        B2SimplexCache distanceCache = new B2SimplexCache();
        distanceCache.count = 0;
        B2DistanceOutput distanceOutput = b2ShapeDistance(ref distanceCache, ref distanceInput, null, 0);

        m_context.draw.DrawString(5, m_textLine, $"hit = {output.hit}, iters = {output.iterations}, lambda = {output.fraction:g}, distance = {distanceOutput.distance:g}");
        m_textLine += m_textIncrement;

        B2Vec2[] vertices = new B2Vec2[B2_MAX_POLYGON_VERTICES];

        for (int i = 0; i < m_countA; ++i)
        {
            vertices[i] = b2TransformPoint(ref m_transformA, m_vAs[i]);
        }

        var b2TransformZero = b2Transform_identity;
        if (m_countA == 1)
        {
            if (m_radiusA > 0.0f)
            {
                m_context.draw.DrawSolidCircle(ref b2TransformZero, vertices[0], m_radiusA, B2HexColor.b2_colorLightGray);
            }
            else
            {
                m_context.draw.DrawPoint(vertices[0], 5.0f, B2HexColor.b2_colorLightGray);
            }
        }
        else
        {
            m_context.draw.DrawSolidPolygon(ref b2TransformZero, vertices, m_countA, m_radiusA, B2HexColor.b2_colorLightGray);
        }

        for (int i = 0; i < m_countB; ++i)
        {
            vertices[i] = b2TransformPoint(ref m_transformB, m_vBs[i]);
        }

        if (m_countB == 1)
        {
            if (m_radiusB > 0.0f)
            {
                m_context.draw.DrawSolidCircle(ref b2TransformZero, vertices[0], m_radiusB, B2HexColor.b2_colorGreen);
            }
            else
            {
                m_context.draw.DrawPoint(vertices[0], 5.0f, B2HexColor.b2_colorGreen);
            }
        }
        else
        {
            m_context.draw.DrawSolidPolygon(ref b2TransformZero, vertices, m_countB, m_radiusB, B2HexColor.b2_colorGreen);
        }

        for (int i = 0; i < m_countB; ++i)
        {
            vertices[i] = b2TransformPoint(ref transformB2, m_vBs[i]);
        }

        if (m_countB == 1)
        {
            if (m_radiusB > 0.0f)
            {
                m_context.draw.DrawSolidCircle(ref b2TransformZero, vertices[0], m_radiusB, B2HexColor.b2_colorOrange);
            }
            else
            {
                m_context.draw.DrawPoint(vertices[0], 5.0f, B2HexColor.b2_colorOrange);
            }
        }
        else
        {
            m_context.draw.DrawSolidPolygon(ref b2TransformZero, vertices, m_countB, m_radiusB, B2HexColor.b2_colorOrange);
        }

        if (output.hit)
        {
            B2Vec2 p1 = output.point;
            m_context.draw.DrawPoint(p1, 10.0f, B2HexColor.b2_colorRed);
            B2Vec2 p2 = b2MulAdd(p1, 1.0f, output.normal);
            m_context.draw.DrawSegment(p1, p2, B2HexColor.b2_colorRed);
        }

        m_context.draw.DrawSegment(m_transformB.p, b2Add(m_transformB.p, m_translationB), B2HexColor.b2_colorGray);
    }
}