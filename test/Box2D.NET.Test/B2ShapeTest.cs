﻿// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
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
            B2MassData md = b2ComputeCircleMass(ref circle, 1.0f);
            Assert.That(md.mass - B2_PI, Is.LessThan(FLT_EPSILON));
            Assert.That(md.center.X, Is.EqualTo(1.0f));
            Assert.That(md.center.Y, Is.EqualTo(0.0f));
            Assert.That(md.rotationalInertia - 1.5f * B2_PI, Is.LessThan(FLT_EPSILON));
        }

        {
            float radius = capsule.radius;
            float length = b2Distance(capsule.center1, capsule.center2);

            B2MassData md = b2ComputeCapsuleMass(ref capsule, 1.0f);

            // Box that full contains capsule
            B2Polygon r = b2MakeBox(radius, radius + 0.5f * length);
            B2MassData mdr = b2ComputePolygonMass(ref r, 1.0f);

            // Approximate capsule using convex hull
            B2Vec2[] points = new B2Vec2[2 * N];
            float d = B2_PI / (N - 1.0f);
            float angle = -0.5f * B2_PI;
            for (int i = 0; i < N; ++i)
            {
                points[i].X = 1.0f + radius * MathF.Cos(angle);
                points[i].Y = radius * MathF.Sin(angle);
                angle += d;
            }

            angle = 0.5f * B2_PI;
            for (int i = N; i < 2 * N; ++i)
            {
                points[i].X = -1.0f + radius * MathF.Cos(angle);
                points[i].Y = radius * MathF.Sin(angle);
                angle += d;
            }

            B2Hull hull = b2ComputeHull(points, 2 * N);
            B2Polygon ac = b2MakePolygon(ref hull, 0.0f);
            B2MassData ma = b2ComputePolygonMass(ref ac, 1.0f);

            Assert.That(ma.mass < md.mass && md.mass < mdr.mass);
            Assert.That(ma.rotationalInertia < md.rotationalInertia && md.rotationalInertia < mdr.rotationalInertia);
        }

        {
            B2MassData md = b2ComputePolygonMass(ref box, 1.0f);
            Assert.That(md.mass - 4.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(md.center.X, Is.LessThan(FLT_EPSILON));
            Assert.That(md.center.Y, Is.LessThan(FLT_EPSILON));
            Assert.That(md.rotationalInertia - 8.0f / 3.0f, Is.LessThanOrEqualTo(2.0f * FLT_EPSILON));
        }
    }

    [Test]
    public void ShapeAABBTest()
    {
        {
            B2AABB b = b2ComputeCircleAABB(ref circle, b2Transform_identity);
            Assert.That(b.lowerBound.X, Is.LessThan(FLT_EPSILON));
            Assert.That(b.lowerBound.Y + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.X - 2.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.Y - 1.0f, Is.LessThan(FLT_EPSILON));
        }

        {
            B2AABB b = b2ComputePolygonAABB(ref box, b2Transform_identity);
            Assert.That(b.lowerBound.X + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.lowerBound.Y + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.X - 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.Y - 1.0f, Is.LessThan(FLT_EPSILON));
        }

        {
            B2AABB b = b2ComputeSegmentAABB(ref segment, b2Transform_identity);
            Assert.That(b.lowerBound.X, Is.LessThan(FLT_EPSILON));
            Assert.That(b.lowerBound.Y + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.X, Is.LessThan(FLT_EPSILON));
            Assert.That(b.upperBound.Y - 1.0f, Is.LessThan(FLT_EPSILON));
        }
    }

    [Test]
    public void PointInShapeTest()
    {
        B2Vec2 p1 = new B2Vec2(0.5f, 0.5f);
        B2Vec2 p2 = new B2Vec2(4.0f, -4.0f);

        {
            bool hit;
            hit = b2PointInCircle(ref circle, p1);
            Assert.That(hit, Is.EqualTo(true));
            hit = b2PointInCircle(ref circle, p2);
            Assert.That(hit, Is.EqualTo(false));
        }

        {
            bool hit;
            hit = b2PointInPolygon(ref box, p1);
            Assert.That(hit, Is.EqualTo(true));
            hit = b2PointInPolygon(ref box, p2);
            Assert.That(hit, Is.EqualTo(false));
        }
    }

    [Test]
    public void RayCastShapeTest()
    {
        B2RayCastInput input = new B2RayCastInput(new B2Vec2(-4.0f, 0.0f), new B2Vec2(8.0f, 0.0f), 1.0f);

        {
            B2CastOutput output = b2RayCastCircle(ref circle, ref input);
            Assert.That(output.hit);
            Assert.That(output.normal.X + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(output.normal.Y, Is.LessThan(FLT_EPSILON));
            Assert.That(output.fraction - 0.5f, Is.LessThan(FLT_EPSILON));
        }

        {
            B2CastOutput output = b2RayCastPolygon(ref box, ref input);
            Assert.That(output.hit);
            Assert.That(output.normal.X + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(output.normal.Y, Is.LessThan(FLT_EPSILON));
            Assert.That(output.fraction - 3.0f / 8.0f, Is.LessThan(FLT_EPSILON));
        }

        {
            B2CastOutput output = b2RayCastSegment(ref segment, ref input, true);
            Assert.That(output.hit);
            Assert.That(output.normal.X + 1.0f, Is.LessThan(FLT_EPSILON));
            Assert.That(output.normal.Y, Is.LessThan(FLT_EPSILON));
            Assert.That(output.fraction - 0.5f, Is.LessThan(FLT_EPSILON));
        }
    }
}
