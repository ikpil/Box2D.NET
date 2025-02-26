// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using Box2D.NET.Primitives;
using NUnit.Framework;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Hulls;

namespace Box2D.NET.Test;

public class B2ShapeTest
{
    private B2Capsule capsule = new B2Capsule(new B2Vec2(-1.0f, 0.0f), new B2Vec2(1.0f, 0.0f), 1.0f);
    private B2Circle circle = new B2Circle(new B2Vec2(1.0f, 0.0f), 1.0f);
    private B2Polygon box = b2MakeBox(1.0f, 1.0f);
    private B2Segment segment = new B2Segment(new B2Vec2(0.0f, 1.0f), new B2Vec2(0.0f, -1.0f));

    public const int N = 4;


    [Test]
    public void ShapeMassTest()
    {
        {
            B2MassData md = b2ComputeCircleMass(circle, 1.0f);
            Assert.That(md.mass - B2_PI, Is.LessThan(FLT_EPSILON));
            Assert.That(md.center.x, Is.EqualTo(1.0f));
            Assert.That(md.center.y, Is.EqualTo(0.0f));
            Assert.That(md.rotationalInertia - 1.5f * B2_PI, Is.LessThan(FLT_EPSILON));
        }

        {
            float radius = capsule.radius;
            float length = b2Distance(capsule.center1, capsule.center2);

            B2MassData md = b2ComputeCapsuleMass(capsule, 1.0f);

            // Box that full contains capsule
            B2Polygon r = b2MakeBox(radius, radius + 0.5f * length);
            B2MassData mdr = b2ComputePolygonMass(r, 1.0f);

            // Approximate capsule using convex hull
            B2Vec2[] points = new B2Vec2[2 * N];
            float d = B2_PI / (N - 1.0f);
            float angle = -0.5f * B2_PI;
            for (int i = 0; i < N; ++i)
            {
                points[i].x = 1.0f + radius * MathF.Cos(angle);
                points[i].y = radius * MathF.Sin(angle);
                angle += d;
            }

            angle = 0.5f * B2_PI;
            for (int i = N; i < 2 * N; ++i)
            {
                points[i].x = -1.0f + radius * MathF.Cos(angle);
                points[i].y = radius * MathF.Sin(angle);
                angle += d;
            }

            B2Hull hull = b2ComputeHull(points, 2 * N);
            B2Polygon ac = b2MakePolygon(hull, 0.0f);
            B2MassData ma = b2ComputePolygonMass(ac, 1.0f);

            Assert.That(ma.mass < md.mass && md.mass < mdr.mass);
            Assert.That(ma.rotationalInertia < md.rotationalInertia && md.rotationalInertia < mdr.rotationalInertia);
        }

        {
            B2MassData md = b2ComputePolygonMass(box, 1.0f);
            Assert.That(md.mass - 4.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(md.center.x, Is.LessThan(FLT_EPSILON));
            Assert.That(md.center.y, Is.LessThan(FLT_EPSILON));
            Assert.That(md.rotationalInertia - 8.0f / 3.0f, Is.LessThanOrEqualTo(2.0f * FLT_EPSILON));
        }
    }

    [Test]
    public void ShapeAABBTest()
    {
        {
            B2AABB b = b2ComputeCircleAABB(circle, b2Transform_identity);
            Assert.That(b.lowerBound.x, Is.LessThan(FLT_EPSILON));
            Assert.That(b.lowerBound.y + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.x - 2.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.y - 1.0f, Is.LessThan(FLT_EPSILON));
        }

        {
            B2AABB b = b2ComputePolygonAABB(box, b2Transform_identity);
            Assert.That(b.lowerBound.x + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.lowerBound.y + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.x - 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.y - 1.0f, Is.LessThan(FLT_EPSILON));
        }

        {
            B2AABB b = b2ComputeSegmentAABB(segment, b2Transform_identity);
            Assert.That(b.lowerBound.x, Is.LessThan(FLT_EPSILON));
            Assert.That(b.lowerBound.y + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.x, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.y - 1.0f, Is.LessThan(FLT_EPSILON));
        }
    }

    [Test]
    public void PointInShapeTest()
    {
        B2Vec2 p1 = new B2Vec2(0.5f, 0.5f);
        B2Vec2 p2 = new B2Vec2(4.0f, -4.0f);

        {
            bool hit;
            hit = b2PointInCircle(p1, circle);
            Assert.That(hit, Is.EqualTo(true));
            hit = b2PointInCircle(p2, circle);
            Assert.That(hit, Is.EqualTo(false));
        }

        {
            bool hit;
            hit = b2PointInPolygon(p1, box);
            Assert.That(hit, Is.EqualTo(true));
            hit = b2PointInPolygon(p2, box);
            Assert.That(hit, Is.EqualTo(false));
        }
    }

    [Test]
    public void RayCastShapeTest()
    {
        B2RayCastInput input = new B2RayCastInput(new B2Vec2(-4.0f, 0.0f), new B2Vec2(8.0f, 0.0f), 1.0f);

        {
            B2CastOutput output = b2RayCastCircle(input, circle);
            Assert.That(output.hit);
            Assert.That(output.normal.x + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(output.normal.y, Is.LessThan(FLT_EPSILON));
            Assert.That(output.fraction - 0.5f, Is.LessThan(FLT_EPSILON));
        }

        {
            B2CastOutput output = b2RayCastPolygon(ref input, box);
            Assert.That(output.hit);
            Assert.That(output.normal.x + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(output.normal.y, Is.LessThan(FLT_EPSILON));
            Assert.That(output.fraction - 3.0f / 8.0f, Is.LessThan(FLT_EPSILON));
        }

        {
            B2CastOutput output = b2RayCastSegment(ref input, segment, true);
            Assert.That(output.hit);
            Assert.That(output.normal.x + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(output.normal.y, Is.LessThan(FLT_EPSILON));
            Assert.That(output.fraction - 0.5f, Is.LessThan(FLT_EPSILON));
        }
    }
}
