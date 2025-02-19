// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System.Threading;

namespace Box2D.NET
{
    public class atomic
    {
        public static void b2AtomicStoreInt(ref b2AtomicInt a, int value)
        {
            Interlocked.Exchange(ref a.value, value);
        }

        public static int b2AtomicLoadInt(ref b2AtomicInt a)
        {
            return Interlocked.Or(ref a.value, 0);
        }

        public static int b2AtomicFetchAddInt(ref b2AtomicInt a, int increment)
        {
            return Interlocked.Add(ref a.value, increment);
        }

        public static bool b2AtomicCompareExchangeInt(ref b2AtomicInt a, int expected, int desired)
        {
            return expected == Interlocked.CompareExchange(ref a.value, desired, expected);
        }

        public static void b2AtomicStoreU32(ref b2AtomicU32 a, uint value)
        {
            Interlocked.Exchange(ref a.value, value);
        }

        public static uint b2AtomicLoadU32(ref b2AtomicU32 a)
        {
            return Interlocked.Or(ref a.value, 0);
        }
    }
}