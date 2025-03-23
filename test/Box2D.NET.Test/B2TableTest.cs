// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using NUnit.Framework;
using static Box2D.NET.B2CTZs;
using static Box2D.NET.B2Tables;
using static Box2D.NET.B2Timers;
#if B2_SNOOP_TABLE_COUNTERS
using static Box2D.NET.B2Atomics;
#endif

namespace Box2D.NET.Test;

public class B2TableTest
{
    public const int SET_SPAN = 317;
    public const int ITEM_COUNT = ((SET_SPAN * SET_SPAN - SET_SPAN) / 2);

    [Test]
    public void TableTest()
    {
        int power = b2BoundingPowerOf2(3008);
        Assert.That(power, Is.EqualTo(12));

        int nextPowerOf2 = b2RoundUpPowerOf2(3008);
        Assert.That(nextPowerOf2, Is.EqualTo((1 << power)));

        const int N = SET_SPAN;
        const uint itemCount = ITEM_COUNT;
        bool[] removed = new bool[ITEM_COUNT];

        for (int iter = 0; iter < 1; ++iter)
        {
            B2HashSet set = b2CreateSet(16);

            // Fill set
            for (int i = 0; i < N; ++i)
            {
                for (int j = i + 1; j < N; ++j)
                {
                    ulong key = B2_SHAPE_PAIR_KEY(i, j);
                    b2AddKey(ref set, key);
                }
            }

            Assert.That(set.count, Is.EqualTo(itemCount));

            // Remove a portion of the set
            int k = 0;
            uint removeCount = 0;
            for (int i = 0; i < N; ++i)
            {
                for (int j = i + 1; j < N; ++j)
                {
                    if (j == i + 1)
                    {
                        ulong key = B2_SHAPE_PAIR_KEY(i, j);
                        b2RemoveKey(ref set, key);
                        removed[k++] = true;
                        removeCount += 1;
                    }
                    else
                    {
                        removed[k++] = false;
                    }
                }
            }

            Assert.That(set.count, Is.EqualTo((itemCount - removeCount)));

            // Snoop counters. These should be disabled in optimized builds because they are expensive.
#if B2_SNOOP_TABLE_COUNTERS
            B2AtomicInt b2_probeCount = new B2AtomicInt();
            b2AtomicStoreInt(ref b2_probeCount, 0);
#endif

            // Test key search
            // ~5ns per search on an AMD 7950x
            ulong ticks = b2GetTicks();

            k = 0;
            for (int i = 0; i < N; ++i)
            {
                for (int j = i + 1; j < N; ++j)
                {
                    ulong key = B2_SHAPE_PAIR_KEY(j, i);
                    Assert.That(b2ContainsKey(ref set, key) || removed[k], $"b2ContainsKey(set, {key}) = {b2ContainsKey(ref set, key)} || removed[{k}] = {removed[k]}");
                    k += 1;
                }
            }

            // ulong ticks = b2GetTicks(&timer);
            // Console.Write("set ticks = %llu\n", ticks);

            float ms = b2GetMilliseconds(ticks);
            Console.Write("set: count = %d, b2ContainsKey = %.5f ms, ave = %.5f us\n", itemCount, ms, 1000.0f * ms / itemCount);

#if B2_SNOOP_TABLE_COUNTERS
            int probeCount = b2AtomicLoadInt(ref b2_probeCount);
            float aveProbeCount = (float)probeCount / (float)itemCount;
            Console.Write("item count = %d, probe count = %d, ave probe count %.2f\n", itemCount, probeCount, aveProbeCount);
#endif

            // Remove all keys from set
            for (int i = 0; i < N; ++i)
            {
                for (int j = i + 1; j < N; ++j)
                {
                    ulong key = B2_SHAPE_PAIR_KEY(i, j);
                    b2RemoveKey(ref set, key);
                }
            }

            Assert.That(set.count, Is.EqualTo(0));

            b2DestroySet(ref set);
        }
    }
}