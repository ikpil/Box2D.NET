using System;
using System.Diagnostics;
using Box2D.NET.Primitives;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;
using static Box2D.NET.joint;
using static Box2D.NET.id;
using static Box2D.NET.hull;
using static Box2D.NET.geometry;
using static Box2D.NET.types;
using static Box2D.NET.math_function;
using static Box2D.NET.body;
using static Box2D.NET.shape;
using static Box2D.NET.wheel_joint;
using static Box2D.NET.world;
using static Box2D.NET.mouse_joint;


namespace Box2D.NET.Samples;

public class Camera
{
    public b2Vec2 m_center;
    public float m_zoom;
    public int m_width;
    public int m_height;

    public Camera()
    {
        m_width = 1280;
        m_height = 800;
        ResetView();
    }

    public void ResetView()
    {
        m_center = new b2Vec2(0.0f, 20.0f);
        m_zoom = 1.0f;
    }

    public b2Vec2 ConvertScreenToWorld(b2Vec2 ps)
    {
        float w = m_width;
        float h = m_height;
        float u = ps.x / w;
        float v = (h - ps.y) / h;

        float ratio = w / h;
        b2Vec2 extents = new b2Vec2(m_zoom * ratio, m_zoom);

        b2Vec2 lower = b2Sub(m_center, extents);
        b2Vec2 upper = b2Add(m_center, extents);

        b2Vec2 pw = new b2Vec2((1.0f - u) * lower.x + u * upper.x, (1.0f - v) * lower.y + v * upper.y);
        return pw;
    }

    public b2Vec2 ConvertWorldToScreen(b2Vec2 pw)
    {
        float w = m_width;
        float h = m_height;
        float ratio = w / h;

        b2Vec2 extents = new b2Vec2(m_zoom * ratio, m_zoom);

        b2Vec2 lower = b2Sub(m_center, extents);
        b2Vec2 upper = b2Add(m_center, extents);

        float u = (pw.x - lower.x) / (upper.x - lower.x);
        float v = (pw.y - lower.y) / (upper.y - lower.y);

        b2Vec2 ps = new b2Vec2(u * w, (1.0f - v) * h);
        return ps;
    }

    // Convert from world coordinates to normalized device coordinates.
    // http://www.songho.ca/opengl/gl_projectionmatrix.html
    // This also includes the view transform
    public void BuildProjectionMatrix(Span<float> m, float zBias)
    {
        float ratio = (float)m_width / (float)m_height;
        b2Vec2 extents = new b2Vec2(m_zoom * ratio, m_zoom);

        b2Vec2 lower = b2Sub(m_center, extents);
        b2Vec2 upper = b2Add(m_center, extents);
        float w = upper.x - lower.x;
        float h = upper.y - lower.y;

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

        m[12] = -2.0f * m_center.x / w;
        m[13] = -2.0f * m_center.y / h;
        m[14] = zBias;
        m[15] = 1.0f;
    }

    public b2AABB GetViewBounds()
    {
        b2AABB bounds;
        bounds.lowerBound = ConvertScreenToWorld(new b2Vec2(0.0f, (float)m_height));
        bounds.upperBound = ConvertScreenToWorld(new b2Vec2((float)m_width, 0.0f));
        return bounds;
    }
}