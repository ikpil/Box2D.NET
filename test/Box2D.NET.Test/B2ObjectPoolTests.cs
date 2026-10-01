// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Box2D.NET.Test
{
    public class B2ObjectPoolTests
    {
        [Test]
        public void PoolCreatesLazilyAndResetsBeforeReuse()
        {
            int created = 0;
            int resets = 0;
            using B2ObjectPool<int[]> pool = new B2ObjectPool<int[]>(
                () => { created++; return new int[1]; },
                item => { resets++; item[0] = 0; return true; });
            Assert.That(created, Is.EqualTo(0));
            int[] item = pool.Get();
            item[0] = 42;
            Assert.That(created, Is.EqualTo(1));
            Assert.That(pool.Count, Is.EqualTo(0));
            pool.Return(item);
            Assert.That(resets, Is.EqualTo(1));
            Assert.That(item[0], Is.EqualTo(0));
            Assert.That(pool.Count, Is.EqualTo(1));
            int[] next = pool.Get();
            Assert.That(next, Is.SameAs(item));
            Assert.That(created, Is.EqualTo(1));
            pool.Return(next);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void LimitOnlyRestrictsRetentionNotBorrowing(int maximumRetained)
        {
            int resets = 0;
            using B2ObjectPool<object> pool = new B2ObjectPool<object>(
                () => new object(), _ => { resets++; return true; }, maximumRetained);
            object first = pool.Get();
            object second = pool.Get();
            object third = pool.Get();
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(third, Is.Not.SameAs(first));
            Assert.That(third, Is.Not.SameAs(second));
            pool.Return(first);
            pool.Return(second);
            pool.Return(third);
            Assert.That(resets, Is.EqualTo(3));
            Assert.That(pool.Count, Is.EqualTo(maximumRetained));
        }

        [Test]
        public void PolicyCanRejectRetention()
        {
            using B2ObjectPool<object> pool = new B2ObjectPool<object>(() => new object(), _ => false);
            object item = pool.Get();
            pool.Return(item);
            Assert.That(pool.Count, Is.EqualTo(0));
            object next = pool.Get();
            Assert.That(next, Is.Not.SameAs(item));
            pool.Return(next);
        }

        [Test]
        public void ClosedPoolResetsLateReturnsWithoutRetainingThem()
        {
            int resets = 0;
            B2ObjectPool<object> pool = new B2ObjectPool<object>(
                () => new object(), _ => { resets++; return true; });
            object idle = pool.Get();
            object outstanding = pool.Get();
            pool.Return(idle);
            pool.Dispose();
            pool.Dispose();
            Assert.That(pool.Count, Is.EqualTo(0));
            Assert.That(() => pool.Get(), Throws.TypeOf<ObjectDisposedException>());
            pool.Return(outstanding);
            Assert.That(resets, Is.EqualTo(2));
            Assert.That(pool.Count, Is.EqualTo(0));
        }

        [Test]
        public void PolicyRunsOutsideStorageLock()
        {
            B2ObjectPool<object> pool = null;
            pool = new B2ObjectPool<object>(
                () => { Task.Run(() => pool.Count).WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult(); return new object(); },
                _ => { Task.Run(() => pool.Count).WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult(); return true; });
            using (pool)
            {
                pool.Return(pool.Get());
            }
        }

        [Test]
        public void ConcurrentBorrowersNeverShareAnActiveObject()
        {
            const int workerCount = 4;
            using B2ObjectPool<object> pool = new B2ObjectPool<object>(() => new object(), _ => true, 2);
            ConcurrentDictionary<object, byte> active = new ConcurrentDictionary<object, byte>();
            using Barrier barrier = new Barrier(workerCount);
            Task[] tasks = new Task[workerCount];
            for (int i = 0; i < workerCount; ++i)
            {
                tasks[i] = Task.Factory.StartNew(() =>
                {
                    for (int iteration = 0; iteration < 50; ++iteration)
                    {
                        object item = pool.Get();
                        try
                        {
                            Assert.That(active.TryAdd(item, 0), Is.True);
                            Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
                            Assert.That(active.TryRemove(item, out _), Is.True);
                        }
                        finally
                        {
                            pool.Return(item);
                        }
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            Task.WaitAll(tasks);
            Assert.That(active, Is.Empty);
            Assert.That(pool.Count, Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void InvalidArgumentsAndNullFactoryResultAreRejected()
        {
            Assert.That(() => new B2ObjectPool<object>(null, _ => true), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new B2ObjectPool<object>(() => new object(), null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new B2ObjectPool<object>(() => new object(), _ => true, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            using B2ObjectPool<object> pool = new B2ObjectPool<object>(() => null, _ => true);
            Assert.That(() => pool.Get(), Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => pool.Return(null), Throws.TypeOf<ArgumentNullException>());
        }
    }
}
