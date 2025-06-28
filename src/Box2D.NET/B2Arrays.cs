// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Buffers;

namespace Box2D.NET
{
    // Macro generated functions for dynamic arrays
    // Pros
    // - type safe
    // - array data debuggable (visible count and capacity)
    // - bounds checking
    // - forward declaration
    // - simple implementation
    // - generates functions (like C++ templates)
    // - functions have https://en.wikipedia.org/wiki/Sequence_point
    // - avoids stretchy buffer dropped pointer update bugs
    // Cons
    // - cannot debug
    // - breaks code navigation
    
    // The fragmentation problem with factor 2:
    // When you double capacity, the new allocation is larger than the sum of all previous allocations:
    //
    // 1st allocation: 8 bytes
    // 2nd allocation: 16 bytes
    // 3rd allocation: 32 bytes (larger than 8 + 16 = 24)
    //
    // This means the memory freed from previous allocations can never be reused for future expansions
    // of the same array. The allocator must always find fresh memory.

    // todo_erin consider code-gen: https://github.com/IbrahimHindawi/haikal
    public static class B2Arrays
    {
        /* Resize */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2Array_Resize<T>(ref B2Array<T> a, int count) where T : new()
        {
            b2Array_Reserve(ref a, count);
            a.count = count;
        }

        /* Get */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T b2Array_Get<T>(ref B2Array<T> a, int index)
        {
            if (0 > index || index >= a.count)
            {
                throw new IndexOutOfRangeException($"Index is out of range - count({a.count}) index({index})");
            }
            
            return ref a.data[index];
        }

        /* Add */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T b2Array_Add<T>(ref B2Array<T> a) where T : new()
        {
            if (a.count == a.capacity)
            {
                int newCapacity = a.capacity < 2 ? 2 : a.capacity + (a.capacity >> 1);
                b2Array_Reserve(ref a, newCapacity);
            }

            a.count += 1;
            return ref a.data[a.count - 1];
        }

        /* Push */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2Array_Push<T>(ref B2Array<T> a, T value) where T : new()
        {
            if (a.count == a.capacity)
            {
                int newCapacity = a.capacity < 2 ? 2 : a.capacity + (a.capacity >> 1);
                b2Array_Reserve(ref a, newCapacity);
            }

            a.data[a.count] = value;
            a.count += 1;
        }

        /* Set */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2Array_Set<T>(ref B2Array<T> a, int index, T value)
        {
            ref T v = ref b2Array_Get(ref a, index);
            v = value;
        }

        /* RemoveSwap */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2Array_RemoveSwap<T>(ref B2Array<T> a, int index) where T : new()
        {
            if (0 > index || index >= a.count)
            {
                throw new IndexOutOfRangeException($"Index is out of range - count({a.count}) index({index})");
            }
            
            int movedIndex = B2_NULL_INDEX;
            if (index != a.count - 1)
            {
                movedIndex = a.count - 1;
                a.data[index] = a.data[movedIndex];

                // fixed, ikpil
                if (!typeof(T).IsValueType)
                {
                    a.data[movedIndex] = new T();
                }
            }

            a.count -= 1;
            return movedIndex;
        }

        /* Pop */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T b2Array_Pop<T>(ref B2Array<T> a) where T : new()
        {
            if (0 >= a.count)
            {
                throw new IndexOutOfRangeException($"Index is out of range - count({a.count})");
            }

            T value = a.data[a.count - 1];

            // fixed, ikpil
            if (!typeof(T).IsValueType)
            {
                a.data[a.count - 1] = new T();
            }

            a.count -= 1;
            return value;
        }

        /* Clear */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2Array_Clear<T>(ref B2Array<T> a)
        {
            a.count = 0;
        }

        /* ByteCount */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2Array_ByteCount<T>(ref B2Array<T> a)
        {
            if (typeof(T).IsValueType)
            {
                return a.capacity * Marshal.SizeOf<T>();
            }

            return -1;
        }

        // Array implementations to be instantiated in a source file where the type T is known
        /* Create */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Array<T> b2Array_Create<T>(int capacity = 0) where T : new()
        {
            B2Array<T> a = new B2Array<T>();
            if (capacity > 0)
            {
                a.data = new T[capacity];
                if (!typeof(T).IsValueType)
                {
                    for (int i = 0; i < capacity; ++i)
                    {
                        a.data[i] = new T();
                    }
                }

                a.capacity = capacity;
            }

            return a;
        }

        /* Reserve */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2Array_Reserve<T>(ref B2Array<T> a, int newCapacity) where T : new()
        {
            if (newCapacity <= a.capacity)
            {
                return;
            }

            a.data = b2GrowAlloc(a.data, a.capacity, newCapacity);
            a.capacity = newCapacity;
        }

        /* Destroy */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2Array_Destroy<T>(ref B2Array<T> a)
        {
            b2Free(a.data, a.capacity);
            a.data = null;
            a.count = 0;
            a.capacity = 0;
        }
    }
}