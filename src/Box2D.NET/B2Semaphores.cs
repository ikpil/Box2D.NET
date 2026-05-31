// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Threading;

namespace Box2D.NET
{
    public static class B2Semaphores
    {
        public static B2Semaphore b2CreateSemaphore(int initCount)
        {
            B2Semaphore s;
            s.semaphore = new SemaphoreSlim(initCount, int.MaxValue);
            return s;
        }

        public static void b2DestroySemaphore(ref B2Semaphore s)
        {
            s.semaphore?.Dispose();
            s.semaphore = null;
        }

        public static void b2WaitSemaphore(ref B2Semaphore s)
        {
            s.semaphore.Wait();
        }

        public static void b2SignalSemaphore(ref B2Semaphore s)
        {
            s.semaphore.Release();
        }
    }
}