// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2AABBs;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET.Test;

public class B2CollisionTest
{
    [Test]
    public void AABBTest()
    {
        B2AABB a;
        a.lowerBound = new B2Vec2(-1.0f, -1.0f);
        a.upperBound = new B2Vec2(-2.0f, -2.0f);

        Assert.That(b2IsValidAABB(a), Is.EqualTo(false));

        a.upperBound = new B2Vec2(1.0f, 1.0f);
        Assert.That(b2IsValidAABB(a), Is.EqualTo(true));

        B2AABB b = new B2AABB(new B2Vec2(2.0f, 2.0f), new B2Vec2(4.0f, 4.0f));
        Assert.That(b2AABB_Overlaps(a, b), Is.EqualTo(false));
        Assert.That(b2AABB_Contains(a, b), Is.EqualTo(false));
    }


    [Test]
    public void AABBRayCastTest()
    {
        // Test AABB centered at origin with bounds [-1, -1] to [1, 1]
        B2AABB aabb = new B2AABB(new B2Vec2(-1.0f, -1.0f), new B2Vec2(1.0f, 1.0f));

        // Test 1: Ray hits AABB from left side
        {
            B2Vec2 p1 = new B2Vec2(-3.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(3.0f, 0.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.True);
            Assert.That(output.fraction - 1.0f / 3.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.X + 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.Y, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.point.X + 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.point.Y, Is.LessThanOrEqualTo(FLT_EPSILON));
        }

        // Test 2: Ray hits AABB from right side
        {
            B2Vec2 p1 = new B2Vec2(3.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(-3.0f, 0.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.True);
            Assert.That(output.fraction - 1.0f / 3.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.X - 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.Y, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.point.X - 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.point.Y, Is.LessThanOrEqualTo(FLT_EPSILON));
        }

        // Test 3: Ray hits AABB from bottom
        {
            B2Vec2 p1 = new B2Vec2(0.0f, -3.0f);
            B2Vec2 p2 = new B2Vec2(0.0f, 3.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.True);
            Assert.That(output.fraction - 1.0f / 3.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.X, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.Y + 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.point.X, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.point.Y + 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
        }

        // Test 4: Ray hits AABB from top
        {
            B2Vec2 p1 = new B2Vec2(0.0f, 3.0f);
            B2Vec2 p2 = new B2Vec2(0.0f, -3.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.True);
            Assert.That(output.fraction - 1.0f / 3.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.X, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.Y - 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.point.X, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.point.Y - 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
        }

        // Test 5: Ray misses AABB completely (parallel to x-axis)
        {
            B2Vec2 p1 = new B2Vec2(-3.0f, 2.0f);
            B2Vec2 p2 = new B2Vec2(3.0f, 2.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.False);
        }

        // Test 6: Ray misses AABB completely (parallel to y-axis)
        {
            B2Vec2 p1 = new B2Vec2(2.0f, -3.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 3.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.False);
        }

        // Test 7: Ray starts inside AABB
        {
            B2Vec2 p1 = new B2Vec2(0.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 0.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.False);
        }

        // Test 8: Ray hits corner of AABB (diagonal ray)
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, -2.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 2.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.True);
            Assert.That(output.fraction - 0.25f, Is.LessThanOrEqualTo(FLT_EPSILON));
            // Normal should be either (-1, 0) or (0, -1) depending on which edge is hit first
            Assert.That((output.normal.X == -1.0f && output.normal.Y == 0.0f) || (output.normal.X == 0.0f && output.normal.Y == -1.0f), Is.True);
        }

        // Test 9: Ray parallel to AABB edge but outside
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, 1.5f);
            B2Vec2 p2 = new B2Vec2(2.0f, 1.5f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.False);
        }

        // Test 10: Ray parallel to AABB edge and exactly on boundary
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, 1.0f);
            B2Vec2 p2 = new B2Vec2(2.0f, 1.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.True);
            Assert.That(output.fraction - 0.25f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.X + 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.Y, Is.LessThanOrEqualTo(FLT_EPSILON));
        }

        // Test 11: Very short ray that doesn't reach AABB
        {
            B2Vec2 p1 = new B2Vec2(-3.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(-2.5f, 0.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.False);
        }

        // Test 12: Zero-length ray (degenerate case)
        {
            B2Vec2 p1 = new B2Vec2(0.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(0.0f, 0.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.False);
        }

        // Test 13: Ray hits AABB at exact boundary condition (t = 1.0)
        {
            B2Vec2 p1 = new B2Vec2(-2.0f, 0.0f);
            B2Vec2 p2 = new B2Vec2(-1.0f, 0.0f);
            B2CastOutput output = b2AABB_RayCast(aabb, p1, p2);

            Assert.That(output.hit, Is.True);
            Assert.That(output.fraction - 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.X + 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.Y, Is.LessThanOrEqualTo(FLT_EPSILON));
        }

        // Test 14: Different AABB position (not centered at origin)
        {
            B2AABB offsetAABB = new B2AABB(new B2Vec2(2.0f, 3.0f), new B2Vec2(4.0f, 5.0f));
            B2Vec2 p1 = new B2Vec2(0.0f, 4.0f);
            B2Vec2 p2 = new B2Vec2(6.0f, 4.0f);
            B2CastOutput output = b2AABB_RayCast(offsetAABB, p1, p2);

            Assert.That(output.hit, Is.True);
            Assert.That(output.fraction - 1.0f / 3.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.X + 1.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.normal.Y, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.point.X - 2.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
            Assert.That(output.point.Y - 4.0f, Is.LessThanOrEqualTo(FLT_EPSILON));
        }
    }
}