// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using static Box2D.NET.B2Mutexes;
using static Box2D.NET.B2Semaphores;
using static Box2D.NET.B2Threads;

namespace Box2D.NET.Test;

public class B2ThreadTest
{
    private sealed class SemData
    {
        public B2Semaphore sem;
        public int value;
    }

    private static void SemWorker(object context)
    {
        SemData data = (SemData)context;
        data.value = 99;
        b2SignalSemaphore(ref data.sem);
    }

    [Test]
    public void SemaphoreCreateDestroyTest()
    {
        B2Semaphore s = b2CreateSemaphore(0);
        Assert.That(s.semaphore, Is.Not.Null);
        b2DestroySemaphore(ref s);
    }

    [Test]
    public void SemaphoreSignalWaitTest()
    {
        SemData data = new SemData
        {
            sem = b2CreateSemaphore(0),
            value = 0,
        };

        B2Thread thread = b2CreateThread(SemWorker, data, "sem test");
        b2WaitSemaphore(ref data.sem);

        Assert.That(data.value, Is.EqualTo(99));

        b2JoinThread(thread);
        b2DestroySemaphore(ref data.sem);
    }

    [Test]
    public void SemaphoreInitialCountTest()
    {
        B2Semaphore s = b2CreateSemaphore(3);

        b2WaitSemaphore(ref s);
        b2WaitSemaphore(ref s);
        b2WaitSemaphore(ref s);

        b2SignalSemaphore(ref s);
        b2WaitSemaphore(ref s);

        b2DestroySemaphore(ref s);
    }

    [Test]
    public void ThreadCreateJoinTest()
    {
        SemData data = new SemData
        {
            sem = b2CreateSemaphore(0),
            value = 0,
        };

        B2Thread thread = b2CreateThread(SemWorker, data, "join test");
        b2JoinThread(thread);

        Assert.That(data.value, Is.EqualTo(99));

        b2DestroySemaphore(ref data.sem);
    }

    private sealed class SumData
    {
        public B2Mutex mutex;
        public int sum;
    }

    private static void SumWorker(object context)
    {
        SumData data = (SumData)context;
        for (int i = 0; i < 1000; ++i)
        {
            b2LockMutex(ref data.mutex);
            data.sum += 1;
            b2UnlockMutex(ref data.mutex);
        }
    }

    [Test]
    public void ThreadMultipleTest()
    {
        SumData data = new SumData
        {
            mutex = b2CreateMutex(),
            sum = 0,
        };

        const int threadCount = 4;
        B2Thread[] threads = new B2Thread[threadCount];
        for (int i = 0; i < threadCount; ++i)
        {
            threads[i] = b2CreateThread(SumWorker, data, $"sum test {i}");
        }

        for (int i = 0; i < threadCount; ++i)
        {
            b2JoinThread(threads[i]);
        }

        Assert.That(data.sum, Is.EqualTo(threadCount * 1000));

        b2DestroyMutex(ref data.mutex);
    }
}
