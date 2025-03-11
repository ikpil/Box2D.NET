// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Silk.NET.GLFW;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Constants;
using static Box2D.NET.Shared.RandomSupports;

namespace Box2D.NET.Samples.Samples.Geometries;

public class ConvexHull : Sample
{
    public const int e_count = B2_MAX_POLYGON_VERTICES;

    B2Vec2[] m_points = new B2Vec2[B2_MAX_POLYGON_VERTICES];
    int m_count;
    int m_generation;
    bool m_auto;
    bool m_bulk;
    private static readonly int SampleIndex = SampleFactory.Shared.RegisterSample("Geometry", "Convex Hull", Create);

    private static Sample Create(SampleAppContext ctx, Settings settings)
    {
        return new ConvexHull(ctx, settings);
    }


    public ConvexHull(SampleAppContext ctx, Settings settings)
        : base(ctx, settings)
    {
        if (settings.restart == false)
        {
            m_context.g_camera.m_center = new B2Vec2(0.5f, 0.0f);
            m_context.g_camera.m_zoom = 25.0f * 0.3f;
        }

        m_generation = 0;
        m_auto = false;
        m_bulk = false;
        Generate();
    }

    void Generate()
    {
#if FALSE
		m_points[0] = new B2Vec2( 5.65314484f, 0.204832315f );
		m_points[1] = new B2Vec2(-5.65314484f, -0.204832315f );
		m_points[2] = new B2Vec2(2.34463644f, 1.15731204f );
		m_points[3] = new B2Vec2(0.0508846045f, 3.23230696f );
		m_points[4] = new B2Vec2(-5.65314484f, -0.204832315f );
		m_points[5] = new B2Vec2(-5.65314484f, -0.204832315f );
		m_points[6] = new B2Vec2(3.73758054f, -1.11098099f );
		m_points[7] = new B2Vec2(1.33504069f, -4.43795443f );

		m_count = e_count;
#elif FALSE
		m_points[0] = new B2Vec2( -0.328125f, 0.179688f );
		m_points[1] = new B2Vec2( -0.203125f, 0.304688f );
		m_points[2] = new B2Vec2( 0.171875f, 0.304688f );
		m_points[3] = new B2Vec2( 0.359375f, 0.117188f );
		m_points[4] = new B2Vec2( 0.359375f, -0.195313f );
		m_points[5] = new B2Vec2( 0.234375f, -0.320313f );
		m_points[6] = new B2Vec2( -0.265625f, -0.257813f );
		m_points[7] = new B2Vec2( -0.328125f, -0.132813f );

		b2Hull hull = b2ComputeHull( m_points, 8 );
		bool valid = b2ValidateHull( &hull );
		if ( valid == false )
		{
			Debug.Assert( valid );
		}

		m_count = e_count;
#else

        float angle = B2_PI * RandomFloat();
        B2Rot r = b2MakeRot(angle);

        B2Vec2 lowerBound = new B2Vec2(-4.0f, -4.0f);
        B2Vec2 upperBound = new B2Vec2(4.0f, 4.0f);

        for (int i = 0; i < e_count; ++i)
        {
            float x = 10.0f * RandomFloat();
            float y = 10.0f * RandomFloat();

            // Clamp onto a square to help create collinearities.
            // This will stress the convex hull algorithm.
            B2Vec2 v = b2Clamp(new B2Vec2(x, y), lowerBound, upperBound);
            m_points[i] = b2RotateVector(r, v);
        }

        m_count = e_count;
#endif

        m_generation += 1;
    }

    public override void Keyboard(Keys key)
    {
        switch ((Keys)key)
        {
            case Keys.A:
                m_auto = !m_auto;
                break;

            case Keys.B:
                m_bulk = !m_bulk;
                break;

            case Keys.G:
                Generate();
                break;

            default:
                break;
        }
    }

    public override void Step(Settings settings)
    {
        base.Step(settings);

        m_context.g_draw.DrawString(5, m_textLine, "Options: generate(g), auto(a), bulk(b)");
        m_textLine += m_textIncrement;

        B2Hull hull = new B2Hull();
        bool valid = false;
        float milliseconds = 0.0f;

        if (m_bulk)
        {
#if ENABLED
            // defect hunting
            for (int i = 0; i < 10000; ++i)
            {
                Generate();
                hull = b2ComputeHull(m_points, m_count);
                if (hull.count == 0)
                {
                    // m_bulk = false;
                    // break;
                    continue;
                }

                valid = b2ValidateHull(hull);
                if (valid == false || m_bulk == false)
                {
                    m_bulk = false;
                    break;
                }
            }
#else
			// performance
			Generate();
			b2Timer timer;
			for ( int i = 0; i < 1000000; ++i )
			{
				hull = b2ComputeHull( m_points, m_count );
			}
			valid = hull.count > 0;
			milliseconds = timer.GetMilliseconds();
#endif
        }
        else
        {
            if (m_auto)
            {
                Generate();
            }

            hull = b2ComputeHull(m_points, m_count);
            if (hull.count > 0)
            {
                valid = b2ValidateHull(hull);
                if (valid == false)
                {
                    m_auto = false;
                }
            }
        }

        if (valid == false)
        {
            m_context.g_draw.DrawString(5, m_textLine, $"generation = {m_generation}, FAILED");
            m_textLine += m_textIncrement;
        }
        else
        {
            m_context.g_draw.DrawString(5, m_textLine, $"generation = {m_generation}, count = {hull.count}");
            m_textLine += m_textIncrement;
        }

        if (milliseconds > 0.0f)
        {
            m_context.g_draw.DrawString(5, m_textLine, $"milliseconds = {milliseconds:G}");
            m_textLine += m_textIncrement;
        }

        m_textLine += m_textIncrement;

        m_context.g_draw.DrawPolygon(hull.points, hull.count, B2HexColor.b2_colorGray);

        for (int i = 0; i < m_count; ++i)
        {
            m_context.g_draw.DrawPoint(m_points[i], 5.0f, B2HexColor.b2_colorBlue);
            m_context.g_draw.DrawString(b2Add(m_points[i], new B2Vec2(0.1f, 0.1f)), $"{i}");
        }

        for (int i = 0; i < hull.count; ++i)
        {
            m_context.g_draw.DrawPoint(hull.points[i], 6.0f, B2HexColor.b2_colorGreen);
        }
    }
}