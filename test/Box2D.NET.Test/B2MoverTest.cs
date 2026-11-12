// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using NUnit.Framework;
using static Box2D.NET.B2Movers;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Constants;

namespace Box2D.NET.Test;

public class B2MoverTest
{
    private const float FLT_EPSILON = 1.1920929e-7f;
    private const float FLT_MAX = float.MaxValue;

    private static B2CollisionPlane MakePlane(B2Vec2 normal, float pushLimit)
    {
        return new B2CollisionPlane(new B2Plane(normal, 0.0f), pushLimit, 0.0f, true);
    }

    private static void EnsureSmall(float value, float tolerance)
    {
        Assert.That(MathF.Abs(value), Is.LessThanOrEqualTo(tolerance));
    }

    [Test]
    public void NoPlanesTest()
    {
        B2Vec2 targetDelta = new B2Vec2(0.5f, -0.25f);
        B2PlaneSolverResult result = b2SolvePlanes(targetDelta, Span<B2CollisionPlane>.Empty, 0);

        Assert.That(result.delta.X, Is.EqualTo(targetDelta.X));
        Assert.That(result.delta.Y, Is.EqualTo(targetDelta.Y));
        Assert.That(result.iterationCount, Is.EqualTo(0));
    }

    [Test]
    public void SolvePlaneTest()
    {
        B2CollisionPlane[] plane = { MakePlane(new B2Vec2(0.0f, 1.0f), FLT_MAX) };
        B2PlaneSolverResult result = b2SolvePlanes(new B2Vec2(0.0f, -1.0f), plane, 1);

        // An overshoot would eject the mover out the far side of the floor
        Assert.That(result.delta.Y, Is.LessThanOrEqualTo(0.0f));

        // The mover comes to rest a slop under the surface, not through it
        Assert.That(result.delta.Y, Is.GreaterThan(-10.0f * B2_LINEAR_SLOP));
        Assert.That(result.delta.X, Is.EqualTo(0.0f));
        Assert.That(plane[0].push, Is.GreaterThan(0.0f));
    }

    [Test]
    public void PushLimitTest()
    {
        B2CollisionPlane[] plane = { MakePlane(new B2Vec2(0.0f, 1.0f), 0.1f) };
        B2PlaneSolverResult result = b2SolvePlanes(new B2Vec2(0.0f, -1.0f), plane, 1);

        // Soft collision, so the mover sinks once the limit is spent
        EnsureSmall(plane[0].push - 0.1f, FLT_EPSILON);
        EnsureSmall(result.delta.Y + 0.9f, FLT_EPSILON);
    }

    [Test]
    public void CornerTest()
    {
        B2CollisionPlane[] planes =
        {
            MakePlane(new B2Vec2(0.0f, 1.0f), FLT_MAX),
            MakePlane(new B2Vec2(1.0f, 0.0f), FLT_MAX),
        };

        B2PlaneSolverResult result = b2SolvePlanes(new B2Vec2(-1.0f, -1.0f), planes, planes.Length);

        // Both planes hold at once, rather than the second relaxing the first
        for (int i = 0; i < planes.Length; ++i)
        {
            float separation = b2PlaneSeparation(planes[i].plane, result.delta);
            EnsureSmall(separation + B2_LINEAR_SLOP, B2_LINEAR_SLOP);
            Assert.That(planes[i].push, Is.GreaterThan(0.0f));
        }
    }

    [Test]
    public void WedgeTest()
    {
        // An acute wedge takes several passes to settle, unlike a square corner
        B2CollisionPlane[] planes =
        {
            MakePlane(new B2Vec2(0.0f, 1.0f), FLT_MAX),
            MakePlane(new B2Vec2(0.8f, 0.6f), FLT_MAX),
        };

        B2PlaneSolverResult result = b2SolvePlanes(new B2Vec2(-1.0f, -1.0f), planes, planes.Length);

        // Settling means resting on both faces. Stopping early leaves the mover shoved
        // clear of one of them, which reads as a large positive separation
        for (int i = 0; i < planes.Length; ++i)
        {
            float separation = b2PlaneSeparation(planes[i].plane, result.delta);
            EnsureSmall(separation + B2_LINEAR_SLOP, B2_LINEAR_SLOP);
        }
    }

    [Test]
    public void ClipVectorTest()
    {
        B2CollisionPlane[] plane = { MakePlane(new B2Vec2(0.0f, 1.0f), FLT_MAX) };
        plane[0].push = 0.5f;

        B2Vec2 v = b2ClipVector(new B2Vec2(1.0f, -1.0f), plane, 1);
        EnsureSmall(v.X - 1.0f, FLT_EPSILON);
        EnsureSmall(v.Y, FLT_EPSILON);

        // Separating velocity passes through untouched
        v = b2ClipVector(new B2Vec2(1.0f, 1.0f), plane, 1);
        EnsureSmall(v.X - 1.0f, FLT_EPSILON);
        EnsureSmall(v.Y - 1.0f, FLT_EPSILON);

        plane[0].clipVelocity = false;
        v = b2ClipVector(new B2Vec2(1.0f, -1.0f), plane, 1);
        EnsureSmall(v.Y + 1.0f, FLT_EPSILON);

        // Zero push means the solver never touched this plane
        plane[0].clipVelocity = true;
        plane[0].push = 0.0f;
        v = b2ClipVector(new B2Vec2(1.0f, -1.0f), plane, 1);
        EnsureSmall(v.Y + 1.0f, FLT_EPSILON);
    }
}
