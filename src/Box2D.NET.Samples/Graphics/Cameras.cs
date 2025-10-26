// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Samples.Graphics;

public static class Cameras
{
    public static Camera GetDefaultCamera()
    {
        var camera = new Camera();
        camera.center = new B2Vec2(0.0f, 20.0f);
        camera.zoom = 1.0f;
        camera.width = 1920.0f;
        camera.height = 1080.0f;
        return camera;
    }

    public static void ResetView(Camera camera)
    {
        camera.center = new B2Vec2(0.0f, 20.0f);
        camera.zoom = 1.0f;
    }

    public static B2Vec2 ConvertScreenToWorld(Camera camera, B2Vec2 ps)
    {
        float w = camera.width;
        float h = camera.height;
        float u = ps.X / w;
        float v = (h - ps.Y) / h;

        float ratio = w / h;
        B2Vec2 extents = new B2Vec2(camera.zoom * ratio, camera.zoom);

        B2Vec2 lower = b2Sub(camera.center, extents);
        B2Vec2 upper = b2Add(camera.center, extents);

        B2Vec2 pw = new B2Vec2((1.0f - u) * lower.X + u * upper.X, (1.0f - v) * lower.Y + v * upper.Y);
        return pw;
    }

    public static B2Vec2 ConvertWorldToScreen(Camera camera, B2Vec2 pw)
    {
        float w = camera.width;
        float h = camera.height;
        float ratio = w / h;

        B2Vec2 extents = new B2Vec2(camera.zoom * ratio, camera.zoom);

        B2Vec2 lower = b2Sub(camera.center, extents);
        B2Vec2 upper = b2Add(camera.center, extents);

        float u = (pw.X - lower.X) / (upper.X - lower.X);
        float v = (pw.Y - lower.Y) / (upper.Y - lower.Y);

        B2Vec2 ps = new B2Vec2(u * w, (1.0f - v) * h);
        return ps;
    }

    // Convert from world coordinates to normalized device coordinates.
    // http://www.songho.ca/opengl/gl_projectionmatrix.html
    // This also includes the view transform
    public static void BuildProjectionMatrix(Camera camera, Span<float> m, float zBias)
    {
        float ratio = (float)camera.width / (float)camera.height;
        B2Vec2 extents = new B2Vec2(camera.zoom * ratio, camera.zoom);

        B2Vec2 lower = b2Sub(camera.center, extents);
        B2Vec2 upper = b2Add(camera.center, extents);
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

        m[12] = -2.0f * camera.center.X / w;
        m[13] = -2.0f * camera.center.Y / h;
        m[14] = zBias;
        m[15] = 1.0f;
    }

    public static void MakeOrthographicMatrix(Span<float> m, float left, float right, float bottom, float top, float near, float far)
    {
        m[0] = 2.0f / (right - left);
        m[1] = 0.0f;
        m[2] = 0.0f;
        m[3] = 0.0f;

        m[4] = 0.0f;
        m[5] = 2.0f / (top - bottom);
        m[6] = 0.0f;
        m[7] = 0.0f;

        m[8] = 0.0f;
        m[9] = 0.0f;
        m[10] = -2.0f / (far - near);
        m[11] = 0.0f;

        m[12] = -(right + left) / (right - left);
        m[13] = -(top + bottom) / (top - bottom);
        m[14] = -(far + near) / (far - near);
        m[15] = 1.0f;
    }

    public static B2AABB GetViewBounds(Camera camera)
    {
        if (camera.height == 0.0f || camera.width == 0.0f)
        {
            B2AABB bounds = new B2AABB(b2Vec2_zero, b2Vec2_zero);
            return bounds;
        }

        {
            B2AABB bounds;
            bounds.lowerBound = ConvertScreenToWorld(camera, new B2Vec2(0.0f, (float)camera.height));
            bounds.upperBound = ConvertScreenToWorld(camera, new B2Vec2((float)camera.width, 0.0f));
            return bounds;
        }
    }
}
