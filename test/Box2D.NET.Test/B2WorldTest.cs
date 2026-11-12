// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.Shared.Benchmarks;

namespace Box2D.NET.Test;

public class B2WorldTest
{
    // This is a simple example of building and running a simulation
    // using Box2D. Here we create a large ground box and a small dynamic
    // box.
    // There are no graphics for this example. Box2D is meant to be used
    // with your rendering engine in your game engine.
    [Test]
    public void HelloWorld()
    {
        // Construct a world object, which will hold and simulate the rigid bodies.
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = new B2Vec2(0.0f, -10.0f);

        B2WorldId worldId = b2CreateWorld(worldDef);
        Assert.That(b2World_IsValid(worldId));

        // Define the ground body.
        B2BodyDef groundBodyDef = b2DefaultBodyDef();
        groundBodyDef.position = b2ToPos(new B2Vec2(0.0f, -10.0f));

        // Call the body factory which allocates memory for the ground body
        // from a pool and creates the ground box shape (also from a pool).
        // The body is also added to the world.
        B2BodyId groundId = b2CreateBody(worldId, groundBodyDef);
        Assert.That(b2Body_IsValid(groundId));

        // Define the ground box shape. The extents are the half-widths of the box.
        B2Polygon groundBox = b2MakeBox(50.0f, 10.0f);

        // Add the box shape to the ground body.
        B2ShapeDef groundShapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(groundId, groundShapeDef, groundBox);

        // Define the dynamic body. We set its position and call the body factory.
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = b2ToPos(new B2Vec2(0.0f, 4.0f));

        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        // Define another box shape for our dynamic body.
        B2Polygon dynamicBox = b2MakeBox(1.0f, 1.0f);

        // Define the dynamic body shape
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        // Set the box density to be non-zero, so it will be dynamic.
        shapeDef.density = 1.0f;

        // Override the default friction.
        shapeDef.material.friction = 0.3f;

        // Add the shape to the body.
        b2CreatePolygonShape(bodyId, shapeDef, dynamicBox);

        // Prepare for simulation. Typically we use a time step of 1/60 of a
        // second (60Hz) and 4 sub-steps. This provides a high quality simulation
        // in most game scenarios.
        float timeStep = 1.0f / 60.0f;
        int subStepCount = 4;

        B2Pos position = b2Body_GetPosition(bodyId);
        B2Rot rotation = b2Body_GetRotation(bodyId);

        // This is our little game loop.
        for (int i = 0; i < 90; ++i)
        {
            // Instruct the world to perform a single step of simulation.
            // It is generally best to keep the time step and iterations fixed.
            b2World_Step(worldId, timeStep, subStepCount);

            // Now print the position and angle of the body.
            position = b2Body_GetPosition(bodyId);
            rotation = b2Body_GetRotation(bodyId);

            // Console.Write("%4.2f %4.2f %4.2f\n", position.x, position.y, b2Rot_GetAngle(rotation));
        }

        // When the world destructor is called, all bodies and joints are freed. This can
        // create orphaned ids, so be careful about your world management.
        b2DestroyWorld(worldId);

        Assert.That(b2AbsFloat(position.X), Is.LessThan(0.01f));
        Assert.That(b2AbsFloat(position.Y - 1.00f), Is.LessThan(0.01f));
        Assert.That(b2AbsFloat(b2Rot_GetAngle(rotation)), Is.LessThan(0.01f));
    }

    [Test]
    public void EmptyWorld()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);
        Assert.That(b2World_IsValid(worldId), Is.EqualTo(true));

        float timeStep = 1.0f / 60.0f;
        int subStepCount = 1;

        for (int i = 0; i < 60; ++i)
        {
            b2World_Step(worldId, timeStep, subStepCount);
        }

        b2DestroyWorld(worldId);

        Assert.That(b2World_IsValid(worldId), Is.EqualTo(false));
    }

    [Test]
    public void MaxCapacityTracksWorldUsage()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.capacity.staticBodyCount = 4;
        worldDef.capacity.dynamicBodyCount = 8;
        worldDef.capacity.staticShapeCount = 4;
        worldDef.capacity.dynamicShapeCount = 8;
        worldDef.capacity.contactCount = 16;

        B2WorldId worldId = b2CreateWorld(worldDef);
        Assert.That(b2World_IsValid(worldId), Is.EqualTo(true));

        B2BodyDef groundBodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(worldId, groundBodyDef);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(groundId, shapeDef, b2MakeBox(10.0f, 1.0f));

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = b2ToPos(new B2Vec2(0.0f, 4.0f));

        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        b2CreatePolygonShape(bodyId, shapeDef, b2MakeBox(0.5f, 0.5f));

        b2World_Step(worldId, 1.0f / 60.0f, 4);

        B2Capacity capacity = b2World_GetMaxCapacity(worldId);
        Assert.That(capacity.staticBodyCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(capacity.dynamicBodyCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(capacity.staticShapeCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(capacity.dynamicShapeCount, Is.GreaterThanOrEqualTo(1));

        b2DestroyWorld(worldId);
    }

    public const int BODY_COUNT = 10;

    [Test]
    public void DestroyAllBodiesWorld()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);
        Assert.That(b2World_IsValid(worldId), Is.EqualTo(true));

        int count = 0;
        bool creating = true;

        B2BodyId[] bodyIds = new B2BodyId[BODY_COUNT];
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        B2Polygon square = b2MakeSquare(0.5f);

        for (int i = 0; i < 2 * BODY_COUNT + 10; ++i)
        {
            if (creating)
            {
                if (count < BODY_COUNT)
                {
                    bodyIds[count] = b2CreateBody(worldId, bodyDef);

                    B2ShapeDef shapeDef = b2DefaultShapeDef();
                    b2CreatePolygonShape(bodyIds[count], shapeDef, square);
                    count += 1;
                }
                else
                {
                    creating = false;
                }
            }
            else if (count > 0)
            {
                b2DestroyBody(bodyIds[count - 1]);
                bodyIds[count - 1] = b2_nullBodyId;
                count -= 1;
            }

            b2World_Step(worldId, 1.0f / 60.0f, 3);
        }

        B2Counters counters = b2World_GetCounters(worldId);
        Assert.That(counters.bodyCount, Is.EqualTo(0));

        b2DestroyWorld(worldId);

        Assert.That(b2World_IsValid(worldId), Is.EqualTo(false));
    }

    [Test]
    public void TestIsValid()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);
        Assert.That(b2World_IsValid(worldId));

        B2BodyDef bodyDef = b2DefaultBodyDef();

        B2BodyId bodyId1 = b2CreateBody(worldId, bodyDef);
        Assert.That(b2Body_IsValid(bodyId1), Is.EqualTo(true));

        B2BodyId bodyId2 = b2CreateBody(worldId, bodyDef);
        Assert.That(b2Body_IsValid(bodyId2), Is.EqualTo(true));

        b2DestroyBody(bodyId1);
        Assert.That(b2Body_IsValid(bodyId1), Is.EqualTo(false));

        b2DestroyBody(bodyId2);
        Assert.That(b2Body_IsValid(bodyId2), Is.EqualTo(false));

        b2DestroyWorld(worldId);

        Assert.That(b2World_IsValid(worldId), Is.EqualTo(false));
        Assert.That(b2Body_IsValid(bodyId2), Is.EqualTo(false));
        Assert.That(b2Body_IsValid(bodyId1), Is.EqualTo(false));
    }

    public const int WORLD_COUNT = B2_MAX_WORLDS / 2;

    [Test]
    public void TestWorldRecycle()
    {
        B2_ASSERT(WORLD_COUNT > 0, "world count");

        int count = 100;

        B2WorldId[] worldIds = new B2WorldId[WORLD_COUNT];

        for (int i = 0; i < count; ++i)
        {
            B2WorldDef worldDef = b2DefaultWorldDef();
            for (int j = 0; j < WORLD_COUNT; ++j)
            {
                worldIds[j] = b2CreateWorld(worldDef);
                Assert.That(b2World_IsValid(worldIds[j]), Is.EqualTo(true), $"i({i}) j({j})");

                B2BodyDef bodyDef = b2DefaultBodyDef();
                b2CreateBody(worldIds[j], bodyDef);
            }

            for (int j = 0; j < WORLD_COUNT; ++j)
            {
                float timeStep = 1.0f / 60.0f;
                int subStepCount = 1;

                for (int k = 0; k < 10; ++k)
                {
                    b2World_Step(worldIds[j], timeStep, subStepCount);
                }
            }

            for (int j = WORLD_COUNT - 1; j >= 0; --j)
            {
                b2DestroyWorld(worldIds[j]);
                Assert.That(b2World_IsValid(worldIds[j]), Is.EqualTo(false), $"i({i}) j({j})");
                worldIds[j] = b2_nullWorldId;
            }
        }
    }

    public static bool CustomFilter(B2ShapeId shapeIdA, B2ShapeId shapeIdB, object context)
    {
        B2_UNUSED(shapeIdA);
        B2_UNUSED(shapeIdB);
        Assert.That(context, Is.EqualTo(null));
        return true;
    }

    public static void PreSolveStatic(B2ShapeId shapeIdA, B2ShapeId shapeIdB, ref B2Manifold manifold, object context)
    {
        B2_UNUSED(shapeIdA);
        B2_UNUSED(shapeIdB);
        B2_UNUSED(manifold);
        B2_UNUSED(context);
    }

    // This test is here to ensure all API functions link correctly.
    [Test]
    public void TestWorldCoverage()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();

        B2WorldId worldId = b2CreateWorld(worldDef);
        Assert.That(b2World_IsValid(worldId));

        b2World_EnableSleeping(worldId, true);
        b2World_EnableSleeping(worldId, false);
        bool flag = b2World_IsSleepingEnabled(worldId);
        Assert.That(flag, Is.EqualTo(false));

        b2World_EnableContinuous(worldId, false);
        b2World_EnableContinuous(worldId, true);
        flag = b2World_IsContinuousEnabled(worldId);
        Assert.That(flag, Is.EqualTo(true));

        b2World_SetRestitutionThreshold(worldId, 0.0f);
        b2World_SetRestitutionThreshold(worldId, 2.0f);
        float value = b2World_GetRestitutionThreshold(worldId);
        Assert.That(value, Is.EqualTo(2.0f));

        b2World_SetHitEventThreshold(worldId, 0.0f);
        b2World_SetHitEventThreshold(worldId, 100.0f);
        value = b2World_GetHitEventThreshold(worldId);
        Assert.That(value, Is.EqualTo(100.0f));

        b2World_SetCustomFilterCallback(worldId, CustomFilter, null);
        b2World_SetPreSolveCallback(worldId, PreSolveStatic, null, null);

        B2Vec2 g = new B2Vec2(1.0f, 2.0f);
        b2World_SetGravity(worldId, g);
        B2Vec2 v = b2World_GetGravity(worldId);
        Assert.That(v.X, Is.EqualTo(g.X));
        Assert.That(v.Y, Is.EqualTo(g.Y));

        B2ExplosionDef explosionDef = b2DefaultExplosionDef();
        b2World_Explode(worldId, explosionDef);

        b2World_SetContactTuning(worldId, 10.0f, 2.0f, 4.0f);
        b2World_GetContactTuning(worldId, out float hertz, out float dampingRatio, out float pushSpeed);
        Assert.That(hertz == 10.0f && dampingRatio == 2.0f && pushSpeed == 4.0f, Is.True);

        b2World_SetMaxLinearSpeed(worldId, 10.0f);
        value = b2World_GetMaxLinearSpeed(worldId);
        Assert.That(value, Is.EqualTo(10.0f));

        b2World_EnableWarmStarting(worldId, true);
        flag = b2World_IsWarmStartingEnabled(worldId);
        Assert.That(flag, Is.EqualTo(true));

        int count = b2World_GetAwakeBodyCount(worldId);
        Assert.That(count, Is.EqualTo(0));

        b2World_SetUserData(worldId, B2UserData.Double(value));
        var userData = b2World_GetUserData(worldId);
        Assert.That((float)userData.GetDouble(), Is.EqualTo(value));

        b2World_Step(worldId, 1.0f, 1);

        b2DestroyWorld(worldId);
    }

    [Test]
    public void TestSensor()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        // Wall from x = 1 to x = 2
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_staticBody;
        bodyDef.position.X = 1.5f;
        bodyDef.position.Y = 11.0f;
        B2BodyId wallId = b2CreateBody(worldId, bodyDef);
        B2Polygon box = b2MakeBox(0.5f, 10.0f);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.enableSensorEvents = true;
        b2CreatePolygonShape(wallId, shapeDef, box);

        // Bullet fired towards the wall
        bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.isBullet = true;
        bodyDef.gravityScale = 0.0f;
        bodyDef.position = b2ToPos(new B2Vec2(7.39814f, 4.0f));
        bodyDef.linearVelocity = new B2Vec2(-20.0f, 0.0f);
        B2BodyId bulletId = b2CreateBody(worldId, bodyDef);
        shapeDef = b2DefaultShapeDef();
        shapeDef.isSensor = true;
        shapeDef.enableSensorEvents = true;
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.1f);
        b2CreateCircleShape(bulletId, shapeDef, circle);

        int beginCount = 0;
        int endCount = 0;

        while (true)
        {
            float timeStep = 1.0f / 60.0f;
            int subStepCount = 4;
            b2World_Step(worldId, timeStep, subStepCount);

            B2Pos bulletPos = b2Body_GetPosition(bulletId);
            //Console.Write( "Bullet pos: %g %g\n", bulletPos.x, bulletPos.y );

            B2SensorEvents events = b2World_GetSensorEvents(worldId);

            if (events.beginCount > 0)
            {
                beginCount += 1;
            }

            if (events.endCount > 0)
            {
                endCount += 1;
            }

            if (bulletPos.X < -1.0f)
            {
                break;
            }
        }

        b2DestroyWorld(worldId);

        Assert.That(beginCount, Is.EqualTo(1));
        Assert.That(endCount, Is.EqualTo(1));
    }

    [Test]
    public void TestSetWorkerCount()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = 1;
        B2WorldId worldId = b2CreateWorld(worldDef);
        Assert.That(b2World_IsValid(worldId));
        Assert.That(b2World_GetWorkerCount(worldId), Is.EqualTo(1));

        var junkyardData = CreateJunkyard(worldId);
        StepJunkyard(junkyardData, worldId, 1);

        b2World_SetWorkerCount(worldId, 4);
        Assert.That(b2World_GetWorkerCount(worldId), Is.EqualTo(4));

        StepJunkyard(junkyardData, worldId, 2);

        b2World_SetWorkerCount(worldId, 4);
        Assert.That(b2World_GetWorkerCount(worldId), Is.EqualTo(4));

        StepJunkyard(junkyardData, worldId, 3);

        b2World_SetWorkerCount(worldId, 0);
        Assert.That(b2World_GetWorkerCount(worldId), Is.EqualTo(1));

        StepJunkyard(junkyardData, worldId, 4);

        b2World_SetWorkerCount(worldId, -5);
        Assert.That(b2World_GetWorkerCount(worldId), Is.EqualTo(1));

        StepJunkyard(junkyardData, worldId, 5);

        b2World_SetWorkerCount(worldId, B2_MAX_WORKERS + 10);
        Assert.That(b2World_GetWorkerCount(worldId), Is.EqualTo(B2_MAX_WORKERS));

        StepJunkyard(junkyardData, worldId, 2);

        b2DestroyWorld(worldId);
    }

    [Test]
    public void ChainSegmentShapeTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = new B2Vec2(0.0f, -10.0f);
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(worldId, bodyDef);

        B2ChainSegment cs = new B2ChainSegment();
        cs.ghost1 = new B2Vec2(2.0f, 0.0f);
        cs.segment.point1 = new B2Vec2(1.0f, 0.0f);
        cs.segment.point2 = new B2Vec2(-1.0f, 0.0f);
        cs.ghost2 = new B2Vec2(-2.0f, 0.0f);
        cs.chainId = 99;

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2ShapeId orphanShape = b2CreateChainSegmentShape(groundId, shapeDef, cs);
        Assert.That(B2_IS_NON_NULL(orphanShape));

        Assert.That(b2Shape_GetType(orphanShape), Is.EqualTo(B2ShapeType.b2_chainSegmentShape));

        B2ChainId parentChain = b2Shape_GetParentChain(orphanShape);
        Assert.That(B2_IS_NULL(parentChain));

        B2ChainSegment got = b2Shape_GetChainSegment(orphanShape);
        Assert.That(got.ghost1.X - cs.ghost1.X, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got.ghost1.Y - cs.ghost1.Y, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got.segment.point1.X - cs.segment.point1.X, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got.segment.point1.Y - cs.segment.point1.Y, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got.segment.point2.X - cs.segment.point2.X, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got.segment.point2.Y - cs.segment.point2.Y, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got.ghost2.X - cs.ghost2.X, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got.ghost2.Y - cs.ghost2.Y, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got.chainId, Is.EqualTo(B2_NULL_INDEX));

        B2BodyDef dynamicDef = b2DefaultBodyDef();
        dynamicDef.type = B2BodyType.b2_dynamicBody;
        dynamicDef.position = b2ToPos(new B2Vec2(0.0f, 2.0f));
        B2BodyId circleBodyId = b2CreateBody(worldId, dynamicDef);
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        B2ShapeDef circleShapeDef = b2DefaultShapeDef();
        b2CreateCircleShape(circleBodyId, circleShapeDef, circle);

        for (int i = 0; i < 120; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
        }

        B2Pos circlePos = b2Body_GetPosition(circleBodyId);
        Assert.That(circlePos.Y, Is.GreaterThan(0.0f));

        B2ChainSegment cs2 = new B2ChainSegment();
        cs2.ghost1 = new B2Vec2(3.0f, 0.0f);
        cs2.segment.point1 = new B2Vec2(2.0f, 0.0f);
        cs2.segment.point2 = new B2Vec2(-2.0f, 0.0f);
        cs2.ghost2 = new B2Vec2(-3.0f, 0.0f);
        cs2.chainId = B2_NULL_INDEX;

        b2Shape_SetChainSegment(orphanShape, cs2);

        B2ChainSegment got2 = b2Shape_GetChainSegment(orphanShape);
        Assert.That(got2.segment.point1.X - cs2.segment.point1.X, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got2.segment.point1.Y - cs2.segment.point1.Y, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got2.segment.point2.X - cs2.segment.point2.X, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got2.segment.point2.Y - cs2.segment.point2.Y, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got2.ghost1.X - cs2.ghost1.X, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got2.ghost1.Y - cs2.ghost1.Y, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got2.ghost2.X - cs2.ghost2.X, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got2.ghost2.Y - cs2.ghost2.Y, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got2.chainId, Is.EqualTo(B2_NULL_INDEX));

        B2ChainId parentChain2 = b2Shape_GetParentChain(orphanShape);
        Assert.That(B2_IS_NULL(parentChain2));

        B2BodyId convBody = b2CreateBody(worldId, bodyDef);
        B2Circle convCircle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.25f);
        B2ShapeId convShape = b2CreateCircleShape(convBody, shapeDef, convCircle);
        Assert.That(b2Shape_GetType(convShape), Is.EqualTo(B2ShapeType.b2_circleShape));

        b2Shape_SetChainSegment(convShape, cs2);

        Assert.That(b2Shape_GetType(convShape), Is.EqualTo(B2ShapeType.b2_chainSegmentShape));
        B2ChainSegment got3 = b2Shape_GetChainSegment(convShape);
        Assert.That(got3.ghost1.X - cs2.ghost1.X, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got3.ghost1.Y - cs2.ghost1.Y, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got3.ghost2.X - cs2.ghost2.X, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got3.ghost2.Y - cs2.ghost2.Y, Is.EqualTo(0.0f).Within(1e-5f));
        Assert.That(got3.chainId, Is.EqualTo(B2_NULL_INDEX));

        B2ChainId parentChain3 = b2Shape_GetParentChain(convShape);
        Assert.That(B2_IS_NULL(parentChain3));

        b2DestroyShape(orphanShape, true);
        b2DestroyWorld(worldId);
    }

    [Test]
    public void DeferredMassFlagSyncTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.updateBodyMass = false;

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.5f);
        b2CreateCircleShape(bodyId, shapeDef, circle);

        b2Body_UpdateMassFromShapes(bodyId);

        b2World_Step(worldId, 1.0f / 60.0f, 4);

        b2DestroyWorld(worldId);
    }

    [Test]
    public void EnableSleepFlagSyncTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.enableSleep = false;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        Assert.That(b2Body_IsSleepingEnabled(bodyId), Is.False);

        b2Body_EnableSleeping(bodyId, true);
        Assert.That(b2Body_IsSleepingEnabled(bodyId), Is.True);

        b2World_Step(worldId, 1.0f / 60.0f, 4);

        b2DestroyWorld(worldId);
    }

    [Test]
    public void EnableContactRecyclingTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;

        // Default is enabled
        B2BodyId bodyA = b2CreateBody(worldId, bodyDef);
        Assert.That(b2Body_IsContactRecyclingEnabled(bodyA), Is.True);

        b2Body_EnableContactRecycling(bodyA, false);
        Assert.That(b2Body_IsContactRecyclingEnabled(bodyA), Is.False);

        b2Body_EnableContactRecycling(bodyA, true);
        Assert.That(b2Body_IsContactRecyclingEnabled(bodyA), Is.True);

        // Per-def opt-out at creation
        bodyDef.enableContactRecycling = false;
        B2BodyId bodyB = b2CreateBody(worldId, bodyDef);
        Assert.That(b2Body_IsContactRecyclingEnabled(bodyB), Is.False);

        // Stepping after toggling must not trip the flag-sync validator
        b2World_Step(worldId, 1.0f / 60.0f, 4);

        b2DestroyWorld(worldId);
    }

    [Test]
    public void SetBulletDriftTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.isBullet = false;
            B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

            Assert.That(b2Body_IsBullet(bodyId), Is.False);

            b2Body_SetBullet(bodyId, true);
            Assert.That(b2Body_IsBullet(bodyId), Is.True);

            B2MotionLocks locks = new B2MotionLocks();
            locks.linearX = true;
            b2Body_SetMotionLocks(bodyId, locks);

            Assert.That(b2Body_IsBullet(bodyId), Is.True);
        }

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.isBullet = true;
            B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

            Assert.That(b2Body_IsBullet(bodyId), Is.True);

            b2Body_SetBullet(bodyId, false);
            Assert.That(b2Body_IsBullet(bodyId), Is.False);

            B2MotionLocks locks = new B2MotionLocks();
            locks.linearX = true;
            b2Body_SetMotionLocks(bodyId, locks);

            Assert.That(b2Body_IsBullet(bodyId), Is.False);
        }

        b2DestroyWorld(worldId);
    }
    [Test]
    public void WorldBounds()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        // An empty world has no bounds
        B2AABB empty = b2World_GetBounds(worldId);
        Assert.That(empty.lowerBound.X, Is.EqualTo(0.0f));
        Assert.That(empty.upperBound.X, Is.EqualTo(0.0f));

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = b2ToPos(new B2Vec2(-10.0f, 0.0f));
        B2BodyId staticId = b2CreateBody(worldId, bodyDef);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeBox(1.0f, 1.0f);
        b2CreatePolygonShape(staticId, shapeDef, box);

        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = b2ToPos(new B2Vec2(10.0f, 5.0f));
        B2BodyId dynamicId = b2CreateBody(worldId, bodyDef);
        b2CreatePolygonShape(dynamicId, shapeDef, box);

        B2AABB bounds = b2World_GetBounds(worldId);
        Assert.That(bounds.lowerBound.X, Is.LessThanOrEqualTo(-11.0f));
        Assert.That(bounds.lowerBound.Y, Is.LessThanOrEqualTo(-1.0f));
        Assert.That(bounds.upperBound.X, Is.GreaterThanOrEqualTo(11.0f));
        Assert.That(bounds.upperBound.Y, Is.GreaterThanOrEqualTo(6.0f));

        b2DestroyWorld(worldId);
    }

    // Counts marked nodes, the root included. Free nodes are never marked.
    private static int CountEnlargedNodes(B2DynamicTree tree)
    {
        int count = 0;
        for (int i = 0; i < tree.nodeEnd; ++i)
        {
            count += b2IsNodeMoved(tree.nodes[i]) ? 1 : 0;
        }

        return count;
    }

    // A proxy can be enlarged in one step and destroyed before the next. The remove recomputes the
    // ancestor marks from the surviving children, so the mover's marks go with it and the next step
    // starts clean.
    private static void EnlargedProxyDestroyed(int workerCount)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = b2Vec2_zero;
        worldDef.workerCount = workerCount;
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2World world = b2GetWorldFromId(worldId);
        B2DynamicTree tree = world.broadPhase.trees[(int)B2BodyType.b2_dynamicBody];

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Circle circle = new B2Circle(b2Vec2_zero, 0.25f);

        // Resting bodies keep the tree deep enough that the mover has an ancestor that outlives it
        for (int i = 0; i < 8; ++i)
        {
            bodyDef.position = new B2Pos(2.0f * i, 0.0f);
            B2BodyId restingId = b2CreateBody(worldId, bodyDef);
            b2CreateCircleShape(restingId, shapeDef, circle);
        }

        // This body outruns its AABB margin every step
        bodyDef.position = new B2Pos(7.0f, 1.0f);
        bodyDef.linearVelocity = new B2Vec2(6.0f, 0.0f);
        B2BodyId moverId = b2CreateBody(worldId, bodyDef);
        b2CreateCircleShape(moverId, shapeDef, circle);

        float timeStep = 1.0f / 60.0f;
        b2World_Step(worldId, timeStep, 4);

        Assert.That(CountEnlargedNodes(tree), Is.GreaterThan(0));

        b2DestroyBody(moverId);

        Assert.That(CountEnlargedNodes(tree), Is.EqualTo(0));

        b2World_Step(worldId, timeStep, 4);

        Assert.That(CountEnlargedNodes(tree), Is.EqualTo(0));

        b2DestroyWorld(worldId);
    }

    [Test]
    public void EnlargedProxyDestroyedTest()
    {
        EnlargedProxyDestroyed(1);
        EnlargedProxyDestroyed(4);
    }

    // Broad phase pair creation through every route that marks a proxy: creation, teleport, a static
    // shape that asks for contacts, refiltering, collide connected, enable, a type change, a static
    // rebuild that must keep its pending marks, and the filters that reject a pair.
    // Bodies never move on their own here, gravity is off and sleep is disabled, so a contact can only
    // come from the route under test. The count is every live contact, touching or not, which is what
    // the broad phase decides.
    private struct PairBox
    {
        public B2BodyId bodyId;
        public B2ShapeId shapeId;
    }

    private static PairBox CreatePairBox(B2WorldId worldId, B2BodyType type, float x, float y, in B2ShapeDef shapeDef)
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = type;
        bodyDef.position = new B2Pos(x, y);

        PairBox box;
        box.bodyId = b2CreateBody(worldId, bodyDef);

        B2Polygon square = b2MakeSquare(0.5f);
        box.shapeId = b2CreatePolygonShape(box.bodyId, shapeDef, square);
        return box;
    }

    private static B2WorldId CreatePairWorld(int workerCount)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = b2Vec2_zero;
        worldDef.enableSleep = false;
        worldDef.workerCount = workerCount;
        return b2CreateWorld(worldDef);
    }

    private static int PairContactCount(B2WorldId worldId)
    {
        return b2World_GetCounters(worldId).contactCount;
    }

    private static void StepPairWorld(B2WorldId worldId)
    {
        b2World_Step(worldId, 1.0f / 60.0f, 4);
    }

    // Every kind of proxy created over a resting body
    private static void PairCreation(int workerCount)
    {
        B2WorldId worldId = CreatePairWorld(workerCount);
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        // Dynamic created on static
        CreatePairBox(worldId, B2BodyType.b2_staticBody, 0.0f, 0.0f, shapeDef);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 0.5f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        // Dynamic created on a resting dynamic
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 10.0f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 10.5f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(2));

        // Kinematic created on a resting dynamic, the mark is on the kinematic side
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 20.0f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        CreatePairBox(worldId, B2BodyType.b2_kinematicBody, 20.5f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(3));

        // A static shape created on a resting dynamic pairs by default and not when told to stay quiet
        B2ShapeDef quietDef = b2DefaultShapeDef();
        quietDef.invokeContactCreation = false;
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 30.0f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        CreatePairBox(worldId, B2BodyType.b2_staticBody, 30.5f, 0.0f, quietDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(3));

        CreatePairBox(worldId, B2BodyType.b2_staticBody, 29.5f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(4));

        b2DestroyWorld(worldId);
    }

    // A teleport marks the moved proxy, kinematic and static ones included
    private static void PairTeleport(int workerCount)
    {
        B2WorldId worldId = CreatePairWorld(workerCount);
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        CreatePairBox(worldId, B2BodyType.b2_staticBody, 0.0f, 0.0f, shapeDef);
        PairBox mover = CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 10.0f, 0.0f, shapeDef);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 20.0f, 0.0f, shapeDef);
        PairBox wall = CreatePairBox(worldId, B2BodyType.b2_staticBody, 30.0f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        b2Body_SetTransform(mover.bodyId, new B2Pos(0.5f, 0.0f), b2Rot_identity);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2Body_SetTransform(wall.bodyId, new B2Pos(20.5f, 0.0f), b2Rot_identity);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(2));

        // Kinematic onto a resting dynamic, the mark is on the kinematic side
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 40.0f, 0.0f, shapeDef);
        PairBox pusher = CreatePairBox(worldId, B2BodyType.b2_kinematicBody, 50.0f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(2));

        b2Body_SetTransform(pusher.bodyId, new B2Pos(40.5f, 0.0f), b2Rot_identity);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(3));

        b2DestroyWorld(worldId);
    }

    // A filter change must find the pairs the old filter rejected and drop the ones it now rejects
    private static void PairRefilter(int workerCount)
    {
        B2WorldId worldId = CreatePairWorld(workerCount);

        B2ShapeDef pickyDef = b2DefaultShapeDef();
        pickyDef.filter.categoryBits = 2;
        pickyDef.filter.maskBits = 1;

        B2ShapeDef closedDef = b2DefaultShapeDef();
        closedDef.filter.categoryBits = 4;

        B2Filter openFilter = b2DefaultFilter();
        openFilter.categoryBits = 1;

        PairBox ground = CreatePairBox(worldId, B2BodyType.b2_staticBody, 0.0f, 0.0f, closedDef);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 0.5f, 0.0f, pickyDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        b2Shape_SetFilter(ground.shapeId, openFilter);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2Shape_SetFilter(ground.shapeId, closedDef.filter);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        // Same on a dynamic shape
        PairBox other = CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 10.0f, 0.0f, closedDef);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 10.5f, 0.0f, pickyDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        b2Shape_SetFilter(other.shapeId, openFilter);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2DestroyWorld(worldId);
    }

    // Collide connected is off by default and turning it on must find the pair, static partner too.
    // The weld frames coincide so the joint does not move anything.
    private static void PairCollideConnected(int workerCount)
    {
        B2WorldId worldId = CreatePairWorld(workerCount);
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        PairBox boxA = CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 0.0f, 0.0f, shapeDef);
        PairBox boxB = CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 0.5f, 0.0f, shapeDef);

        B2WeldJointDef jointDef = b2DefaultWeldJointDef();
        jointDef.@base.bodyIdA = boxA.bodyId;
        jointDef.@base.bodyIdB = boxB.bodyId;
        jointDef.@base.localFrameB.p = new B2Vec2(-0.5f, 0.0f);
        B2JointId jointId = b2CreateWeldJoint(worldId, jointDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        b2Joint_SetCollideConnected(jointId, true);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2Joint_SetCollideConnected(jointId, false);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        // Static partner, the mark goes on the dynamic body
        PairBox wall = CreatePairBox(worldId, B2BodyType.b2_staticBody, 10.0f, 0.0f, shapeDef);
        PairBox boxC = CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 10.5f, 0.0f, shapeDef);
        jointDef.@base.bodyIdA = wall.bodyId;
        jointDef.@base.bodyIdB = boxC.bodyId;
        B2JointId wallJointId = b2CreateWeldJoint(worldId, jointDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        b2Joint_SetCollideConnected(wallJointId, true);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2DestroyWorld(worldId);
    }

    private static bool RejectTaggedShape(B2ShapeId shapeIdA, B2ShapeId shapeIdB, object context)
    {
        return b2Shape_GetUserData(shapeIdA).type == B2UserDataType.None && b2Shape_GetUserData(shapeIdB).type == B2UserDataType.None;
    }

    // The filters that reject a pair. The benchmarks never exercise these, so this is the only guard.
    private static void PairFilters(int workerCount)
    {
        B2WorldId worldId = CreatePairWorld(workerCount);
        b2World_SetCustomFilterCallback(worldId, RejectTaggedShape, null);

        // Category and mask must pass in both directions
        B2ShapeDef defA = b2DefaultShapeDef();
        defA.filter.categoryBits = 1;
        defA.filter.maskBits = 2;
        B2ShapeDef defB = b2DefaultShapeDef();
        defB.filter.categoryBits = 2;
        defB.filter.maskBits = 1;
        B2ShapeDef defC = b2DefaultShapeDef();
        defC.filter.categoryBits = 2;
        defC.filter.maskBits = 4;
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 0.0f, 0.0f, defA);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 0.5f, 0.0f, defB);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, -0.5f, 0.0f, defC);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        // Group index overrides the bits, negative rejects and positive accepts
        B2ShapeDef negativeDef = b2DefaultShapeDef();
        negativeDef.filter.groupIndex = -1;
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 10.0f, 0.0f, negativeDef);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 10.5f, 0.0f, negativeDef);
        B2ShapeDef positiveDef = defC;
        positiveDef.filter.groupIndex = 1;
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 20.0f, 0.0f, positiveDef);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 20.5f, 0.0f, positiveDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(2));

        // Custom filter rejects a tagged shape
        B2ShapeDef customDef = b2DefaultShapeDef();
        customDef.enableCustomFiltering = true;
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 30.0f, 0.0f, customDef);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 30.5f, 0.0f, customDef);
        customDef.userData = new B2UserData(1L);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 40.0f, 0.0f, customDef);
        customDef.userData = B2UserData.Empty;
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 40.5f, 0.0f, customDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(3));

        // Sensors never make contacts
        B2ShapeDef sensorDef = b2DefaultShapeDef();
        sensorDef.isSensor = true;
        B2ShapeDef plainDef = b2DefaultShapeDef();
        CreatePairBox(worldId, B2BodyType.b2_staticBody, 50.0f, 0.0f, sensorDef);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 50.5f, 0.0f, plainDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(3));

        b2DestroyWorld(worldId);
    }

    // A body that outran its margin on the step it went to sleep still gets its contact.
    // The marks live in the tree and do not care which set the body is in.
    private static void PairSleepEscape(int workerCount, bool sleepBeforePairs)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = b2Vec2_zero;
        worldDef.workerCount = workerCount;
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        CreatePairBox(worldId, B2BodyType.b2_staticBody, 0.0f, 0.0f, shapeDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Pos(-2.0f, 0.0f);
        bodyDef.linearVelocity = new B2Vec2(58.0f, 0.0f);
        B2BodyId moverId = b2CreateBody(worldId, bodyDef);
        B2Polygon square = b2MakeSquare(0.5f);
        b2CreatePolygonShape(moverId, shapeDef, square);

        // One step carries the mover 0.97, far past its margin and into the fat box of the wall with a
        // few centimeters to spare, but not into the wall itself
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        if (sleepBeforePairs)
        {
            b2Body_SetAwake(moverId, false);
            Assert.That(b2Body_IsAwake(moverId), Is.False);
        }

        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2DestroyWorld(worldId);
    }

    // Enable recreates the proxies with marks, on either side of a resting pair. The fat boxes
    // overlap across the gap while the shapes do not, so nothing pushes and nothing moves.
    private static void PairEnable(int workerCount)
    {
        B2WorldId worldId = CreatePairWorld(workerCount);
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        PairBox ground = CreatePairBox(worldId, B2BodyType.b2_staticBody, 0.0f, 0.0f, shapeDef);
        PairBox box = CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 1.1f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2Body_Disable(box.bodyId);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        b2Body_Enable(box.bodyId);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        // The static side, the resting box has no mark of its own
        b2Body_Disable(ground.bodyId);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        b2Body_Enable(ground.bodyId);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2DestroyWorld(worldId);
    }

    // A type change recreates the proxies with marks and must find the resting neighbors again
    private static void PairSetType(int workerCount)
    {
        B2WorldId worldId = CreatePairWorld(workerCount);
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        CreatePairBox(worldId, B2BodyType.b2_staticBody, 0.0f, 0.0f, shapeDef);
        PairBox middle = CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 1.1f, 0.0f, shapeDef);
        CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 2.2f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(2));

        // Kinematic drops the static pair and keeps the dynamic one
        b2Body_SetType(middle.bodyId, B2BodyType.b2_kinematicBody);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2Body_SetType(middle.bodyId, B2BodyType.b2_dynamicBody);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(2));

        // Static drops the static pair too and the dynamic pair becomes static versus dynamic
        b2Body_SetType(middle.bodyId, B2BodyType.b2_staticBody);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2Body_SetType(middle.bodyId, B2BodyType.b2_dynamicBody);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(2));

        b2DestroyWorld(worldId);
    }

    // Bullet is a flag only, the pair must be untouched
    private static void PairSetBullet(int workerCount)
    {
        B2WorldId worldId = CreatePairWorld(workerCount);
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        CreatePairBox(worldId, B2BodyType.b2_staticBody, 0.0f, 0.0f, shapeDef);
        PairBox box = CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 1.1f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2Body_SetBullet(box.bodyId, true);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2Body_SetBullet(box.bodyId, false);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        b2DestroyWorld(worldId);
    }

    // A static rebuild must keep the pending marks. The resting box has none of its own, so a lost
    // mark means the pair is never found.
    private static void PairRebuildStatic(int workerCount)
    {
        B2WorldId worldId = CreatePairWorld(workerCount);
        B2ShapeDef shapeDef = b2DefaultShapeDef();

        PairBox box = CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 0.0f, 0.0f, shapeDef);
        CreatePairBox(worldId, B2BodyType.b2_staticBody, 10.0f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        // No marks to keep
        b2World_RebuildStaticTree(worldId);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(0));

        CreatePairBox(worldId, B2BodyType.b2_staticBody, 1.1f, 0.0f, shapeDef);
        b2World_RebuildStaticTree(worldId);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        // A quiet static shape has no mark to keep
        B2ShapeDef quietDef = b2DefaultShapeDef();
        quietDef.invokeContactCreation = false;
        CreatePairBox(worldId, B2BodyType.b2_staticBody, -1.1f, 0.0f, quietDef);
        b2World_RebuildStaticTree(worldId);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        // Overlapping the resting box, the contact must touch on the first step after the rebuild
        CreatePairBox(worldId, B2BodyType.b2_staticBody, 0.0f, 0.5f, shapeDef);
        b2World_RebuildStaticTree(worldId);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(2));

        B2ContactData[] contactData = new B2ContactData[4];
        int touchingCount = b2Body_GetContactData(box.bodyId, contactData, 4);
        Assert.That(touchingCount, Is.EqualTo(1));
        Assert.That(contactData[0].manifold.pointCount, Is.GreaterThan(0));

        b2DestroyWorld(worldId);
    }

    // A static shape created over a sleeping body still gets its contact, parked with the sleeper
    // until it wakes. The world counter only sees awake contacts, so this counts on the body.
    private static void PairSleepingStatic(int workerCount)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = b2Vec2_zero;
        worldDef.workerCount = workerCount;
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        PairBox sleeper = CreatePairBox(worldId, B2BodyType.b2_dynamicBody, 0.0f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        b2Body_SetAwake(sleeper.bodyId, false);
        Assert.That(b2Body_IsAwake(sleeper.bodyId), Is.False);

        CreatePairBox(worldId, B2BodyType.b2_staticBody, 0.5f, 0.0f, shapeDef);
        StepPairWorld(worldId);
        Assert.That(b2Body_GetContactCapacity(sleeper.bodyId), Is.EqualTo(1));
        Assert.That(b2Body_IsAwake(sleeper.bodyId), Is.False);

        // The quiet kind never pairs with a sleeper
        B2ShapeDef quietDef = b2DefaultShapeDef();
        quietDef.invokeContactCreation = false;
        CreatePairBox(worldId, B2BodyType.b2_staticBody, -0.5f, 0.0f, quietDef);
        StepPairWorld(worldId);
        Assert.That(b2Body_GetContactCapacity(sleeper.bodyId), Is.EqualTo(1));

        // Waking brings the parked contact into the step and it touches at once
        b2Body_SetAwake(sleeper.bodyId, true);
        StepPairWorld(worldId);
        Assert.That(PairContactCount(worldId), Is.EqualTo(1));

        B2ContactData[] contactData = new B2ContactData[4];
        int touchingCount = b2Body_GetContactData(sleeper.bodyId, contactData, 4);
        Assert.That(touchingCount, Is.EqualTo(1));
        Assert.That(contactData[0].manifold.pointCount, Is.GreaterThan(0));

        b2DestroyWorld(worldId);
    }

    [TestCase(1)]
    [TestCase(4)]
    public void BroadPhasePairsTest(int workerCount)
    {
        PairCreation(workerCount);
        PairTeleport(workerCount);
        PairRefilter(workerCount);
        PairCollideConnected(workerCount);
        PairFilters(workerCount);
        PairEnable(workerCount);
        PairSetType(workerCount);
        PairSetBullet(workerCount);
        PairRebuildStatic(workerCount);
        PairSleepEscape(workerCount, false);
        PairSleepEscape(workerCount, true);
        PairSleepingStatic(workerCount);
    }

    // The contact sim carries a persistent locator for each body sim instead of a copy written by the
    // collide task every step. These scenarios drive every site that changes a body's set or local index
    // while its contacts are live, then step under validation so b2ValidateSolverSets and the prepare
    // asserts are the real check. Worker count parity on the state hash guards the multithreaded paths.

    private static void StepLocatorWorld(B2WorldId worldId, int stepCount)
    {
        for (int i = 0; i < stepCount; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
        }
    }

    private static bool StepUntilAsleep(B2WorldId worldId, B2BodyId bodyId, int maxSteps)
    {
        for (int i = 0; i < maxSteps; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
            if (b2Body_IsAwake(bodyId) == false)
            {
                return true;
            }
        }

        return false;
    }

    private static B2WorldId CreateLocatorWorld(int workerCount, bool enableSleep)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = new B2Vec2(0.0f, -10.0f);
        worldDef.enableSleep = enableSleep;
        worldDef.workerCount = workerCount;
        return b2CreateWorld(worldDef);
    }

    private static B2BodyId CreateLocatorGround(B2WorldId worldId, float x, float halfWidth)
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = new B2Pos(x, -0.5f);
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeBox(halfWidth, 0.5f);
        b2CreatePolygonShape(bodyId, shapeDef, box);
        return bodyId;
    }

    private static B2BodyId CreateLocatorBox(B2WorldId worldId, B2BodyType type, float x, float y)
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = type;
        bodyDef.position = new B2Pos(x, y);
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon square = b2MakeSquare(0.5f);
        b2CreatePolygonShape(bodyId, shapeDef, square);
        return bodyId;
    }

    // A pile sleeps on static ground and a dropped body wakes it. Covers first touch, the graph add and
    // the wake of a whole sleeping set.
    private static ulong LocatorSleepWake(int workerCount)
    {
        B2WorldId worldId = CreateLocatorWorld(workerCount, true);
        CreateLocatorGround(worldId, 0.0f, 10.0f);

        B2BodyId[] pile = new B2BodyId[3];
        for (int i = 0; i < 3; ++i)
        {
            pile[i] = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, 0.0f, 0.5f + i);
        }

        Assert.That(StepUntilAsleep(worldId, pile[0], 600), Is.True);
        Assert.That(b2Body_IsAwake(pile[2]), Is.False);

        B2BodyId dropper = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, 0.0f, 6.0f);
        Assert.That(b2Body_IsAwake(pile[0]), Is.False);

        StepLocatorWorld(worldId, 60);
        Assert.That(b2Body_IsAwake(pile[0]), Is.True);
        Assert.That(b2Body_IsAwake(dropper), Is.True);

        Assert.That(StepUntilAsleep(worldId, dropper, 900), Is.True);
        Assert.That(b2Body_IsAwake(pile[0]), Is.False);

        ulong hash = b2World_GetStateHash(worldId);
        b2DestroyWorld(worldId);
        return hash;
    }

    // Destroying a body in the middle of the awake set swaps the last awake sim into the hole while that
    // body's contacts are live.
    private static ulong LocatorDestroyAwakeBody(int workerCount)
    {
        B2WorldId worldId = CreateLocatorWorld(workerCount, false);
        CreateLocatorGround(worldId, 0.0f, 20.0f);

        B2BodyId[] stack = new B2BodyId[4];
        for (int i = 0; i < 4; ++i)
        {
            stack[i] = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, 0.0f, 0.5f + i);
        }

        // A second stack far away owns the tail of the awake set
        B2BodyId[] neighbors = new B2BodyId[3];
        for (int i = 0; i < 3; ++i)
        {
            neighbors[i] = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, 10.0f, 0.5f + i);
        }

        StepLocatorWorld(worldId, 120);
        float restY = b2Body_GetPosition(neighbors[2]).Y;

        b2DestroyBody(stack[0]);
        StepLocatorWorld(worldId, 30);

        Assert.That(b2AbsFloat(b2Body_GetPosition(neighbors[2]).Y - restY), Is.LessThanOrEqualTo(0.01f));

        ulong hash = b2World_GetStateHash(worldId);
        b2DestroyWorld(worldId);
        return hash;
    }

    // Destroying a static body swaps the last static sim into the hole. The moved body still has awake
    // contacts, so its side of them has to be re-encoded or the collide task reads past the static set.
    private static ulong LocatorDestroyStaticBody(int workerCount)
    {
        B2WorldId worldId = CreateLocatorWorld(workerCount, false);

        B2BodyId[] platforms = new B2BodyId[3];
        B2BodyId[] boxes = new B2BodyId[3];
        for (int i = 0; i < 3; ++i)
        {
            float x = 10.0f * i;
            platforms[i] = CreateLocatorGround(worldId, x, 2.0f);
            boxes[i] = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, x, 0.5f);
        }

        StepLocatorWorld(worldId, 60);
        float restY = b2Body_GetPosition(boxes[2]).Y;

        b2DestroyBody(platforms[0]);
        StepLocatorWorld(worldId, 40);

        Assert.That(b2AbsFloat(b2Body_GetPosition(boxes[2]).Y - restY), Is.LessThanOrEqualTo(0.01f));
        Assert.That(b2Body_GetPosition(boxes[0]).Y, Is.LessThan(-1.0f));

        ulong hash = b2World_GetStateHash(worldId);
        b2DestroyWorld(worldId);
        return hash;
    }

    // A kinematic that never sleeps keeps a non touching contact to a body that falls asleep. That
    // contact stays in the awake set with its sleeping side on the cold path, then the kinematic closes
    // the gap and wakes it.
    private static ulong LocatorSleepingNeighbor(int workerCount)
    {
        B2WorldId worldId = CreateLocatorWorld(workerCount, true);
        CreateLocatorGround(worldId, 0.0f, 10.0f);

        B2BodyId sleeper = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, 0.0f, 0.5f);

        // The fat boxes overlap across a tenth of a meter, well past the speculative distance
        B2BodyId mover = CreateLocatorBox(worldId, B2BodyType.b2_kinematicBody, 1.1f, 0.5f);
        b2Body_EnableSleeping(mover, false);

        Assert.That(StepUntilAsleep(worldId, sleeper, 600), Is.True);
        Assert.That(b2Body_IsAwake(mover), Is.True);

        B2ContactData[] contactData = new B2ContactData[4];
        Assert.That(b2Body_GetContactCapacity(mover), Is.EqualTo(1));
        Assert.That(b2Body_GetContactData(mover, contactData, 4), Is.EqualTo(0));

        StepLocatorWorld(worldId, 30);
        Assert.That(b2Body_IsAwake(sleeper), Is.False);

        b2Body_SetLinearVelocity(mover, new B2Vec2(-2.0f, 0.0f));
        StepLocatorWorld(worldId, 10);
        Assert.That(b2Body_IsAwake(sleeper), Is.True);

        ulong hash = b2World_GetStateHash(worldId);
        b2DestroyWorld(worldId);
        return hash;
    }

    // Type change, disable and enable all route through the body transfer, which pulls a body sim out of
    // the middle of a set while other bodies hold contacts.
    private static ulong LocatorTypeChange(int workerCount)
    {
        B2WorldId worldId = CreateLocatorWorld(workerCount, false);
        CreateLocatorGround(worldId, 0.0f, 20.0f);

        B2BodyId subject = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, 0.0f, 0.5f);
        CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, 0.0f, 1.5f);

        B2BodyId[] neighbors = new B2BodyId[3];
        for (int i = 0; i < 3; ++i)
        {
            neighbors[i] = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, 10.0f, 0.5f + i);
        }

        StepLocatorWorld(worldId, 120);
        float restY = b2Body_GetPosition(neighbors[2]).Y;

        b2Body_SetType(subject, B2BodyType.b2_staticBody);
        StepLocatorWorld(worldId, 30);
        Assert.That(b2Body_GetType(subject), Is.EqualTo(B2BodyType.b2_staticBody));

        b2Body_SetType(subject, B2BodyType.b2_dynamicBody);
        StepLocatorWorld(worldId, 30);
        Assert.That(b2Body_GetType(subject), Is.EqualTo(B2BodyType.b2_dynamicBody));

        b2Body_Disable(subject);
        StepLocatorWorld(worldId, 10);
        Assert.That(b2Body_IsEnabled(subject), Is.False);

        b2Body_Enable(subject);
        StepLocatorWorld(worldId, 30);
        Assert.That(b2Body_IsEnabled(subject), Is.True);

        Assert.That(b2AbsFloat(b2Body_GetPosition(neighbors[2]).Y - restY), Is.LessThanOrEqualTo(0.01f));

        ulong hash = b2World_GetStateHash(worldId);
        b2DestroyWorld(worldId);
        return hash;
    }

    // A joint between two sleeping bodies merges their sets instead of waking them, while an awake
    // kinematic holds a non touching contact to one of them across the merge.
    private static ulong LocatorSetMerge(int workerCount)
    {
        B2WorldId worldId = CreateLocatorWorld(workerCount, true);
        CreateLocatorGround(worldId, 0.0f, 20.0f);

        B2BodyId left = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, -5.0f, 0.5f);
        B2BodyId right = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, 5.0f, 0.5f);

        B2BodyId watcher = CreateLocatorBox(worldId, B2BodyType.b2_kinematicBody, -3.9f, 0.5f);
        b2Body_EnableSleeping(watcher, false);

        Assert.That(StepUntilAsleep(worldId, left, 600), Is.True);
        Assert.That(b2Body_IsAwake(right), Is.False);
        Assert.That(b2Body_IsAwake(watcher), Is.True);
        Assert.That(b2Body_GetContactCapacity(watcher), Is.EqualTo(1));

        B2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
        jointDef.@base.bodyIdA = left;
        jointDef.@base.bodyIdB = right;
        jointDef.length = 10.0f;
        B2JointId jointId = b2CreateDistanceJoint(worldId, jointDef);

        Assert.That(b2Body_IsAwake(left), Is.False);
        Assert.That(b2Body_IsAwake(right), Is.False);

        StepLocatorWorld(worldId, 30);
        Assert.That(b2Body_IsAwake(left), Is.False);

        b2Body_SetAwake(left, true);
        StepLocatorWorld(worldId, 10);
        Assert.That(b2Body_IsAwake(right), Is.True);

        b2DestroyJoint(jointId);
        StepLocatorWorld(worldId, 10);

        ulong hash = b2World_GetStateHash(worldId);
        b2DestroyWorld(worldId);
        return hash;
    }

    // A body moving far enough in one step carries the fast flag, which the collide task now reads off
    // the sim. A public setter mid flight makes sure a flag sync does not drop it.
    private static ulong LocatorFastBody(int workerCount)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = b2Vec2_zero;
        worldDef.workerCount = workerCount;
        B2WorldId worldId = b2CreateWorld(worldDef);

        CreateLocatorBox(worldId, B2BodyType.b2_staticBody, 10.0f, 0.0f);
        B2BodyId bullet = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, 0.0f, 0.0f);
        b2Body_SetLinearVelocity(bullet, new B2Vec2(40.0f, 0.0f));

        // A step carries it two thirds of a meter, well past half its minimum extent
        StepLocatorWorld(worldId, 4);
        Assert.That(b2Body_GetPosition(bullet).X, Is.GreaterThan(2.0f));

        b2Body_EnableSleeping(bullet, false);
        StepLocatorWorld(worldId, 4);

        b2Body_EnableSleeping(bullet, true);
        StepLocatorWorld(worldId, 30);

        // It stopped at the wall instead of tunneling through
        Assert.That(b2Body_GetPosition(bullet).X, Is.LessThan(9.1f));
        Assert.That(b2Body_GetPosition(bullet).X, Is.GreaterThan(8.5f));

        ulong hash = b2World_GetStateHash(worldId);
        b2DestroyWorld(worldId);
        return hash;
    }

    // A row of boxes at rest in zero gravity is unaffected by sleeping, so the run that sleeps and wakes
    // on impact has to land where the run that never sleeps lands. Positions rather than the state hash
    // because the hash folds in set and local index, which sleeping is entitled to change.
    private static void LocatorSleepParity(int workerCount)
    {
        float[,] finalX = new float[2, 3];

        for (int pass = 0; pass < 2; ++pass)
        {
            B2WorldDef worldDef = b2DefaultWorldDef();
            worldDef.gravity = b2Vec2_zero;
            worldDef.enableSleep = pass == 0;
            worldDef.workerCount = workerCount;
            B2WorldId worldId = b2CreateWorld(worldDef);

            B2BodyId[] boxes = new B2BodyId[3];
            for (int i = 0; i < 3; ++i)
            {
                boxes[i] = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, (float)i, 0.0f);
            }

            StepLocatorWorld(worldId, 60);
            Assert.That(b2Body_IsAwake(boxes[0]), Is.EqualTo(pass == 1));

            B2BodyId impactor = CreateLocatorBox(worldId, B2BodyType.b2_dynamicBody, -5.0f, 0.0f);
            b2Body_SetLinearVelocity(impactor, new B2Vec2(5.0f, 0.0f));

            StepLocatorWorld(worldId, 120);
            Assert.That(b2Body_IsAwake(boxes[2]), Is.True);

            for (int i = 0; i < 3; ++i)
            {
                finalX[pass, i] = b2Body_GetPosition(boxes[i]).X;
            }

            b2DestroyWorld(worldId);
        }

        for (int i = 0; i < 3; ++i)
        {
            Assert.That(b2AbsFloat(finalX[0, i] - finalX[1, i]), Is.LessThanOrEqualTo(0.01f));
        }
    }

    [Test]
    public void BodySimLocatorTest()
    {
        int[] workerCounts = { 1, 4 };
        ulong[] hashes = new ulong[2];

        for (int i = 0; i < 2; ++i)
        {
            hashes[i] = LocatorSleepWake(workerCounts[i]);
        }

        Assert.That(hashes[0], Is.EqualTo(hashes[1]));

        for (int i = 0; i < 2; ++i)
        {
            hashes[i] = LocatorDestroyAwakeBody(workerCounts[i]);
        }

        Assert.That(hashes[0], Is.EqualTo(hashes[1]));

        for (int i = 0; i < 2; ++i)
        {
            hashes[i] = LocatorDestroyStaticBody(workerCounts[i]);
        }

        Assert.That(hashes[0], Is.EqualTo(hashes[1]));

        for (int i = 0; i < 2; ++i)
        {
            hashes[i] = LocatorSleepingNeighbor(workerCounts[i]);
        }

        Assert.That(hashes[0], Is.EqualTo(hashes[1]));

        for (int i = 0; i < 2; ++i)
        {
            hashes[i] = LocatorTypeChange(workerCounts[i]);
        }

        Assert.That(hashes[0], Is.EqualTo(hashes[1]));

        for (int i = 0; i < 2; ++i)
        {
            hashes[i] = LocatorSetMerge(workerCounts[i]);
        }

        Assert.That(hashes[0], Is.EqualTo(hashes[1]));

        for (int i = 0; i < 2; ++i)
        {
            hashes[i] = LocatorFastBody(workerCounts[i]);
        }

        Assert.That(hashes[0], Is.EqualTo(hashes[1]));

        for (int i = 0; i < 2; ++i)
        {
            LocatorSleepParity(workerCounts[i]);
        }
    }

    // A joint with no dynamic body lives in the static set even when one body is an awake kinematic.
    // Disabling and enabling that body has to respect this, whether the joint was created that way or
    // got there through body type changes. Box3D issue #164.
    [Test]
    public void NonDynamicJointDisableTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = b2Vec2_zero;
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        B2BodyId bodyIdA = b2CreateBody(worldId, bodyDef);
        bodyDef.position = new B2Pos(2.0f, 0.0f);
        B2BodyId bodyIdB = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeBox(0.5f, 0.5f);
        b2CreatePolygonShape(bodyIdA, shapeDef, box);
        b2CreatePolygonShape(bodyIdB, shapeDef, box);

        B2WeldJointDef jointDef = b2DefaultWeldJointDef();
        jointDef.@base.bodyIdA = bodyIdA;
        jointDef.@base.bodyIdB = bodyIdB;
        B2JointId weldId = b2CreateWeldJoint(worldId, jointDef);

        b2Body_SetType(bodyIdA, B2BodyType.b2_staticBody);
        b2Body_SetType(bodyIdB, B2BodyType.b2_kinematicBody);
        b2World_Step(worldId, 1.0f / 60.0f, 4);

        b2Body_Disable(bodyIdB);
        b2World_Step(worldId, 1.0f / 60.0f, 4);
        b2Body_Enable(bodyIdB);
        b2World_Step(worldId, 1.0f / 60.0f, 4);
        Assert.That(b2Joint_IsValid(weldId), Is.True);

        B2World world = b2GetWorldFromId(worldId);
        Assert.That(b2GetJointFullId(world, weldId).setIndex, Is.EqualTo((int)B2SolverSetType.b2_staticSet));

        b2Body_SetType(bodyIdB, B2BodyType.b2_dynamicBody);
        b2World_Step(worldId, 1.0f / 60.0f, 4);
        Assert.That(b2GetJointFullId(world, weldId).setIndex, Is.EqualTo((int)B2SolverSetType.b2_awakeSet));

        // Created directly between a static and a kinematic body
        bodyDef.type = B2BodyType.b2_kinematicBody;
        bodyDef.position = new B2Pos(4.0f, 0.0f);
        B2BodyId bodyIdC = b2CreateBody(worldId, bodyDef);
        b2CreatePolygonShape(bodyIdC, shapeDef, box);

        B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
        revoluteDef.@base.bodyIdA = bodyIdA;
        revoluteDef.@base.bodyIdB = bodyIdC;
        B2JointId revoluteId = b2CreateRevoluteJoint(worldId, revoluteDef);

        b2Body_Disable(bodyIdC);
        b2Body_Enable(bodyIdC);
        b2World_Step(worldId, 1.0f / 60.0f, 4);
        Assert.That(b2GetJointFullId(world, revoluteId).setIndex, Is.EqualTo((int)B2SolverSetType.b2_staticSet));

        b2DestroyWorld(worldId);
    }

    private sealed class RejectFlag
    {
        public bool reject;
    }

    private static void PreSolveReject(B2ShapeId shapeIdA, B2ShapeId shapeIdB, ref B2Manifold manifold, object context)
    {
        B2_UNUSED(shapeIdA);
        B2_UNUSED(shapeIdB);
        RejectFlag flag = (RejectFlag)context;
        if (flag.reject)
        {
            manifold.pointCount = 0;
        }
    }

    // A rejected pre-solve empties the manifold while the shapes may already touch. The next step must
    // rebuild the contact and ask again instead of recycling the empty pair. Box3D discussion #175.
    [Test]
    public void PreSolveRejectRecycleTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.enableSleep = false;
        B2WorldId worldId = b2CreateWorld(worldDef);

        RejectFlag flag = new RejectFlag { reject = true };
        b2World_SetPreSolveCallback(worldId, PreSolveReject, null, flag);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = new B2Pos(0.0f, -0.5f);
        B2BodyId groundId = b2CreateBody(worldId, bodyDef);
        B2Polygon groundBox = b2MakeBox(10.0f, 0.5f);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(groundId, shapeDef, groundBox);

        float radius = 0.25f;
        bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Pos(0.0f, radius);
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        shapeDef.enablePreSolveEvents = true;
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), radius);
        b2CreateCircleShape(bodyId, shapeDef, circle);

        b2World_Step(worldId, 1.0f / 60.0f, 4);

        flag.reject = false;

        float minSeparation = float.MaxValue;
        for (int i = 0; i < 30; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);

            B2Pos p = b2Body_GetPosition(bodyId);
            minSeparation = b2MinFloat(minSeparation, (float)p.Y - radius);
        }

        b2DestroyWorld(worldId);

        Assert.That(minSeparation, Is.GreaterThan(-B2_LINEAR_SLOP));
    }

    private sealed class PreSolveCounter
    {
        public int count;
    }

    private static void CountingPreSolve(B2ShapeId shapeIdA, B2ShapeId shapeIdB, ref B2Manifold manifold, object context)
    {
        B2_UNUSED(shapeIdA);
        B2_UNUSED(shapeIdB);
        PreSolveCounter counter = (PreSolveCounter)context;
        counter.count += 1;
    }

    // Contacts latch the recycle and pre-solve flags at creation, and a recycled contact never reaches
    // pre-solve. Enabling pre-solve mid-simulation must refresh the contacts that already exist,
    // including a settled contact that recycles every step.
    [Test]
    public void PreSolveToggleTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.enableSleep = false;
        B2WorldId worldId = b2CreateWorld(worldDef);

        PreSolveCounter counter = new PreSolveCounter();
        b2World_SetPreSolveCallback(worldId, CountingPreSolve, null, counter);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.position = new B2Pos(0.0f, -0.5f);
        B2BodyId groundId = b2CreateBody(worldId, bodyDef);
        B2Polygon groundBox = b2MakeBox(10.0f, 0.5f);
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(groundId, shapeDef, groundBox);

        float radius = 0.25f;
        bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = new B2Pos(0.0f, radius + 0.2f);
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), radius);
        B2ShapeId ballId = b2CreateCircleShape(bodyId, shapeDef, circle);

        // Let the ball settle so the contact recycles
        for (int i = 0; i < 60; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
        }

        Assert.That(counter.count, Is.EqualTo(0));

        b2Shape_EnablePreSolveEvents(ballId, true);

        int before = counter.count;
        for (int i = 0; i < 10; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
        }

        // Pre-solve must fire every step, including on the recycled contact
        Assert.That(counter.count - before, Is.EqualTo(10));

        b2Shape_EnablePreSolveEvents(ballId, false);

        before = counter.count;
        for (int i = 0; i < 10; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
        }

        Assert.That(counter.count - before, Is.EqualTo(0));

        b2DestroyWorld(worldId);
    }
}
