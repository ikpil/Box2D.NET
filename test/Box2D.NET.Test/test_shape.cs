// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using NUnit.Framework;
using static Box2D.NET.geometry;
using static Box2D.NET.math_function;
using static Box2D.NET.hull;

namespace Box2D.NET.Test;

public class test_shape
{
    private b2Capsule capsule = new b2Capsule(new b2Vec2(-1.0f, 0.0f ), new b2Vec2(1.0f, 0.0f ), 1.0f);
    private b2Circle circle = new b2Circle(new b2Vec2( 1.0f, 0.0f ), 1.0f);
    private b2Polygon box = b2MakeBox(1.0f, 1.0f);
    private b2Segment segment = new b2Segment(new b2Vec2(0.0f, 1.0f), new b2Vec2(0.0f, -1.0f));

    public const int N = 4;


    [Test]
    public void ShapeMassTest()
    {
        {
            b2MassData md = b2ComputeCircleMass(circle, 1.0f);
            Assert.That(md.mass - B2_PI, Is.LessThan(FLT_EPSILON));
            Assert.That(md.center.x, Is.EqualTo(1.0f));
            Assert.That(md.center.y, Is.EqualTo(0.0f));
            Assert.That(md.rotationalInertia - 1.5f * B2_PI, Is.LessThan(FLT_EPSILON));
        }

        {
            float radius = capsule.radius;
            float length = b2Distance(capsule.center1, capsule.center2);

            b2MassData md = b2ComputeCapsuleMass(capsule, 1.0f);

            // Box that full contains capsule
            b2Polygon r = b2MakeBox(radius, radius + 0.5f * length);
            b2MassData mdr = b2ComputePolygonMass(r, 1.0f);

            // Approximate capsule using convex hull
            b2Vec2[] points = new b2Vec2[2 * N];
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

            b2Hull hull = b2ComputeHull(points, 2 * N);
            b2Polygon ac = b2MakePolygon(hull, 0.0f);
            b2MassData ma = b2ComputePolygonMass(ac, 1.0f);

            Assert.That(ma.mass < md.mass && md.mass < mdr.mass);
            Assert.That(ma.rotationalInertia < md.rotationalInertia && md.rotationalInertia < mdr.rotationalInertia);
        }

        {
            b2MassData md = b2ComputePolygonMass(box, 1.0f);
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
            b2AABB b = b2ComputeCircleAABB(circle, b2Transform_identity);
            Assert.That(b.lowerBound.x, Is.LessThan(FLT_EPSILON));
            Assert.That(b.lowerBound.y + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.x - 2.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.y - 1.0f, Is.LessThan(FLT_EPSILON));
        }

        {
            b2AABB b = b2ComputePolygonAABB(box, b2Transform_identity);
            Assert.That(b.lowerBound.x + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.lowerBound.y + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.x - 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.y - 1.0f, Is.LessThan(FLT_EPSILON));
        }

        {
            b2AABB b = b2ComputeSegmentAABB(segment, b2Transform_identity);
            Assert.That(b.lowerBound.x, Is.LessThan(FLT_EPSILON));
            Assert.That(b.lowerBound.y + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.x, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.y - 1.0f, Is.LessThan(FLT_EPSILON));
        }
    }

    [Test]
    public void PointInShapeTest()
    {
        b2Vec2 p1 = new b2Vec2(0.5f, 0.5f);
        b2Vec2 p2 = new b2Vec2(4.0f, -4.0f);

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
        b2RayCastInput input = new b2RayCastInput(new b2Vec2( -4.0f, 0.0f ), new b2Vec2( 8.0f, 0.0f ), 1.0f );

        {
            b2CastOutput output = b2RayCastCircle(input, circle);
            Assert.That(output.hit);
            Assert.That(output.normal.x + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(output.normal.y, Is.LessThan(FLT_EPSILON));
            Assert.That(output.fraction - 0.5f, Is.LessThan(FLT_EPSILON));
        }

        {
            b2CastOutput output = b2RayCastPolygon(input, box);
            Assert.That(output.hit);
            Assert.That(output.normal.x + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(output.normal.y, Is.LessThan(FLT_EPSILON));
            Assert.That(output.fraction - 3.0f / 8.0f, Is.LessThan(FLT_EPSILON));
        }

        {
            b2CastOutput output = b2RayCastSegment(input, segment, true);
            Assert.That(output.hit);
            Assert.That(output.normal.x + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(output.normal.y, Is.LessThan(FLT_EPSILON));
            Assert.That(output.fraction - 0.5f, Is.LessThan(FLT_EPSILON));
        }
    }
}