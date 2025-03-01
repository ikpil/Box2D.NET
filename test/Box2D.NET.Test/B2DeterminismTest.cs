// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Cores;

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
        
        Debug.Assert(0 >= _runningTasks.Count);
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
            int loop = 0;
            int idx = 0;
            foreach (var count in Next(itemCount, minRange))
            {
                int startIndex = idx;
                int endIndex = idx + count;
                idx = endIndex;

                uint workerIndex = (uint)(++loop % _workerCount);
                var task = Task.Run(async () =>
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        box2dTask(startIndex, endIndex, workerIndex, box2dContext);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
                
                _runningTasks.Enqueue(task);
            }

            ++taskCount;

            return box2dTask;
        }
        else
        {
            box2dTask(0, itemCount, 0, box2dContext);

            return null;
        }
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
    private const int e_columns = 10;
    private const int e_rows = 10;
    private const int e_count = e_columns * e_rows;
    private const int e_maxTasks = 128;

    private B2Vec2[][] finalPositions = new B2Vec2[][] { new B2Vec2[e_count], new B2Vec2[e_count] };
    private B2Rot[][] finalRotations = new B2Rot[][] { new B2Rot[e_count], new B2Rot[e_count] };


    // todo_erin move this to shared
    public void TiltedStacks(int testIndex, int workerCount)
    {
        var tester = new b2TaskTester(workerCount, e_maxTasks);

        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.enqueueTask = tester.EnqueueTask;
        worldDef.finishTask = tester.FinishTask;
        worldDef.workerCount = workerCount;
        worldDef.enableSleep = false;

        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyId[] bodies = new B2BodyId[e_count];

        {
            B2BodyDef bd = b2DefaultBodyDef();
            bd.position = new B2Vec2(0.0f, -1.0f);
            B2BodyId groundId = b2CreateBody(worldId, bd);

            B2Polygon box = b2MakeBox(1000.0f, 1.0f);
            B2ShapeDef sd = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, sd, box);
        }

        {
            B2Polygon box = b2MakeRoundedBox(0.45f, 0.45f, 0.05f);
            B2ShapeDef sd = b2DefaultShapeDef();
            sd.density = 1.0f;
            sd.friction = 0.3f;

            float offset = 0.2f;
            float dx = 5.0f;
            float xroot = -0.5f * dx * (e_columns - 1.0f);

            for (int j = 0; j < e_columns; ++j)
            {
                float x = xroot + j * dx;

                for (int i = 0; i < e_rows; ++i)
                {
                    B2BodyDef bd = b2DefaultBodyDef();
                    bd.type = B2BodyType.b2_dynamicBody;

                    int n = j * e_rows + i;

                    bd.position = new B2Vec2(x + offset * i, 0.5f + 1.0f * i);
                    B2BodyId bodyId = b2CreateBody(worldId, bd);
                    bodies[n] = bodyId;

                    b2CreatePolygonShape(bodyId, sd, box);
                }
            }
        }

        float timeStep = 1.0f / 60.0f;
        int subStepCount = 3;

        for (int i = 0; i < 100; ++i)
        {
            b2World_Step(worldId, timeStep, subStepCount);
            tester.taskCount = 0;
            TracyCFrameMark();
        }

        for (int i = 0; i < e_count; ++i)
        {
            finalPositions[testIndex][i] = b2Body_GetPosition(bodies[i]);
            finalRotations[testIndex][i] = b2Body_GetRotation(bodies[i]);
        }

        b2DestroyWorld(worldId);
    }

    // Test multithreaded determinism.
    [Test]
    public void MultithreadingTest()
    {
        // Test 1 : 4 threads
        TiltedStacks(0, 4);

        // Test 2 : 1 thread
        TiltedStacks(1, 1);

        // Both runs should produce identical results
        for (int i = 0; i < e_count; ++i)
        {
            B2Vec2 p1 = finalPositions[0][i];
            B2Vec2 p2 = finalPositions[1][i];
            B2Rot rot1 = finalRotations[0][i];
            B2Rot rot2 = finalRotations[1][i];

            Assert.That(p1.x, Is.EqualTo(p2.x));
            Assert.That(p1.y, Is.EqualTo(p2.y));
            Assert.That(rot1.c, Is.EqualTo(rot2.c));
            Assert.That(rot1.s, Is.EqualTo(rot2.s));
        }
    }

    // Test cross platform determinism based on the FallingHinges sample.
    [Test]
    public void CrossPlatformTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(0.0f, -1.0f);
            B2BodyId groundId = b2CreateBody(worldId, bodyDef);

            B2Polygon box = b2MakeBox(20.0f, 1.0f);
            B2ShapeDef shapeDef = b2DefaultShapeDef();
            b2CreatePolygonShape(groundId, shapeDef, box);
        }

        {
            int columnCount = 4;
            int rowCount = 30;
            int bodyCount = rowCount * columnCount;

            B2BodyId[] bodies = new B2BodyId[bodyCount];

            float h = 0.25f;
            float r = 0.1f * h;
            B2Polygon box = b2MakeRoundedBox(h - r, h - r, r);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.friction = 0.3f;

            float offset = 0.4f * h;
            float dx = 10.0f * h;
            float xroot = -0.5f * dx * (columnCount - 1.0f);

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.enableLimit = true;
            jointDef.lowerAngle = -0.1f * B2_PI;
            jointDef.upperAngle = 0.2f * B2_PI;
            jointDef.enableSpring = true;
            jointDef.hertz = 0.5f;
            jointDef.dampingRatio = 0.5f;
            jointDef.localAnchorA = new B2Vec2(h, h);
            jointDef.localAnchorB = new B2Vec2(offset, -h);
            jointDef.drawSize = 0.1f;

            int bodyIndex = 0;

            for (int j = 0; j < columnCount; ++j)
            {
                float x = xroot + j * dx;

                B2BodyId prevBodyId = b2_nullBodyId;

                for (int i = 0; i < rowCount; ++i)
                {
                    B2BodyDef bodyDef = b2DefaultBodyDef();
                    bodyDef.type = B2BodyType.b2_dynamicBody;

                    bodyDef.position.x = x + offset * i;
                    bodyDef.position.y = h + 2.0f * h * i;

                    // this tests the deterministic cosine and sine functions
                    bodyDef.rotation = b2MakeRot(0.1f * i - 1.0f);

                    B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

                    if ((i & 1) == 0)
                    {
                        prevBodyId = bodyId;
                    }
                    else
                    {
                        jointDef.bodyIdA = prevBodyId;
                        jointDef.bodyIdB = bodyId;
                        b2CreateRevoluteJoint(worldId, jointDef);
                        prevBodyId = b2_nullBodyId;
                    }

                    b2CreatePolygonShape(bodyId, shapeDef, box);

                    Debug.Assert(bodyIndex < bodyCount);
                    bodies[bodyIndex] = bodyId;

                    bodyIndex += 1;
                }
            }

            Debug.Assert(bodyIndex == bodyCount);

            uint hash = 0;
            int sleepStep = -1;
            float timeStep = 1.0f / 60.0f;

            int stepCount = 0;
            int maxSteps = 500;
            while (stepCount < maxSteps)
            {
                int subStepCount = 4;
                b2World_Step(worldId, timeStep, subStepCount);
                TracyCFrameMark();

                if (hash == 0)
                {
                    B2BodyEvents bodyEvents = b2World_GetBodyEvents(worldId);

                    if (bodyEvents.moveCount == 0)
                    {
                        int awakeCount = b2World_GetAwakeBodyCount(worldId);
                        Assert.That(awakeCount, Is.EqualTo(0));

                        hash = B2_HASH_INIT;
                        for (int i = 0; i < bodyCount; ++i)
                        {
                            B2Transform xf = b2Body_GetTransform(bodies[i]);
                            byte[] bxf = new byte[sizeof(float) * 4];
                            xf.TryWriteBytes(bxf);
                            hash = b2Hash(hash, bxf, bxf.Length);
                        }

                        sleepStep = stepCount;
                        Console.Write("step = %d, hash = 0x%08x\n", sleepStep, hash);

                        break;
                    }
                }

                stepCount += 1;
            }

            Assert.That(stepCount, Is.LessThan(maxSteps));
            Assert.That(sleepStep, Is.EqualTo(263));
            Assert.That(hash, Is.EqualTo(0x7de58fbe));

            //free(bodies);

            b2DestroyWorld(worldId);
        }
    }
}
