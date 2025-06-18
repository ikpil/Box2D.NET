// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Box2D.NET.Shared;
using NUnit.Framework;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.Shared.Determinism;

namespace Box2D.NET.Test;

public class b2TaskTester : IDisposable
{
    private readonly int _workerCount;
    private SemaphoreSlim _semaphore;
    private int e_maxTasks;
    public int taskCount;
    private ConcurrentQueue<Task> _runningTasks;

    public b2TaskTester(int workerCount, int maxTasks)
    {
        _workerCount = workerCount;
        _semaphore = new SemaphoreSlim(workerCount);
        e_maxTasks = maxTasks;
        _runningTasks = new ConcurrentQueue<Task>();
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        _semaphore = null;

        B2_ASSERT(0 >= _runningTasks.Count);
    }

    private IEnumerable<int> Next(int itemCount, int minRange)
    {
        if (itemCount <= minRange)
        {
            yield return itemCount;
        }
        else
        {
            var workerCount = Math.Min(_workerCount, minRange);
            int quotient = itemCount / workerCount;
            int remainder = itemCount % workerCount;

            int distributeValue = remainder / quotient;
            int extraValueCount = remainder % quotient;

            int index = 0;
            for (int i = 0; i < workerCount; i++)
            {
                int count = quotient + distributeValue;
                if (i < extraValueCount)
                {
                    count = +1;
                }

                yield return count;
            }
        }
    }

    public object EnqueueTask(b2TaskCallback box2dTask, int itemCount, int minRange, object box2dContext, object userContext)
    {
        B2_UNUSED(userContext);

        if (taskCount < e_maxTasks)
        {
            uint loop = 0;
            int index = 0;
            int remain = itemCount;
            while (0 < remain)
            {
                var stepCount = Math.Min(remain, minRange);
                remain -= stepCount;

                uint workerIndex = (loop++) % (uint)_workerCount;
                var startIndex = index;
                var endIndex = startIndex + stepCount;

                index = endIndex;

                var running = Task.Run(async () =>
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        box2dTask.Invoke(startIndex, endIndex, workerIndex, box2dContext);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });

                _runningTasks.Enqueue(running);
            }
        }
        else
        {
            box2dTask(0, itemCount, 0, box2dContext);

            return null;
        }

        ++taskCount;
        return box2dTask;
    }

    public void FinishTask(object userTask, object userContext)
    {
        B2_UNUSED(userContext);

        // wait!
        while (_runningTasks.TryDequeue(out var task))
        {
            task.Wait();
        }
    }
}

public class B2DeterminismTest
{
    private const int EXPECTED_SLEEP_STEP = 342;
    private const uint EXPECTED_HASH = 0xd8d6b53a;

    private const int e_maxTasks = 128;

    // todo_erin move this to shared
    public static int SingleMultithreadingTest(int workerCount)
    {
        var tester = new b2TaskTester(workerCount, e_maxTasks);

        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.enqueueTask = tester.EnqueueTask;
        worldDef.finishTask = tester.FinishTask;
        worldDef.workerCount = workerCount;

        B2WorldId worldId = b2CreateWorld(ref worldDef);

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

        b2DestroyWorld(worldId);

        Assert.That(data.sleepStep == EXPECTED_SLEEP_STEP);
        Assert.That(data.hash == EXPECTED_HASH);

        DestroyFallingHinges(ref data);

        return 0;
    }

    // Test multithreaded determinism.
    [Test]
    public void MultithreadingTest()
    {
        for (int workerCount = 1; workerCount < 6; ++workerCount)
        {
            int result = SingleMultithreadingTest(workerCount);
            Assert.That(result == 0);
        }
    }

    // Test cross-platform determinism.
    [Test]
    public void CrossPlatformTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(ref worldDef);

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

        Assert.That(data.sleepStep == EXPECTED_SLEEP_STEP);
        Assert.That(data.hash == EXPECTED_HASH);

        DestroyFallingHinges(ref data);

        b2DestroyWorld(worldId);
    }
}