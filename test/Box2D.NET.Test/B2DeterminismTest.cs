// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using Box2D.NET.Shared;
using NUnit.Framework;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.Shared.Determinism;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;

namespace Box2D.NET.Test;

public class B2DeterminismTest
{
    private const int EXPECTED_SLEEP_STEP = 295;
    private const uint EXPECTED_HASH = 0x36564C94;

    // todo_erin move this to shared
    public static int SingleMultithreadingTest(int workerCount)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = workerCount;

        B2WorldId worldId = b2CreateWorld(worldDef);

        FallingHingeData data = CreateFallingHinges(worldId);

        float timeStep = 1.0f / 60.0f;
        int stepLimit = 500;
        for ( int i = 0; i < stepLimit; ++i )
        {
            int subStepCount = 4;
            b2World_Step(worldId, timeStep, subStepCount);
            TracyCFrameMark();

            bool done = UpdateFallingHinges(worldId, ref data);
            if (done)
            {
                break;
            }
        }

        b2DestroyWorld(worldId);

        Assert.That(data.sleepStep, Is.EqualTo(EXPECTED_SLEEP_STEP));
        Assert.That(data.hash, Is.EqualTo(EXPECTED_HASH));

        DestroyFallingHinges(ref data);

        return 0;
    }

    // Test multithreaded determinism.
    [Test]
    [Order(1)]
    public void MultithreadingTest()
    {
        for (int run = 0; run < 3; ++run)
        {
            for (int workerCount = 1; workerCount < 16; workerCount += 2)
            {
                int result = SingleMultithreadingTest(workerCount);
                Assert.That(result, Is.EqualTo(0));
            }

            for (int workerCount = 32; workerCount >= 0; workerCount -= 5)
            {
                int result = SingleMultithreadingTest(workerCount);
                Assert.That(result, Is.EqualTo(0));
            }
        }
    }

    // Test determinism using the built-in scheduler (no external task system).
    [Test]
    [Order(2)]
    public void BuiltInSchedulerTest()
    {
        for (int workerCount = 2; workerCount <= 8; workerCount += 2)
        {
            B2WorldDef worldDef = b2DefaultWorldDef();
            worldDef.workerCount = workerCount;

            B2WorldId worldId = b2CreateWorld(worldDef);

            FallingHingeData data = CreateFallingHinges(worldId);

            float timeStep = 1.0f / 60.0f;
            int stepLimit = 1000;
            for (int i = 0; i < stepLimit; ++i)
            {
                int subStepCount = 4;
                b2World_Step(worldId, timeStep, subStepCount);

                bool done = UpdateFallingHinges(worldId, ref data);
                if (done)
                {
                    break;
                }
            }

            b2DestroyWorld(worldId);

            string message = $"built-in scheduler workers={workerCount} sleepStep={data.sleepStep} hash=0x{data.hash:X8}";
            Assert.That(data.sleepStep, Is.EqualTo(EXPECTED_SLEEP_STEP), message);
            Assert.That(data.hash, Is.EqualTo(EXPECTED_HASH), message);

            DestroyFallingHinges(ref data);
        }
    }

    // Test cross-platform determinism.
    [Test]
    [Order(3)]
    public void CrossPlatformTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        FallingHingeData data = CreateFallingHinges(worldId);

        float timeStep = 1.0f / 60.0f;

        bool done = false;
        while (done == false)
        {
            int subStepCount = 4;
            b2World_Step(worldId, timeStep, subStepCount);
            TracyCFrameMark();

            done = UpdateFallingHinges(worldId, ref data);
        }

        Assert.That(data.sleepStep, Is.EqualTo(EXPECTED_SLEEP_STEP));
        Assert.That(data.hash, Is.EqualTo(EXPECTED_HASH));

        DestroyFallingHinges(ref data);

        b2DestroyWorld(worldId);
    }

    private static int SingleFallingHingeTest(int workerCount, bool sse2Fallback)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = workerCount;
        B2WorldId worldId = b2CreateWorld(worldDef);
        b2World_EnableSSE2Fallback(worldId, sse2Fallback);

        FallingHingeData data = CreateFallingHinges(worldId);

        float timeStep = 1.0f / 60.0f;
        bool done = false;
        while (done == false)
        {
            b2World_Step(worldId, timeStep, 4);
            done = UpdateFallingHinges(worldId, ref data);
        }

        b2DestroyWorld(worldId);

        if (data.sleepStep != EXPECTED_SLEEP_STEP || data.hash != EXPECTED_HASH)
        {
            Console.WriteLine($"  sse2Fallback={(sse2Fallback ? 1 : 0)} workers={workerCount} sleepStep={data.sleepStep} hash=0x{data.hash:X8}");
        }

        Assert.That(data.sleepStep, Is.EqualTo(EXPECTED_SLEEP_STEP));
        Assert.That(data.hash, Is.EqualTo(EXPECTED_HASH));

        DestroyFallingHinges(ref data);

        return 0;
    }

    private static B2WorldId CreateBouncePile(int workerCount, bool sse2Fallback)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = workerCount;
        B2WorldId worldId = b2CreateWorld(worldDef);
        b2World_EnableSSE2Fallback(worldId, sse2Fallback);

        B2BodyDef groundDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(worldId, groundDef);
        B2ShapeDef groundShapeDef = b2DefaultShapeDef();
        groundShapeDef.material.tangentSpeed = 0.5f;
        B2Segment segment = new B2Segment(new B2Vec2(-40.0f, 0.0f), new B2Vec2(40.0f, 0.0f));
        b2CreateSegmentShape(groundId, groundShapeDef, segment);

        B2Polygon box = b2MakeRoundedBox(0.4f, 0.3f, 0.05f);
        B2Circle circle = new B2Circle(new B2Vec2(0.0f, 0.0f), 0.35f);

        for (int i = 0; i < 300; ++i)
        {
            int column = i % 30;
            int row = i / 30;

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Pos(-15.0f + 1.0f * (float)column + 0.1f * (float)(row % 2), 1.0f + 0.9f * (float)row);
            bodyDef.rotation = b2MakeRot(0.3f * (float)i);
            B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.material.restitution = (i % 3) == 0 ? 0.0f : 0.25f * (float)(i % 5);
            shapeDef.material.friction = 0.2f + 0.1f * (float)(i % 7);
            shapeDef.material.rollingResistance = (i % 4) == 0 ? 0.1f : 0.0f;
            shapeDef.enableHitEvents = (i % 2) == 0;

            if (i % 3 == 1)
            {
                b2CreateCircleShape(bodyId, shapeDef, circle);
            }
            else
            {
                b2CreatePolygonShape(bodyId, shapeDef, box);
            }
        }

        return worldId;
    }

    // Width 4 and the native width must produce bitwise identical simulations.
    [Test]
    [Order(4)]
    public void SIMDWidthTest()
    {
        if (b2IsAVX2Available() == false)
        {
            Console.WriteLine("  subtest skipped: SIMDWidthTest, native SIMD width is 4");
            return;
        }

        for (int workerCount = 0; workerCount <= 4; workerCount += 4)
        {
            Assert.That(SingleFallingHingeTest(workerCount, true), Is.EqualTo(0));

            B2WorldId narrowId = CreateBouncePile(workerCount, true);
            B2WorldId wideId = CreateBouncePile(workerCount, false);

            float timeStep = 1.0f / 60.0f;
            for (int i = 0; i < 300; ++i)
            {
                b2World_Step(narrowId, timeStep, 4);
                b2World_Step(wideId, timeStep, 4);
                Assert.That(b2World_GetStateHash(narrowId), Is.EqualTo(b2World_GetStateHash(wideId)));
            }

            b2DestroyWorld(narrowId);
            b2DestroyWorld(wideId);
        }
    }
}
