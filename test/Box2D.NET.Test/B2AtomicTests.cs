// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using NUnit.Framework;
using static Box2D.NET.B2Atomics;

namespace Box2D.NET.Test;

public class B2AtomicTests
{
    [Test]
    public void Test_b2Atomic_Int_Store_And_Load()
    {
        B2AtomicInt atomic = new B2AtomicInt();
        b2AtomicStoreInt(ref atomic, 42);
        Assert.That(b2AtomicLoadInt(ref atomic), Is.EqualTo(42));
    }

    [Test]
    public void Test_b2Atomic_Int_FetchAdd()
    {
        B2AtomicInt atomic = new B2AtomicInt();
        b2AtomicStoreInt(ref atomic, 10);
        int original = b2AtomicFetchAddInt(ref atomic, 5);
        Assert.That(original, Is.EqualTo(10));
        Assert.That(b2AtomicLoadInt(ref atomic), Is.EqualTo(15));
    }

    [Test]
    public void Test_b2Atomic_Int_CompareExchange()
    {
        B2AtomicInt atomic = new B2AtomicInt();
        b2AtomicStoreInt(ref atomic, 100);

        // Success case
        bool exchanged = b2AtomicCompareExchangeInt(ref atomic, 100, 200);
        Assert.That(exchanged, Is.True);
        Assert.That(b2AtomicLoadInt(ref atomic), Is.EqualTo(200));

        // Fail case
        exchanged = b2AtomicCompareExchangeInt(ref atomic, 100, 300);
        Assert.That(exchanged, Is.False);
        Assert.That(b2AtomicLoadInt(ref atomic), Is.EqualTo(200));
    }

    [Test]
    public void Test_b2Atomic_U32_Store_And_Load()
    {
        B2AtomicU32 atomic = new B2AtomicU32();
        b2AtomicStoreU32(ref atomic, 123456789u);
        Assert.That(b2AtomicLoadU32(ref atomic), Is.EqualTo(123456789u));
    }
    
    [Test]
    public void Test_b2Atomic_Int_FetchAdd_IsThreadSafe()
    {
        B2AtomicInt atomic = new B2AtomicInt();
        int threadCount = (int)Math.Round(Environment.ProcessorCount * 2.5f);
        int addsPerThread = 100_000;
        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < addsPerThread; j++)
                {
                    b2AtomicFetchAddInt(ref atomic, 1);
                }
            });
            threads[i].Start();
        }

        foreach (Thread t in threads)
            t.Join();

        Assert.That(b2AtomicLoadInt(ref atomic), Is.EqualTo(threadCount * addsPerThread));
    }

}
