// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2ParallelFors;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Test;

public class B2StepReuseTests
{
    [Test]
    public void RemovedReferenceReturnsToSpareSlotWithoutAliasing()
    {
        var array = b2Array_Create<B2BodyState>(3);
        var first = b2Array_Emplace(ref array);
        first.angularVelocity = 17;
        var second = b2Array_Emplace(ref array);
        Assert.That(b2Array_RemoveSwap(ref array, 0), Is.EqualTo(1));
        Assert.That(array.data[0], Is.SameAs(second));
        Assert.That(b2Array_Emplace(ref array), Is.SameAs(first));
        Assert.That(array.data[0], Is.Not.SameAs(array.data[1]));
        b2Array_Destroy(ref array);
    }

    [Test]
    public void SleepingSetRetainsCapacityAndWorldDestructionReleasesIt()
    {
        var worldId = b2CreateWorld(b2DefaultWorldDef());
        var world = b2GetWorldFromId(worldId);
        B2SolverSet sleeping = null;
        try
        {
            var definition = b2DefaultBodyDef();
            definition.type = B2BodyType.b2_dynamicBody;
            var id = b2CreateBody(worldId, definition);
            b2Body_SetAwake(id, false);
            var body = b2GetBodyFullId(world, id);
            sleeping = world.solverSets.data[body.setIndex];
            var storage = sleeping.bodySims.data;
            for (int i = 0; i < 32; ++i)
            {
                b2Body_SetAwake(id, true);
                Assert.That(sleeping.bodySims.count, Is.Zero);
                Assert.That(sleeping.bodySims.data, Is.SameAs(storage));
                b2Body_SetAwake(id, false);
                Assert.That(world.solverSets.data[body.setIndex], Is.SameAs(sleeping));
                Assert.That(sleeping.bodySims.data, Is.SameAs(storage));
            }
            b2Body_SetAwake(id, true);
        }
        finally
        {
            b2DestroyWorld(worldId);
        }
        Assert.That(sleeping.bodySims.data, Is.Null);
        Assert.That(sleeping.islandSims.data, Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StaticJointAndWarmedSerialStepAllocateNothing(bool wheel)
    {
        var worldId = b2CreateWorld(b2DefaultWorldDef());
        try
        {
            var ground = b2CreateBody(worldId, b2DefaultBodyDef());
            var definition = b2DefaultBodyDef();
            definition.type = B2BodyType.b2_dynamicBody;
            definition.enableSleep = false;
            var body = b2CreateBody(worldId, definition);
            if (wheel)
            {
                var joint = b2DefaultWheelJointDef();
                joint.@base.bodyIdA = ground;
                joint.@base.bodyIdB = body;
                b2CreateWheelJoint(worldId, joint);
            }
            else
            {
                var joint = b2DefaultRevoluteJointDef();
                joint.@base.bodyIdA = ground;
                joint.@base.bodyIdB = body;
                b2CreateRevoluteJoint(worldId, joint);
            }
            // Warm the allocation counter, JIT and storage before measuring steady steps.
            _ = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1600; ++i)
                b2World_Step(worldId, 1f / 60, 4);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 64; ++i)
                b2World_Step(worldId, 1f / 60, 4);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(bytes, Is.Zero);
        }
        finally { b2DestroyWorld(worldId); }
    }

    [Test]
    public void ParallelForJoinsFailuresAndCanReuseItsStorage()
    {
        var definition = b2DefaultWorldDef();
        definition.workerCount = 4;
        var worldId = b2CreateWorld(definition);
        var world = b2GetWorldFromId(worldId);
        try
        {
            var visits = new int[257];
            var threads = new int[4];
            b2ParallelForCallback visit = (start, end, worker, _) =>
            {
                int thread = Environment.CurrentManagedThreadId;
                int previous = Interlocked.CompareExchange(ref threads[worker], thread, 0);
                if (previous != 0 && previous != thread)
                    throw new Exception("Worker identity changed during a range.");
                for (int i = start; i < end; ++i)
                    Interlocked.Increment(ref visits[i]);
            };
            b2ParallelFor(world, visit, visits.Length, 7, null);
            Assert.That(visits.All(count => count == 1), Is.True);
            Assert.That(world.activeTaskCount, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => b2ParallelFor(world, (_, _, _, _) => throw new Exception("Expected callback failure"), 257, 7, null));
            Assert.That(world.activeTaskCount, Is.Zero);
            // Drain further calls after scheduler slots are exhausted as well.
            for (int run = 0; run < 20; ++run)
            {
                Array.Clear(visits);
                Array.Clear(threads);
                b2ParallelFor(world, visit, visits.Length, 7, null);
                Assert.That(visits.All(count => count == 1), Is.True);
            }
        }
        finally { b2DestroyWorld(worldId); }
    }

    [Test]
    public void NestedParallelForKeepsOuterBatchState()
    {
        var world = new B2World { workerCount = 2 };
        world.enqueueTaskFcn = (callback, context, _) => { callback(context); return null; };
        world.finishTaskFcn = (_, _) => { };
        int outer = 0, inner = 0;
        b2ParallelFor(world, (start, end, _, _) =>
        {
            outer += end - start;
            if (start == 0)
            {
                b2ParallelFor(world, (a, b, _, _) => inner += b - a, 113, 7, null);
            }
        }, 257, 7, null);
        Assert.That(outer, Is.EqualTo(257));
        Assert.That(inner, Is.EqualTo(113));
    }

    [Test]
    public void FatalConstraintFailureWakesPeersAndJoinsEveryTask()
    {
        int started = 0, finished = 0;
        var definition = b2DefaultWorldDef();
        definition.workerCount = 2;
        definition.enqueueTask = (callback, context, _) =>
        {
            // Inject a failure after constraints have been prepared, before workers enter the solver.
            if (context is B2WorkerContext worker && worker.workerIndex == 0)
                worker.context.states = null;
            Interlocked.Increment(ref started);
            return Task.Run(() => callback(context));
        };
        definition.finishTask = (handle, _) =>
        {
            ((Task)handle).GetAwaiter().GetResult();
            Interlocked.Increment(ref finished);
        };
        var worldId = b2CreateWorld(definition);
        var world = b2GetWorldFromId(worldId);
        try
        {
            var body = b2DefaultBodyDef();
            body.type = B2BodyType.b2_dynamicBody;
            for (int i = 0; i < 128; ++i)
                b2CreateBody(worldId, body);
            var step = Task.Run(() => Assert.Throws<InvalidOperationException>(() => b2World_Step(worldId, 1f / 60, 4)));
            Assert.That(step.Wait(TimeSpan.FromSeconds(5)), Is.True, "Constraint failure stranded a worker.");
            Assert.That(finished, Is.EqualTo(started));
            Assert.That(world.activeTaskCount, Is.Zero);
            Assert.That(world.locked, Is.False);
        }
        finally { b2DestroyWorld(worldId); }
    }
}
