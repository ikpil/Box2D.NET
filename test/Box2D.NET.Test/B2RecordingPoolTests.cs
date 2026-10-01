// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Recordings;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Box2D.NET.Test
{
    public class B2RecordingPoolTests
    {
        private sealed class RecordingScene : IDisposable
        {
            public readonly string path = Path.Combine(Path.GetTempPath(), $"box2d-pool-{Guid.NewGuid():N}.b2rec");
            public readonly B2WorldId worldId;

            public RecordingScene(bool record = true)
            {
                B2WorldDef def = b2DefaultWorldDef();
                def.recordingPath = record ? path : null;
                worldId = b2CreateWorld(def);
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.position = new B2Vec2(0.0f, -10.0f);
                B2BodyId body = b2CreateBody(worldId, bodyDef);
                b2CreateCircleShape(body, b2DefaultShapeDef(), new B2Circle(new B2Vec2(), 10.0f));
                b2World_Step(worldId, 1.0f / 60.0f, 4);
            }

            public void Dispose()
            {
                if (b2World_IsValid(worldId))
                {
                    b2DestroyWorld(worldId);
                }
                File.Delete(path);
            }

            public void VerifyRecording(int queryCount)
            {
                b2World_StopRecording(worldId);
                Assert.That(b2ValidateReplayFile(path, 0), Is.True);
                B2RecPlayer player = b2RecPlayer_Create(path, 0);
                Assert.That(player, Is.Not.Null);
                try
                {
                    Assert.That(b2RecPlayer_StepFrame(player), Is.True);
                    Assert.That(b2RecPlayer_GetFrameQueryCount(player), Is.EqualTo(queryCount));
                    for (int i = 0; i < queryCount; ++i)
                    {
                        Assert.That(b2RecPlayer_GetFrameQuery(player, i).hitCount, Is.EqualTo(1));
                    }
                    Assert.That(b2RecPlayer_HasDiverged(player), Is.False);
                }
                finally
                {
                    b2RecPlayer_Destroy(player);
                }
            }
        }

        // All five callback query paths must release their writer, including on callback failure.
        private static void Query(B2WorldId worldId, int kind, Action onHit)
        {
            B2QueryFilter filter = b2DefaultQueryFilter();
            switch (kind)
            {
                case 0:
                    b2World_OverlapAABB(worldId, new B2AABB(new B2Vec2(-1, -1), new B2Vec2(1, 1)),
                        filter, (_, context) => { ((Action)context)(); return true; }, onHit);
                    break;
                case 1:
                    B2ShapeProxy overlap = b2MakeProxy(new B2Vec2(), 1, 0.5f);
                    b2World_OverlapShape(worldId, ref overlap, filter,
                        (_, context) => { ((Action)context)(); return true; }, onHit);
                    break;
                case 2:
                    b2World_CastRay(worldId, new B2Vec2(0, 5), new B2Vec2(0, -10), filter, Cast, onHit);
                    break;
                case 3:
                    B2ShapeProxy cast = b2MakeProxy(new B2Vec2(0, 5), 1, 0.5f);
                    b2World_CastShape(worldId, ref cast, new B2Vec2(0, -10), filter, Cast, onHit);
                    break;
                case 4:
                    B2Capsule mover = new B2Capsule(new B2Vec2(-0.3f, 0), new B2Vec2(0.3f, 0), 0.5f);
                    b2World_CollideMover(worldId, mover, filter, Plane, onHit);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static float Cast(B2ShapeId id, B2Vec2 point, B2Vec2 normal, float fraction, ref Action action)
        {
            action();
            return 1.0f;
        }

        private static bool Plane(B2ShapeId id, ref B2PlaneResult plane, object context)
        {
            ((Action)context)();
            return true;
        }

        private static object Field(object value, string name)
        {
            return value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(value);
        }

        private static object[] Writers(object pool)
        {
            return ((IEnumerable)Field(pool, "_items")).Cast<object>().ToArray();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void CallbackFailureReturnsWriterAndReusesBuffer(int kind)
        {
            using RecordingScene scene = new RecordingScene();
            B2World world = b2GetWorldFromId(scene.worldId);
            int hits = 0;
            Query(scene.worldId, kind, () => hits++);
            Assert.That(hits, Is.EqualTo(1));
            object pool = Field(world, "queryWriterPool");
            object writer = Writers(pool).Single();
            object buffer = Field(Field(writer, "buf"), "data");
            Assert.That(buffer, Is.Not.Null);

            Assert.That(() => Query(scene.worldId, kind,
                () => throw new InvalidOperationException("callback failure")), Throws.TypeOf<InvalidOperationException>());

            Assert.That(Writers(pool).Single(), Is.SameAs(writer));
            Assert.That(Field(writer, "userContext"), Is.Null);
            Assert.That(Field(writer, "owner"), Is.Null);
            Assert.That(Field(Field(writer, "userFcn"), "value"), Is.Null);
            Assert.That(Field(Field(writer, "buf"), "size"), Is.EqualTo(0));
            Assert.That(Field(Field(writer, "buf"), "data"), Is.SameAs(buffer));

            Query(scene.worldId, kind, () => hits++);
            Assert.That(hits, Is.EqualTo(2));
            Assert.That(Writers(pool).Single(), Is.SameAs(writer));
            Assert.That(Field(Field(writer, "buf"), "data"), Is.SameAs(buffer));
            // The failed query must not commit a partial record or leak its hit count.
            scene.VerifyRecording(2);
            // Stopping recording leaves world-owned scratch objects available, but ordinary
            // queries must neither rent a writer nor create another pool.
            Assert.That(b2World_IsValid(scene.worldId), Is.True);
            Assert.That(Field(world, "recording"), Is.Null);
            Assert.That(Field(world, "queryWriterPool"), Is.SameAs(pool));
            Assert.That(Writers(pool).Single(), Is.SameAs(writer));
            Query(scene.worldId, kind, () => hits++);
            Assert.That(hits, Is.EqualTo(3));
            Assert.That(Writers(pool).Single(), Is.SameAs(writer));
            Assert.That(Field(Field(writer, "buf"), "data"), Is.SameAs(buffer));
        }

        [Test]
        public void ConcurrentAndNestedQueriesHaveIndependentWriters()
        {
            using RecordingScene scene = new RecordingScene();
            const int workerCount = 4;
            const int iterations = 25;
            using Barrier barrier = new Barrier(workerCount);
            Task[] tasks = Enumerable.Range(0, workerCount).Select(worker => Task.Factory.StartNew(() =>
            {
                int hits = 0;
                for (int i = 0; i < iterations; ++i)
                {
                    int kind = (worker + i) % 5;
                    Query(scene.worldId, kind, () =>
                    {
                        if (i == 0 && !barrier.SignalAndWait(TimeSpan.FromSeconds(10)))
                        {
                            throw new TimeoutException("Concurrent query callbacks could not run together.");
                        }
                        hits++;
                        Query(scene.worldId, (kind + 1) % 5, () => hits++);
                    });
                }
                Assert.That(hits, Is.EqualTo(iterations * 2));
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
            Task.WaitAll(tasks);
            scene.VerifyRecording(workerCount * iterations * 2);
        }

        [Test]
        public void QueriesWithoutRecordingDoNotRentWriters()
        {
            using RecordingScene scene = new RecordingScene(false);
            int hits = 0;
            for (int kind = 0; kind < 5; ++kind)
            {
                Query(scene.worldId, kind, () => hits++);
            }
            Assert.That(hits, Is.EqualTo(5));
            B2World world = b2GetWorldFromId(scene.worldId);
            Assert.That(Field(world, "recording"), Is.Null);
            Assert.That(Field(world, "queryWriterPool"), Is.Null);
            Assert.That(Field(world, "queryWriterPoolLock"), Is.Null);
        }

        [Test]
        public void WorldDestructionClosesPoolAndReusedSlotStartsWithoutAPool()
        {
            using RecordingScene first = new RecordingScene();
            Query(first.worldId, 0, () => { });
            B2World world = b2GetWorldFromId(first.worldId);
            object pool = Field(world, "queryWriterPool");
            object writer = Writers(pool).Single();

            // Exercise teardown after recording has already stopped as well.
            b2World_StopRecording(first.worldId);
            b2DestroyWorld(first.worldId);
            Assert.That(Field(world, "queryWriterPool"), Is.Null);
            Assert.That(Field(world, "queryWriterPoolLock"), Is.Null);
            Assert.That(Writers(pool), Is.Empty);

            using RecordingScene second = new RecordingScene();
            Assert.That(b2GetWorldFromId(second.worldId), Is.SameAs(world));
            Assert.That(second.worldId.generation, Is.Not.EqualTo(first.worldId.generation));
            Assert.That(Field(world, "queryWriterPool"), Is.Null);
            Query(second.worldId, 0, () => { });
            object nextPool = Field(world, "queryWriterPool");
            Assert.That(nextPool, Is.Not.SameAs(pool));
            Assert.That(Writers(nextPool).Single(), Is.Not.SameAs(writer));
        }

        [Test]
        public void WorldsHaveSeparatePools()
        {
            using RecordingScene first = new RecordingScene();
            using RecordingScene second = new RecordingScene();
            Query(first.worldId, 0, () => { });
            Query(second.worldId, 0, () => { });
            object firstPool = Field(b2GetWorldFromId(first.worldId), "queryWriterPool");
            object secondPool = Field(b2GetWorldFromId(second.worldId), "queryWriterPool");
            Assert.That(firstPool, Is.Not.SameAs(secondPool));
            Assert.That(Writers(firstPool).Single(), Is.Not.SameAs(Writers(secondPool).Single()));
        }

        [Test]
        public void WarmWriterRentAndReturnDoesNotAllocate()
        {
            using RecordingScene scene = new RecordingScene();
            B2World world = b2GetWorldFromId(scene.worldId);
            MethodInfo method = typeof(B2Recordings).GetMethod("b2RentQueryWriterIfRecording",
                BindingFlags.NonPublic | BindingFlags.Static);
            // Bind once instead of including reflection argument arrays in the measurement.
            Func<B2World, IDisposable> rent = (Func<B2World, IDisposable>)method.CreateDelegate(typeof(Func<B2World, IDisposable>));
            for (int i = 0; i < 1000; ++i)
            {
                rent(world).Dispose();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; ++i)
            {
                rent(world).Dispose();
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0));
        }

        [TestCase(65536, true)]
        [TestCase(65537, false)]
        public void ReturningWriterOnlyRetainsBuffersWithinCapacityLimit(int capacity, bool retained)
        {
            using RecordingScene scene = new RecordingScene();
            B2World world = b2GetWorldFromId(scene.worldId);
            MethodInfo rent = typeof(B2Recordings).GetMethod("b2RentQueryWriterIfRecording",
                BindingFlags.NonPublic | BindingFlags.Static);
            object writer = rent.Invoke(null, new object[] { world });
            object pool = Field(world, "queryWriterPool");
            try
            {
                // Inject a buffer at the exact policy boundary without depending on growth
                // increments or the number of shapes visited by a query.
                byte[] bytes = new byte[capacity];
                object buf = Field(writer, "buf");
                Type type = buf.GetType();
                type.GetField("data", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(buf, bytes);
                type.GetField("capacity", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(buf, capacity);
                type.GetField("size", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(buf, 1);
                writer.GetType().GetField("buf", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(writer, buf);

                ((IDisposable)writer).Dispose();
                // An immediate duplicate Dispose must not enqueue the same writer twice.
                ((IDisposable)writer).Dispose();
                Assert.That(Writers(pool).Single(), Is.SameAs(writer));
                Assert.That(Field(Field(writer, "buf"), "size"), Is.EqualTo(0));
                Assert.That(Field(Field(writer, "buf"), "capacity"), Is.EqualTo(retained ? capacity : 0));
                if (retained)
                {
                    Assert.That(Field(Field(writer, "buf"), "data"), Is.SameAs(bytes));
                }
                else
                {
                    Assert.That(Field(Field(writer, "buf"), "data"), Is.Null);
                }
            }
            finally
            {
                ((IDisposable)writer).Dispose();
            }

            Query(scene.worldId, 0, () => { });
            Assert.That(Writers(pool).Single(), Is.SameAs(writer));
            scene.VerifyRecording(1);
        }
    }
}
