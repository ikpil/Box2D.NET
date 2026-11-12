// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using NUnit.Framework;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Distances;

namespace Box2D.NET.Test;

// Port of upstream test_large_world.c. Box2D.NET is single precision only, so the
// BOX2D_DOUBLE_PRECISION sections and the recording test are not ported. The origin runs
// still exercise the origin plumbing (tree lift, per shape re-centering, output compose).
public class B2LargeWorldTest
{
    private const int PYRAMID_BODY_COUNT = 9;

    private struct PyramidResult
    {
        public int sleepStep;
        public B2Vec2[] rel;
    }

    // Read a body center of mass relative to a base position.
    private static B2Vec2 BodyRelativeCenter(B2WorldId worldId, B2BodyId bodyId, B2Pos baseVec)
    {
        B2World world = b2GetWorldFromId(worldId);
        B2Body body = b2GetBodyFullId(world, bodyId);
        B2BodySim sim = b2GetBodySim(world, body);
        return b2SubPos(sim.center, baseVec);
    }

    // Build a stepped pyramid of unit boxes on a base position, settle it, and report the sleep frame
    // and the settled centers relative to the base.
    private static PyramidResult RunPyramid(B2Pos baseVec)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = 1;
        B2WorldId worldId = b2CreateWorld(worldDef);

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = baseVec;
            B2BodyId groundId = b2CreateBody(worldId, bodyDef);

            // Ground top surface at baseY + 0.5
            B2Polygon groundBox = b2MakeBox(10.0f, 0.5f);
            B2ShapeDef groundShapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, groundShapeDef, groundBox);
        }

        // Each box is 1 m, centers on integer offsets. Row 0 rests on the ground, rows stack directly
        // above so the configuration is stable and sleeps quickly.
        B2Vec2[] offsets =
        {
            new B2Vec2(-2.0f, 1.0f), new B2Vec2(-1.0f, 1.0f), new B2Vec2(0.0f, 1.0f), new B2Vec2(1.0f, 1.0f), new B2Vec2(2.0f, 1.0f),
            new B2Vec2(-1.0f, 2.0f), new B2Vec2(0.0f, 2.0f), new B2Vec2(1.0f, 2.0f), new B2Vec2(0.0f, 3.0f),
        };

        B2BodyId[] bodyIds = new B2BodyId[PYRAMID_BODY_COUNT];
        B2Polygon box = b2MakeBox(0.5f, 0.5f);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        for (int i = 0; i < PYRAMID_BODY_COUNT; ++i)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Pos(baseVec.X + offsets[i].X, baseVec.Y + offsets[i].Y);
            bodyIds[i] = b2CreateBody(worldId, bodyDef);
            b2CreatePolygonShape(bodyIds[i], shapeDef, box);
        }

        PyramidResult result = new PyramidResult { sleepStep = -1, rel = new B2Vec2[PYRAMID_BODY_COUNT] };

        for (int step = 0; step < 250; ++step)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);

            if (result.sleepStep < 0)
            {
                B2BodyEvents events = b2World_GetBodyEvents(worldId);
                if (events.moveCount == 0 && b2World_GetAwakeBodyCount(worldId) == 0)
                {
                    result.sleepStep = step;
                }
            }
        }

        for (int i = 0; i < PYRAMID_BODY_COUNT; ++i)
        {
            result.rel[i] = BodyRelativeCenter(worldId, bodyIds[i], baseVec);
        }

        b2DestroyWorld(worldId);
        return result;
    }

    [Test]
    public void LargeWorldPyramidTest()
    {
        PyramidResult origin = RunPyramid(new B2Pos(0.0f, 0.0f));
        Assert.That(origin.sleepStep, Is.GreaterThan(0));
    }

    // Fire a bullet at a thin wall and report where it ends up relative to the base. With continuous
    // collision the bullet stops at the near face, without it the bullet tunnels far past the wall.
    private static float RunBullet(B2Pos baseVec)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = 1;
        B2WorldId worldId = b2CreateWorld(worldDef);

        // Thin tall wall centered on the base
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_staticBody;
        bodyDef.position = baseVec;
        B2BodyId wallId = b2CreateBody(worldId, bodyDef);
        B2Polygon wall = b2MakeBox(0.05f, 5.0f);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(wallId, shapeDef, wall);

        // Bullet fired at the wall from the far side. At 200 m/s it crosses ~3.3 m per step, so without
        // continuous collision it passes the 0.1 m wall in the first step.
        bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.isBullet = true;
        bodyDef.gravityScale = 0.0f;
        bodyDef.position = new B2Pos(baseVec.X + 10.0f, baseVec.Y);
        bodyDef.linearVelocity = new B2Vec2(-200.0f, 0.0f);
        B2BodyId bulletId = b2CreateBody(worldId, bodyDef);
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.1f);
        shapeDef = b2DefaultShapeDef();
        b2CreateCircleShape(bulletId, shapeDef, circle);

        for (int step = 0; step < 30; ++step)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
        }

        float relX = BodyRelativeCenter(worldId, bulletId, baseVec).X;
        b2DestroyWorld(worldId);
        return relX;
    }

    [Test]
    public void LargeWorldBulletTest()
    {
        // At the origin the bullet must stop at the near face. Wall face at 0.05 plus the 0.1 bullet
        // radius puts the rest position near 0.15.
        float relX = RunBullet(new B2Pos(0.0f, 0.0f));
        Assert.That(relX > 0.0f && relX < 0.5f, Is.True);
    }

    // Cast a ray at a unit box on the base and report the hit point relative to the base.
    private static B2Vec2 RunRayCast(B2Pos baseVec)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = 1;
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = baseVec;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        B2Polygon box = b2MakeBox(0.5f, 0.5f);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(bodyId, shapeDef, box);

        // Ray from 5 m left of the box, traveling 10 m right. Hits the left face at base + {-0.5, 0}.
        B2Pos origin = new B2Pos(baseVec.X - 5.0f, baseVec.Y);
        B2Vec2 translation = new B2Vec2(10.0f, 0.0f);
        B2RayResult result = b2World_CastRayClosest(worldId, origin, translation, b2DefaultQueryFilter());

        // A miss leaves the point at the origin, which the caller's position check rejects
        B2Vec2 rel = b2SubPos(result.point, baseVec);
        b2DestroyWorld(worldId);
        return rel;
    }

    [Test]
    public void LargeWorldRayCastTest()
    {
        B2Vec2 rel = RunRayCast(new B2Pos(0.0f, 0.0f));
        Assert.That(MathF.Abs(rel.X + 0.5f), Is.LessThanOrEqualTo(1e-4f));
        Assert.That(MathF.Abs(rel.Y), Is.LessThanOrEqualTo(1e-4f));
    }

    private class OriginQueryData
    {
        public int overlapCount;
        public B2Pos castPoint;
        public float castFraction;
        public float moverFraction;
        public int planeCount;
        public bool insidePoint;
        public B2Pos shapeRayPoint;
        public bool shapeRayHit;
    }

    private static bool OriginOverlapFcn(B2ShapeId id, object ctx)
    {
        OriginQueryData data = (OriginQueryData)ctx;
        data.overlapCount += 1;
        return true;
    }

    private static float OriginCastFcn(B2ShapeId id, B2Pos point, B2Vec2 normal, float fraction, ref OriginQueryData data)
    {
        data.castPoint = point;
        data.castFraction = fraction;
        return fraction;
    }

    private static bool OriginPlaneFcn(B2ShapeId id, ref B2PlaneResult plane, object ctx)
    {
        OriginQueryData data = (OriginQueryData)ctx;
        data.planeCount += 1;
        return true;
    }

    // Issue every origin taking query against a unit box on the base, with all geometry relative to
    // the base.
    private static OriginQueryData RunOriginQueries(B2Pos @base)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = 1;
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = @base;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        B2Polygon box = b2MakeBox(0.5f, 0.5f);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2ShapeId shapeId = b2CreatePolygonShape(bodyId, shapeDef, box);

        B2QueryFilter filter = b2DefaultQueryFilter();
        OriginQueryData data = new OriginQueryData();
        data.castFraction = 1.0f;

        // Overlap a small circle centered on the box
        B2Vec2 center = new B2Vec2(0.0f, 0.0f);
        B2ShapeProxy overlapProxy = b2MakeProxy(center, 1, 0.1f);
        b2World_OverlapShape(worldId, @base, ref overlapProxy, filter, OriginOverlapFcn, data);

        // Cast a small circle at the left face. Center stops at -0.6, hit point on the face at -0.5.
        B2Vec2 start = new B2Vec2(-5.0f, 0.0f);
        B2ShapeProxy castProxy = b2MakeProxy(start, 1, 0.1f);
        b2World_CastShape(worldId, @base, ref castProxy, new B2Vec2(10.0f, 0.0f), filter, OriginCastFcn, data);

        // Mover cast at the box
        B2Capsule mover = new B2Capsule(new B2Vec2(-5.0f, -0.2f), new B2Vec2(-5.0f, 0.2f), 0.3f);
        data.moverFraction = b2World_CastMover(worldId, @base, mover, new B2Vec2(10.0f, 0.0f), filter);

        // Mover overlapping the box gathers planes
        B2Capsule touching = new B2Capsule(new B2Vec2(-0.9f, -0.2f), new B2Vec2(-0.9f, 0.2f), 0.5f);
        b2World_CollideMover(worldId, @base, touching, filter, OriginPlaneFcn, data);

        // Shape level queries at the base
        data.insidePoint = b2Shape_TestPoint(shapeId, @base);

        B2WorldCastOutput rayOutput = b2Shape_CastRay(shapeId, b2OffsetPos(@base, new B2Vec2(-5.0f, 0.0f)), new B2Vec2(10.0f, 0.0f));
        data.shapeRayHit = rayOutput.hit;
        data.shapeRayPoint = rayOutput.point;

        b2DestroyWorld(worldId);
        return data;
    }

    [Test]
    public void LargeWorldOriginQueryTest()
    {
        OriginQueryData origin = RunOriginQueries(b2Pos_zero);
        Assert.That(origin.overlapCount, Is.EqualTo(1));
        Assert.That(origin.castFraction, Is.LessThan(1.0f));
        Assert.That(origin.moverFraction, Is.LessThan(1.0f));
        Assert.That(origin.planeCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(origin.insidePoint, Is.True);
        Assert.That(origin.shapeRayHit, Is.True);

        B2Vec2 castRel = b2SubPos(origin.castPoint, b2Pos_zero);
        Assert.That(MathF.Abs(castRel.X + 0.5f), Is.LessThanOrEqualTo(1e-3f));
        B2Vec2 rayRel = b2SubPos(origin.shapeRayPoint, b2Pos_zero);
        Assert.That(MathF.Abs(rayRel.X + 0.5f), Is.LessThanOrEqualTo(1e-3f));
    }
}
