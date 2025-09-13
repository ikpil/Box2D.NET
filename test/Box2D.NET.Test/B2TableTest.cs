// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using static Box2D.NET.B2Tables;
using static Box2D.NET.B2Timers;
#if B2_SNOOP_TABLE_COUNTERS
using static Box2D.NET.B2Atomics;
#endif

namespace Box2D.NET.Test;

public class B2TableTest
{
    private const int SET_SPAN = 317;
    private const int ITEM_COUNT = ((SET_SPAN * SET_SPAN - SET_SPAN) / 2);
    private const int TEST_SIZE = 1000;

    [Test]
    public void BasicHashSetTest()
    {
        // Test basic creation and destruction
        B2HashSet set = b2CreateSet(16);
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(0));
        Assert.That(b2GetSetCapacity(ref set), Is.EqualTo(16));

        b2DestroySet(ref set);
        Assert.That(set.items, Is.Null);
        Assert.That(set.count, Is.Zero);
        Assert.That(set.capacity, Is.Zero);
    }

    [Test]
    public void HashSetCapacityTest()
    {
        // Test capacity adjustments - capacity should be power of 2
        {
            B2HashSet set = b2CreateSet(1);
            Assert.That(b2GetSetCapacity(ref set), Is.EqualTo(16)); // Minimum capacity
            b2DestroySet(ref set);
        }

        {
            B2HashSet set = b2CreateSet(15);
            Assert.That(b2GetSetCapacity(ref set), Is.EqualTo(16)); // Should round up to 16
            b2DestroySet(ref set);
        }

        {
            B2HashSet set = b2CreateSet(32);
            Assert.That(b2GetSetCapacity(ref set), Is.EqualTo(32)); // Should stay at 32
            b2DestroySet(ref set);
        }

        {
            B2HashSet set = b2CreateSet(33);
            Assert.That(b2GetSetCapacity(ref set), Is.EqualTo(64)); // Should round up to 64
            b2DestroySet(ref set);
        }
    }

    [Test]
    public void HashSetAddRemoveTest()
    {
        B2HashSet set = b2CreateSet(16);

        // Test adding new keys
        bool found = b2AddKey(ref set, 42);
        Assert.That(found, Is.False); // Should be new
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(1));

        found = b2AddKey(ref set, 123);
        Assert.That(found, Is.False); // Should be new
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(2));

        // Test adding duplicate key
        found = b2AddKey(ref set, 42);
        Assert.That(found, Is.True); // Should already exist
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(2)); // Count shouldn't change

        // Test contains
        Assert.That(b2ContainsKey(ref set, 42), Is.True);
        Assert.That(b2ContainsKey(ref set, 123), Is.True);
        Assert.That(b2ContainsKey(ref set, 999), Is.False);

        // Test removal
        bool removed = b2RemoveKey(ref set, 42);
        Assert.That(removed, Is.True);
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(1));
        Assert.That(b2ContainsKey(ref set, 42), Is.False);
        Assert.That(b2ContainsKey(ref set, 123), Is.True);

        // Test removing non-existent key
        removed = b2RemoveKey(ref set, 999);
        Assert.That(removed, Is.False);
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(1));

        // Test removing same key twice
        removed = b2RemoveKey(ref set, 42);
        Assert.That(removed, Is.False);
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(1));

        b2DestroySet(ref set);
    }

    [Test]
    public void HashSetClearTest()
    {
        B2HashSet set = b2CreateSet(16);

        // Add some keys
        b2AddKey(ref set, 10);
        b2AddKey(ref set, 20);
        b2AddKey(ref set, 30);
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(3));

        // Clear the set
        b2ClearSet(ref set);
        Assert.That(b2GetSetCount(ref set), Is.Zero);
        Assert.That(b2ContainsKey(ref set, 10), Is.False);
        Assert.That(b2ContainsKey(ref set, 20), Is.False);
        Assert.That(b2ContainsKey(ref set, 30), Is.False);

        // Test that we can add keys after clearing
        b2AddKey(ref set, 40);
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(1));
        Assert.That(b2ContainsKey(ref set, 40), Is.True);

        b2DestroySet(ref set);
    }

    [Test]
    public void HashSetGrowthTest()
    {
        B2HashSet set = b2CreateSet(16);
        int initialCapacity = b2GetSetCapacity(ref set);

        // Add enough keys to trigger growth (load factor is 0.5)
        // With capacity 16, growth should happen when count reaches 8
        for (ulong i = 0; i < 8; ++i)
        {
            b2AddKey(ref set, i + 1);
        }

        // Should have grown
        int newCapacity = b2GetSetCapacity(ref set);
        Assert.That(newCapacity >= initialCapacity);
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(8));

        // Verify all keys are still present after growth
        for (ulong i = 1; i <= 8; ++i)
        {
            Assert.That(b2ContainsKey(ref set, i), Is.True);
        }

        b2DestroySet(ref set);
    }

    [Test]
    public void HashSetEdgeCasesTest()
    {
        B2HashSet set = b2CreateSet(16);

        // Test large key values
        ulong largeKey = 0xFFFFFFFFFFFFFFFFUL - 1; // Max value minus 1 (since 0 is sentinel)
        b2AddKey(ref set, largeKey);
        Assert.That(b2ContainsKey(ref set, largeKey), Is.True);
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(1));

        // Test keys that might cause hash collisions
        ulong key1 = 0x123456789ABCDEFUL;
        ulong key2 = 0x987654321FEDCBAUL;
        b2AddKey(ref set, key1);
        b2AddKey(ref set, key2);
        Assert.That(b2ContainsKey(ref set, key1), Is.True);
        Assert.That(b2ContainsKey(ref set, key2), Is.True);

        // Test pattern that could cause clustering
        for (ulong i = 0x1000; i < 0x1010; ++i)
        {
            b2AddKey(ref set, i);
        }

        for (ulong i = 0x1000; i < 0x1010; ++i)
        {
            Assert.That(b2ContainsKey(ref set, i), Is.True);
        }

        b2DestroySet(ref set);
    }

    [Test]
    public void HashSetRemovalReorganizationTest()
    {
        B2HashSet set = b2CreateSet(16);

        // Add keys that might cluster together
        ulong[] keys = { 100, 116, 132, 148, 164 }; // These might hash to similar slots
        int keyCount = keys.Length;

        for (int i = 0; i < keyCount; ++i)
        {
            b2AddKey(ref set, keys[i]);
        }

        // Verify all keys are present
        for (int i = 0; i < keyCount; ++i)
        {
            Assert.That(b2ContainsKey(ref set, keys[i]), Is.True);
        }

        // Remove a key from the middle
        b2RemoveKey(ref set, keys[2]);
        Assert.That(b2ContainsKey(ref set, keys[2]), Is.False);

        // Verify other keys are still present (tests reorganization)
        for (int i = 0; i < keyCount; ++i)
        {
            if (i != 2)
            {
                Assert.That(b2ContainsKey(ref set, keys[i]), Is.True);
            }
        }

        b2DestroySet(ref set);
    }


    [Test]
    public void HashSetStressTest()
    {
        B2HashSet set = b2CreateSet(32);

        const int testSize = TEST_SIZE;
        ulong[] keys = new ulong[TEST_SIZE];

        // Generate test keys
        for (int i = 0; i < testSize; ++i)
        {
            keys[i] = (ulong)(i * 7 + 13); // Some pattern to avoid zero
        }

        // Add all keys
        for (int i = 0; i < testSize; ++i)
        {
            bool found = b2AddKey(ref set, keys[i]);
            Assert.That(found, Is.False);
        }

        Assert.That(b2GetSetCount(ref set), Is.EqualTo(testSize));

        // Verify all keys are present
        for (int i = 0; i < testSize; ++i)
        {
            Assert.That(b2ContainsKey(ref set, keys[i]), Is.True);
        }

        // Remove every other key
        int removedCount = 0;
        for (int i = 0; i < testSize; i += 2)
        {
            bool removed = b2RemoveKey(ref set, keys[i]);
            Assert.That(removed, Is.True);
            removedCount++;
        }

        Assert.That(b2GetSetCount(ref set), Is.EqualTo(testSize - removedCount));

        // Verify remaining keys are still present
        for (int i = 0; i < testSize; ++i)
        {
            bool shouldBePresent = (i % 2 == 1);
            Assert.That(b2ContainsKey(ref set, keys[i]), Is.EqualTo(shouldBePresent));
        }

        b2DestroySet(ref set);
    }

    [Test]
    public void HashSetShapePairKeyTest()
    {
        B2HashSet set = b2CreateSet(16);

        // Test the B2_SHAPE_PAIR_KEY macro
        ulong key1 = B2_SHAPE_PAIR_KEY(5, 10);
        ulong key2 = B2_SHAPE_PAIR_KEY(10, 5); // Should be same as key1
        Assert.That(key1, Is.EqualTo(key2));

        b2AddKey(ref set, key1);
        Assert.That(b2ContainsKey(ref set, key1), Is.True);
        Assert.That(b2ContainsKey(ref set, key2), Is.True); // Should find same key

        // Test different pairs
        ulong key3 = B2_SHAPE_PAIR_KEY(1, 2);
        ulong key4 = B2_SHAPE_PAIR_KEY(2, 3);
        Assert.That(key3 != key4);

        b2AddKey(ref set, key3);
        b2AddKey(ref set, key4);
        Assert.That(b2GetSetCount(ref set), Is.EqualTo(3));

        b2DestroySet(ref set);
    }

    [Test]
    public void HashSetBytesTest()
    {
        B2HashSet set = b2CreateSet(32);

        int bytes = b2GetHashSetBytes(ref set);
        int expectedBytes = 32 * Marshal.SizeOf<B2SetItem>();
        Assert.That(bytes, Is.EqualTo(expectedBytes));

        // Add some items and verify bytes calculation doesn't change
        b2AddKey(ref set, 100);
        b2AddKey(ref set, 200);

        int bytesAfterAdd = b2GetHashSetBytes(ref set);
        Assert.That(bytesAfterAdd, Is.EqualTo(expectedBytes));

        b2DestroySet(ref set);
    }

    [Test]
    public void HashSetTest()
    {
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
                    bool found = b2AddKey(ref set, key);
                    Assert.That(found, Is.False);
                    ;
                    b2AddKey(ref set, key);
                }
            }

            Assert.That(b2GetSetCount(ref set), Is.EqualTo(itemCount));

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
                        int size1 = b2GetSetCount(ref set);
                        bool found = b2RemoveKey(ref set, key);
                        Assert.That(found, Is.True);
                        int size2 = b2GetSetCount(ref set);
                        Assert.That(size2, Is.EqualTo(size1 - 1));
                        removed[k++] = true;
                        removeCount += 1;
                    }
                    else
                    {
                        removed[k++] = false;
                    }
                }
            }

            Assert.That(b2GetSetCount(ref set), Is.EqualTo((itemCount - removeCount)));

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
                    bool found = b2ContainsKey(ref set, key);
                    Assert.That(found || removed[k], Is.True, $"b2ContainsKey(set, {key}) = {b2ContainsKey(ref set, key)} || removed[{k}] = {removed[k]}");
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

            Assert.That(b2GetSetCount(ref set), Is.EqualTo(0));

            b2DestroySet(ref set);
        }
    }
}