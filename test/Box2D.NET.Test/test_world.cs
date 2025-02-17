// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT


using System.Diagnostics;
using NUnit.Framework;
using static Box2D.NET.world;
using static Box2D.NET.types;
using static Box2D.NET.body;
using static Box2D.NET.geometry;
using static Box2D.NET.shape;
using static Box2D.NET.math_function;
using static Box2D.NET.id;
using static Box2D.NET.constants;
using static Box2D.NET.joint;
using static Box2D.NET.core;


namespace Box2D.NET.Test;

public class test_world
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
        b2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = new b2Vec2(0.0f, -10.0f);

        b2WorldId worldId = b2CreateWorld(worldDef);
        Assert.That(b2World_IsValid(worldId));

        // Define the ground body.
        b2BodyDef groundBodyDef = b2DefaultBodyDef();
        groundBodyDef.position = new b2Vec2(0.0f, -10.0f);

        // Call the body factory which allocates memory for the ground body
        // from a pool and creates the ground box shape (also from a pool).
        // The body is also added to the world.
        b2BodyId groundId = b2CreateBody(worldId, groundBodyDef);
        Assert.That(b2Body_IsValid(groundId));

        // Define the ground box shape. The extents are the half-widths of the box.
        b2Polygon groundBox = b2MakeBox(50.0f, 10.0f);

        // Add the box shape to the ground body.
        b2ShapeDef groundShapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(groundId, groundShapeDef, groundBox);

        // Define the dynamic body. We set its position and call the body factory.
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.position = new b2Vec2(0.0f, 4.0f);

        b2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        // Define another box shape for our dynamic body.
        b2Polygon dynamicBox = b2MakeBox(1.0f, 1.0f);

        // Define the dynamic body shape
        b2ShapeDef shapeDef = b2DefaultShapeDef();

        // Set the box density to be non-zero, so it will be dynamic.
        shapeDef.density = 1.0f;

        // Override the default friction.
        shapeDef.friction = 0.3f;

        // Add the shape to the body.
        b2CreatePolygonShape(bodyId, shapeDef, dynamicBox);

        // Prepare for simulation. Typically we use a time step of 1/60 of a
        // second (60Hz) and 4 sub-steps. This provides a high quality simulation
        // in most game scenarios.
        float timeStep = 1.0f / 60.0f;
        int subStepCount = 4;

        b2Vec2 position = b2Body_GetPosition(bodyId);
        b2Rot rotation = b2Body_GetRotation(bodyId);

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

        Assert.That(b2AbsFloat(position.x), Is.LessThan(0.01f));
        Assert.That(b2AbsFloat(position.y - 1.00f), Is.LessThan(0.01f));
        Assert.That(b2AbsFloat(b2Rot_GetAngle(rotation)), Is.LessThan(0.01f));
    }

    [Test]
    public void EmptyWorld()
    {
        b2WorldDef worldDef = b2DefaultWorldDef();
        b2WorldId worldId = b2CreateWorld(worldDef);
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

    public const int BODY_COUNT = 10;

    [Test]
    public void DestroyAllBodiesWorld()
    {
        b2WorldDef worldDef = b2DefaultWorldDef();
        b2WorldId worldId = b2CreateWorld(worldDef);
        Assert.That(b2World_IsValid(worldId), Is.EqualTo(true));

        int count = 0;
        bool creating = true;

        b2BodyId[] bodyIds = new b2BodyId[BODY_COUNT];
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        b2Polygon square = b2MakeSquare(0.5f);

        for (int i = 0; i < 2 * BODY_COUNT + 10; ++i)
        {
            if (creating)
            {
                if (count < BODY_COUNT)
                {
                    bodyIds[count] = b2CreateBody(worldId, bodyDef);

                    b2ShapeDef shapeDef = b2DefaultShapeDef();
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

        b2Counters counters = b2World_GetCounters(worldId);
        Assert.That(counters.bodyCount, Is.EqualTo(0));

        b2DestroyWorld(worldId);

        Assert.That(b2World_IsValid(worldId), Is.EqualTo(false));
    }

    [Test]
    public void TestIsValid()
    {
        b2WorldDef worldDef = b2DefaultWorldDef();
        b2WorldId worldId = b2CreateWorld(worldDef);
        Assert.That(b2World_IsValid(worldId));

        b2BodyDef bodyDef = b2DefaultBodyDef();

        b2BodyId bodyId1 = b2CreateBody(worldId, bodyDef);
        Assert.That(b2Body_IsValid(bodyId1), Is.EqualTo(true));

        b2BodyId bodyId2 = b2CreateBody(worldId, bodyDef);
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
        Debug.Assert(WORLD_COUNT > 0, "world count");

        int count = 100;

        b2WorldId[] worldIds = new b2WorldId[WORLD_COUNT];

        for (int i = 0; i < count; ++i)
        {
            b2WorldDef worldDef = b2DefaultWorldDef();
            for (int j = 0; j < WORLD_COUNT; ++j)
            {
                worldIds[j] = b2CreateWorld(worldDef);
                Assert.That(b2World_IsValid(worldIds[j]), Is.EqualTo(true));

                b2BodyDef bodyDef = b2DefaultBodyDef();
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
                Assert.That(b2World_IsValid(worldIds[j]), Is.EqualTo(false));
                worldIds[j] = b2_nullWorldId;
            }
        }
    }

    public static bool CustomFilter(b2ShapeId shapeIdA, b2ShapeId shapeIdB, object context)
    {
        B2_UNUSED(shapeIdA);
        B2_UNUSED(shapeIdB);
        Assert.That(context, Is.EqualTo(null));
        return true;
    }

    public static bool PreSolveStatic(b2ShapeId shapeIdA, b2ShapeId shapeIdB, ref b2Manifold manifold, object context)
    {
        B2_UNUSED(shapeIdA);
        B2_UNUSED(shapeIdB);
        B2_UNUSED(manifold);
        Assert.That(context, Is.EqualTo(null));
        return false;
    }

    // This test is here to ensure all API functions link correctly.
    [Test]
    public void TestWorldCoverage()
    {
        b2WorldDef worldDef = b2DefaultWorldDef();

        b2WorldId worldId = b2CreateWorld(worldDef);
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
        b2World_SetPreSolveCallback(worldId, PreSolveStatic, null);

        b2Vec2 g = new b2Vec2(1.0f, 2.0f);
        b2World_SetGravity(worldId, g);
        b2Vec2 v = b2World_GetGravity(worldId);
        Assert.That(v.x, Is.EqualTo(g.x));
        Assert.That(v.y, Is.EqualTo(g.y));

        b2ExplosionDef explosionDef = b2DefaultExplosionDef();
        b2World_Explode(worldId, explosionDef);

        b2World_SetContactTuning(worldId, 10.0f, 2.0f, 4.0f);
        b2World_SetJointTuning(worldId, 10.0f, 2.0f);

        b2World_SetMaximumLinearSpeed(worldId, 10.0f);
        value = b2World_GetMaximumLinearSpeed(worldId);
        Assert.That(value, Is.EqualTo(10.0f));

        b2World_EnableWarmStarting(worldId, true);
        flag = b2World_IsWarmStartingEnabled(worldId);
        Assert.That(flag, Is.EqualTo(true));

        int count = b2World_GetAwakeBodyCount(worldId);
        Assert.That(count, Is.EqualTo(0));

        b2World_SetUserData(worldId, value);
        object userData = b2World_GetUserData(worldId);
        Assert.That((float)userData, Is.EqualTo(value));

        b2World_Step(worldId, 1.0f, 1);

        b2DestroyWorld(worldId);
    }

    [Test]
    public void TestSensor()
    {
        b2WorldDef worldDef = b2DefaultWorldDef();
        b2WorldId worldId = b2CreateWorld(worldDef);

        // Wall from x = 1 to x = 2
        b2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_staticBody;
        bodyDef.position.x = 1.5f;
        bodyDef.position.y = 11.0f;
        b2BodyId wallId = b2CreateBody(worldId, bodyDef);
        b2Polygon box = b2MakeBox(0.5f, 10.0f);
        b2ShapeDef shapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(wallId, shapeDef, box);

        // Bullet fired towards the wall
        bodyDef = b2DefaultBodyDef();
        bodyDef.type = b2BodyType.b2_dynamicBody;
        bodyDef.isBullet = true;
        bodyDef.gravityScale = 0.0f;
        bodyDef.position = new b2Vec2(7.39814f, 4.0f);
        bodyDef.linearVelocity = new b2Vec2(-20.0f, 0.0f);
        b2BodyId bulletId = b2CreateBody(worldId, bodyDef);
        shapeDef = b2DefaultShapeDef();
        shapeDef.isSensor = true;
        b2Circle circle = new b2Circle(new b2Vec2(0.0f, 0.0f), 0.1f);
        b2CreateCircleShape(bulletId, shapeDef, circle);

        int beginCount = 0;
        int endCount = 0;

        while (true)
        {
            float timeStep = 1.0f / 60.0f;
            int subStepCount = 4;
            b2World_Step(worldId, timeStep, subStepCount);

            b2Vec2 bulletPos = b2Body_GetPosition(bulletId);
            //Console.Write( "Bullet pos: %g %g\n", bulletPos.x, bulletPos.y );

            b2SensorEvents events = b2World_GetSensorEvents(worldId);

            if (events.beginCount > 0)
            {
                beginCount += 1;
            }

            if (events.endCount > 0)
            {
                endCount += 1;
            }

            if (bulletPos.x < -1.0f)
            {
                break;
            }
        }

        b2DestroyWorld(worldId);

        Assert.That(beginCount, Is.EqualTo(1));
        Assert.That(endCount, Is.EqualTo(1));
    }
}