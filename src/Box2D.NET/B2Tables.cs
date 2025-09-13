// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Box2D.NET.B2CTZs;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Buffers;
#if B2_SNOOP_TABLE_COUNTERS
using static Box2D.NET.B2Atomics;
#endif

namespace Box2D.NET
{
    public static class B2Tables
    {
#if B2_SNOOP_TABLE_COUNTERS
        private static B2AtomicInt b2_findCount;
        private static B2AtomicInt b2_probeCount;
#endif
        //#define B2_SHAPE_PAIR_KEY( K1, K2 ) K1 < K2 ? (ulong)K1 << 32 | (ulong)K2 : (ulong)K2 << 32 | (ulong)K1
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong B2_SHAPE_PAIR_KEY(long K1, long K2)
        {
            return K1 < K2 ? (ulong)K1 << 32 | (ulong)K2 : (ulong)K2 << 32 | (ulong)K1;
        }

        public static B2HashSet b2CreateSet(int capacity)
        {
            B2HashSet set = new B2HashSet();

            // Capacity must be a power of 2
            if (capacity > 16)
            {
                set.capacity = b2RoundUpPowerOf2(capacity);
            }
            else
            {
                set.capacity = 16;
            }

            set.count = 0;
            set.items = b2Alloc<B2SetItem>(capacity);
            //memset(set.items, 0, capacity * sizeof(b2SetItem));
            for (int i = 0; i < capacity; ++i)
            {
                set.items[i].Clear();
            }

            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2DestroySet(ref B2HashSet set)
        {
            b2Free(set.items, set.capacity);
            set.items = null;
            set.count = 0;
            set.capacity = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2ClearSet(ref B2HashSet set)
        {
            set.count = 0;
            //memset(set.items, 0, set.capacity);
            for (int i = 0; i < set.capacity; ++i)
            {
                set.items[i].Clear();
            }
        }

        // I need a good hash because the keys are built from pairs of increasing integers.
        // A simple hash like hash = (integer1 XOR integer2) has many collisions.
        // https://lemire.me/blog/2018/08/15/fast-strongly-universal-64-bit-hashing-everywhere/
        // https://preshing.com/20130107/this-hash-set-is-faster-than-a-judy-array/
        // todo try: https://www.jandrewrogers.com/2019/02/12/fast-perfect-hashing/
        // todo try:
        // https://probablydance.com/2018/06/16/fibonacci-hashing-the-optimization-that-the-world-forgot-or-a-better-alternative-to-integer-modulo/

        // I compared with CC on https://jacksonallan.github.io/c_cpp_hash_tables_benchmark/ and got slightly better performance
        // in the washer benchmark.
        // I compared with verstable across 8 benchmarks and the performance was similar.

#if FALSE
        // Fast-hash
        // https://jonkagstrom.com/bit-mixer-construction
        // https://code.google.com/archive/p/fast-hash
        public static uint64_t b2KeyHash( uint64_t key )
        {
            key ^= key >> 23;
            key *= 0x2127599BF4325C37ULL;
            key ^= key >> 47;
            return key;
        }
#elif TRUE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong b2KeyHash(ulong key)
        {
            // Murmur hash
            ulong h = key;
            h ^= h >> 33;
            h *= 0xff51afd7ed558ccduL;
            h ^= h >> 33;
            h *= 0xc4ceb9fe1a85ec53uL;
            h ^= h >> 33;
            return h;
        }
#endif
        public static int b2FindSlot(ref B2HashSet set, ulong key, ulong hash)
        {
#if B2_SNOOP_TABLE_COUNTERS
            b2AtomicFetchAddInt(ref b2_findCount, 1);
#endif

            uint capacity = (uint)set.capacity;
            uint index = (uint)hash & (capacity - 1);
            B2SetItem[] items = set.items;
            while (items[index].key != 0 && items[index].key != key)
            {
#if B2_SNOOP_TABLE_COUNTERS
                b2AtomicFetchAddInt(ref b2_probeCount, 1);
#endif
                index = (index + 1) & (capacity - 1);
            }

            return (int)index;
        }

        public static void b2AddKeyHaveCapacity(ref B2HashSet set, ulong key, ulong hash)
        {
            int index = b2FindSlot(ref set, key, hash);
            B2SetItem[] items = set.items;

            B2_ASSERT(items[index].key == 0);
            items[index].key = key;
            set.count += 1;
        }

        public static void b2GrowTable(ref B2HashSet set)
        {
            uint oldCount = set.count;
            B2_UNUSED(oldCount);

            int oldCapacity = set.capacity;
            B2SetItem[] oldItems = set.items;

            set.count = 0;
            // Capacity must be a power of 2
            set.capacity = 2 * oldCapacity;
            set.items = b2Alloc<B2SetItem>(set.capacity);
            //memset(set.items, 0, set.capacity * sizeof(b2SetItem));
            for (int i = 0; i < set.capacity; ++i)
            {
                set.items[i].Clear();
            }

            // Transfer items into new array
            for (uint i = 0; i < oldCapacity; ++i)
            {
                B2SetItem item = oldItems[i];
                if (item.key == 0)
                {
                    // this item was empty
                    continue;
                }

                ulong hash = b2KeyHash(item.key);
                b2AddKeyHaveCapacity(ref set, item.key, hash);
            }

            B2_ASSERT(set.count == oldCount);

            b2Free(oldItems, oldCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2ContainsKey(ref B2HashSet set, ulong key)
        {
            // key of zero is a sentinel
            B2_ASSERT(key != 0);
            ulong hash = b2KeyHash(key);
            int index = b2FindSlot(ref set, key, hash);
            return set.items[index].key == key;
        }

        // Returns true if key was already in set
        public static bool b2AddKey(ref B2HashSet set, ulong key)
        {
            // key of zero is a sentinel
            B2_ASSERT(key != 0);

            ulong hash = b2KeyHash(key);
            B2_ASSERT(hash != 0);

            int index = b2FindSlot(ref set, key, hash);
            if (set.items[index].key != 0)
            {
                // Already in set
                B2_ASSERT(set.items[index].key == key);
                return true;
            }

            if (2 * set.count >= set.capacity)
            {
                b2GrowTable(ref set);
            }

            b2AddKeyHaveCapacity(ref set, key, hash);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2GetHashSetBytes(ref B2HashSet set)
        {
            return set.capacity * Marshal.SizeOf<B2SetItem>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2GetSetCount(ref B2HashSet set)
        {
            return (int)set.count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2GetSetCapacity(ref B2HashSet set)
        {
            return set.capacity;
        }

        // Returns true if the key was found
        // See https://en.wikipedia.org/wiki/Open_addressing
        public static bool b2RemoveKey(ref B2HashSet set, ulong key)
        {
            ulong hash = b2KeyHash(key);
            int i = b2FindSlot(ref set, key, hash);
            B2SetItem[] items = set.items;
            if (items[i].key == 0)
            {
                // Not in set
                return false;
            }

            // Mark item i as unoccupied
            items[i].key = 0;

            B2_ASSERT(set.count > 0);
            set.count -= 1;

            // Attempt to fill item i
            int j = i;
            int capacity = set.capacity;
            for (;;)
            {
                j = (j + 1) & (capacity - 1);
                if (items[j].key == 0)
                {
                    break;
                }

                // k is the first item for the hash of j
                ulong hash_j = b2KeyHash( items[j].key );
                int k = (int)(hash_j & (uint)(capacity - 1));

                // determine if k lies cyclically in (i,j]
                // i <= j: | i..k..j |
                // i > j: |.k..j  i....| or |....j     i..k.|
                if (i <= j)
                {
                    if (i < k && k <= j)
                    {
                        continue;
                    }
                }
                else
                {
                    if (i < k || k <= j)
                    {
                        continue;
                    }
                }

                // Move j into i
                items[i].key = items[j].key;

                // Mark item j as unoccupied
                items[j].key = 0;

                i = j;
            }

            return true;
        }
    }
}