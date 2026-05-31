// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Buffers;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET
{
    public static class B2StackArrays
    {
        // Used to define a stack array instance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2StackArray<T> b2StackArray_Create<T>(int stackCapacity) where T : new()
        {
            B2_ASSERT(stackCapacity > 0);

            var a = new B2StackArray<T>();
            a.stackData = new T[stackCapacity];
            InitializeReferenceElements(a.stackData, 0, stackCapacity);
            a.data = a.stackData;
            a.count = 0;
            a.capacity = stackCapacity;
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2StackArray_Destroy<T>(ref B2StackArray<T> a)
        {
            if (a.data == null)
            {
                return;
            }

            if (a.data != null && ReferenceEquals(a.data, a.stackData) == false)
            {
                b2Free(a.data, a.capacity);
            }

            a.stackData = null;
            a.data = null;
            a.count = 0;
            a.capacity = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2StackArray_Reserve<T>(ref B2StackArray<T> a, int newCapacity) where T : new()
        {
            B2_ASSERT(a.data != null && a.capacity > 0);
            if (a.capacity >= newCapacity)
            {
                return;
            }

            int oldCapacity = a.capacity;
            if (ReferenceEquals(a.data, a.stackData))
            {
                T[] newData = b2GrowAlloc<T>(null, 0, newCapacity);
                Array.Copy(a.stackData, newData, oldCapacity);
                a.data = newData;
            }
            else
            {
                a.data = b2GrowAlloc(a.data, oldCapacity, newCapacity);
            }

            a.capacity = newCapacity;
        }

        // Push a new element by value
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2StackArray_Push<T>(ref B2StackArray<T> a, T value) where T : new()
        {
            B2_ASSERT(a.data != null && a.capacity > 0);
            if (a.count >= a.capacity)
            {
                b2StackArray_Reserve(ref a, 2 * a.capacity);
            }

            a.data[a.count] = value;
            a.count += 1;
        }

        // Get a pointer to an element
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T b2StackArray_Get<T>(ref B2StackArray<T> a, int index)
        {
            B2_ASSERT(0 <= index && index < a.count);
            return ref a.data[index];
        }

        // Remove an element from an int arrayA by swapping with the last element. This updates the index contained
        // in the moved element in arrayB. Assumes the integers in arrayA index into arrayB. Assumes
        // the elements of arrayB have an indexName member that is the index in arrayA.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void b2RemoveUpdate<T>(ref B2StackArray<int> arrayA, ref B2Array<T> arrayB, int indexB, Func<T, int> getIndexName, Action<T, int> setIndexName)
        {
            int lastIndex = arrayA.count - 1;
            B2_ASSERT(0 <= indexB && indexB < arrayB.count);
            int indexA = getIndexName.Invoke(arrayB.data[indexB]);
            B2_ASSERT(0 <= indexA && indexA < arrayA.count);
            if (indexA != lastIndex)
            {
                int movedIndex = arrayA.data[lastIndex];
                arrayA.data[indexA] = movedIndex;
                setIndexName.Invoke(arrayB.data[movedIndex], indexA);
            }

            arrayA.count -= 1;
        }

        private static void InitializeReferenceElements<T>(T[] data, int startIndex, int count) where T : new()
        {
            if (typeof(T).IsValueType)
            {
                return;
            }

            for (int i = startIndex; i < count; ++i)
            {
                data[i] = new T();
            }
        }
    }
}