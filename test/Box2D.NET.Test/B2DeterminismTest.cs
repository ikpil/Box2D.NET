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

namespace Box2D.NET.Test;

public class B2DeterminismTest
{
    private const int EXPECTED_SLEEP_STEP = 262;
    private const uint EXPECTED_HASH = 0x3841BB81;

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

    // Test cross-platform determinism.
    [Test]
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
}
