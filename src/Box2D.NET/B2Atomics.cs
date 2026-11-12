// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using System.Threading;

namespace Box2D.NET
{
    public static class B2Atomics
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void b2Pause()
        {
            // C uses _mm_pause() / __yield(). Thread.SpinWait(1) emits the same
            // pause/yield hint on x86 and ARM. Task.Yield() was a no-op here because
            // the returned awaitable was never awaited.
            Thread.SpinWait(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2AtomicStoreInt(ref B2AtomicInt a, int value)
        {
            Interlocked.Exchange(ref a.value, value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2AtomicLoadInt(ref B2AtomicInt a)
        {
            return Interlocked.CompareExchange(ref a.value, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2AtomicFetchAddInt(ref B2AtomicInt a, int increment)
        {
            return Interlocked.Add(ref a.value, increment) - increment;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2AtomicCompareExchangeInt(ref B2AtomicInt a, int expected, int desired)
        {
            return expected == Interlocked.CompareExchange(ref a.value, desired, expected);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2AtomicStoreU32(ref B2AtomicU32 a, uint value)
        {
            Interlocked.Exchange(ref a.value, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint b2AtomicLoadU32(ref B2AtomicU32 a)
        {
            return (uint)Interlocked.Read(ref a.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint b2AtomicLoadU32Raw(ref uint a)
        {
            return (uint)Volatile.Read(ref Unsafe.As<uint, int>(ref a));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint b2AtomicFetchOrU32(ref uint a, uint mask)
        {
            ref int target = ref Unsafe.As<uint, int>(ref a);
            int current = Volatile.Read(ref target);
            while (true)
            {
                int previous = Interlocked.CompareExchange(ref target, current | (int)mask, current);
                if (previous == current)
                {
                    return (uint)previous;
                }

                current = previous;
            }
        }
    }
}
