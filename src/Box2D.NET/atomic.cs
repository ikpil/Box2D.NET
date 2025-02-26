// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Threading;
using Box2D.NET.Primitives;

namespace Box2D.NET
{
    public static class atomic
    {
        public static void b2AtomicStoreInt(ref B2AtomicInt a, int value)
        {
            Interlocked.Exchange(ref a.value, value);
        }

        public static int b2AtomicLoadInt(ref B2AtomicInt a)
        {
            return Interlocked.Add(ref a.value, 0);
        }

        public static int b2AtomicFetchAddInt(ref B2AtomicInt a, int increment)
        {
            return Interlocked.Add(ref a.value, increment);
        }

        public static bool b2AtomicCompareExchangeInt(ref B2AtomicInt a, int expected, int desired)
        {
            return expected == Interlocked.CompareExchange(ref a.value, desired, expected);
        }

        public static void b2AtomicStoreU32(ref B2AtomicU32 a, uint value)
        {
            Interlocked.Exchange(ref a.value, value);
        }

        public static uint b2AtomicLoadU32(ref B2AtomicU32 a)
        {
            return (uint)Interlocked.Read(ref a.value);
        }
    }
}
