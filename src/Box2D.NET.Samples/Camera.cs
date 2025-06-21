// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2MathFunction;


namespace Box2D.NET.Samples;

public class Camera
{
    public B2Vec2 m_center;
    public float m_zoom;
    public float m_width;
    public float m_height;

    public Camera()
    {
        m_width = 1920;
        m_height = 1080;
        
        ResetView();
    }

    public void ResetView()
    {
        m_center = new B2Vec2(0.0f, 20.0f);
        m_zoom = 1.0f;
    }

    public B2Vec2 ConvertScreenToWorld(B2Vec2 ps)
    {
        float w = m_width;
        float h = m_height;
        float u = ps.X / w;
        float v = (h - ps.Y) / h;

        float ratio = w / h;
        B2Vec2 extents = new B2Vec2(m_zoom * ratio, m_zoom);

        B2Vec2 lower = b2Sub(m_center, extents);
        B2Vec2 upper = b2Add(m_center, extents);

        B2Vec2 pw = new B2Vec2((1.0f - u) * lower.X + u * upper.X, (1.0f - v) * lower.Y + v * upper.Y);
        return pw;
    }

    public B2Vec2 ConvertWorldToScreen(B2Vec2 pw)
    {
        float w = m_width;
        float h = m_height;
        float ratio = w / h;

        B2Vec2 extents = new B2Vec2(m_zoom * ratio, m_zoom);

        B2Vec2 lower = b2Sub(m_center, extents);
        B2Vec2 upper = b2Add(m_center, extents);

        float u = (pw.X - lower.X) / (upper.X - lower.X);
        float v = (pw.Y - lower.Y) / (upper.Y - lower.Y);

        B2Vec2 ps = new B2Vec2(u * w, (1.0f - v) * h);
        return ps;
    }

    // Convert from world coordinates to normalized device coordinates.
    // http://www.songho.ca/opengl/gl_projectionmatrix.html
    // This also includes the view transform
    public void BuildProjectionMatrix(Span<float> m, float zBias)
    {
        float ratio = (float)m_width / (float)m_height;
        B2Vec2 extents = new B2Vec2(m_zoom * ratio, m_zoom);

        B2Vec2 lower = b2Sub(m_center, extents);
        B2Vec2 upper = b2Add(m_center, extents);
        float w = upper.X - lower.X;
        float h = upper.Y - lower.Y;

        m[0] = 2.0f / w;
        m[1] = 0.0f;
        m[2] = 0.0f;
        m[3] = 0.0f;

        m[4] = 0.0f;
        m[5] = 2.0f / h;
        m[6] = 0.0f;
        m[7] = 0.0f;

        m[8] = 0.0f;
        m[9] = 0.0f;
        m[10] = -1.0f;
        m[11] = 0.0f;

        m[12] = -2.0f * m_center.X / w;
        m[13] = -2.0f * m_center.Y / h;
        m[14] = zBias;
        m[15] = 1.0f;
    }

    public B2AABB GetViewBounds()
    {
        B2AABB bounds;
        bounds.lowerBound = ConvertScreenToWorld(new B2Vec2(0.0f, (float)m_height));
        bounds.upperBound = ConvertScreenToWorld(new B2Vec2((float)m_width, 0.0f));
        return bounds;
    }
}
