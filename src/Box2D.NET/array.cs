// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT


using System.Diagnostics;
using static Box2D.NET.constants;
using static Box2D.NET.core;

namespace Box2D.NET;

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

// todo_erin consider code-gen: https://github.com/IbrahimHindawi/haikal

// Array declaration that doesn't need the type T to be defined
public class b2Array<T>
{
    public T[] data;
    public int count;
    public int capacity;
}

public static class array
{
    /* Resize */
    public static void b2Array_Resize<T>(b2Array<T> a, int count) where T : new()
    {
        b2Array_Reserve(a, count);
        a.count = count;
    }

    /* Get */
    public static T b2Array_Get<T>(b2Array<T> a, int index)
    {
        Debug.Assert(0 <= index && index < a.count);
        return a.data[index];
    }

    /* Add */
    public static ref T b2Array_Add<T>(b2Array<T> a) where T : new()
    {
        if (a.count == a.capacity)
        {
            int newCapacity = a.capacity < 2 ? 2 : a.capacity + (a.capacity >> 1);
            b2Array_Reserve(a, newCapacity);
        }

        a.count += 1;
        return ref a.data[a.count - 1];
    }

    /* Push */
    public static void b2Array_Push<T>(b2Array<T> a, T value) where T : new()
    {
        if (a.count == a.capacity)
        {
            int newCapacity = a.capacity < 2 ? 2 : a.capacity + (a.capacity >> 1);
            b2Array_Reserve(a, newCapacity);
        }

        a.data[a.count] = value;
        a.count += 1;
    }

    /* Set */
    public static void b2Array_Set<T>(b2Array<T> a, int index, T value)
    {
        Debug.Assert(0 <= index && index < a.count);
        a.data[index] = value;
    }

    /* RemoveSwap */
    public static int b2Array_RemoveSwap<T>(b2Array<T> a, int index)
    {
        Debug.Assert(0 <= index && index < a.count);
        int movedIndex = B2_NULL_INDEX;
        if (index != a.count - 1)
        {
            movedIndex = a.count - 1;
            a.data[index] = a.data[movedIndex];
        }

        a.count -= 1;
        return movedIndex;
    }

    /* Pop */
    public static T b2Array_Pop<T>(b2Array<T> a)
    {
        Debug.Assert(a.count > 0);
        T value = a.data[a.count - 1];
        a.count -= 1;
        return value;
    }

    /* Clear */
    public static void b2Array_Clear<T>(b2Array<T> a)
    {
        a.count = 0;
    }

    /* ByteCount */
    public static int b2Array_ByteCount<T>(b2Array<T> a)
    {
        // TODO: @ikpil, check
        //return (int)( a.capacity * sizeof( T ) );                                                                               
        return -1;
    }

    // Array implementations to be instantiated in a source file where the type T is known
    /* Create */
    public static b2Array<T> b2Array_Create<T>(int capacity) where T : new()
    {
        b2Array<T> a = new b2Array<T>();
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
    public static void b2Array_Reserve<T>(b2Array<T> a, int newCapacity) where T : new()
    {
        if (newCapacity <= a.capacity)
        {
            return;
        }

        a.data = b2GrowAlloc(a.data, a.capacity, newCapacity);
        a.capacity = newCapacity;
    }

    /* Destroy */
    public static void b2Array_Destroy<T>(b2Array<T> a)
    {
        b2Free(a.data, a.capacity);
        a.data = null;
        a.count = 0;
        a.capacity = 0;
    }
}

// public struct b2ArraySegment<T>
// {
//     private readonly b2Array<T> _array;
//
//     public b2ArraySegment(b2Array<T> array)
//     {
//         _array = array;
//     }
//     
//     public T this[int index]
//     {
//         get => _array[index];
//         //set => _array[index] = value;
//     }
//
//     public ReadOnlySpan<T> AsReadOnlySpan()
//     {
//         return _array.AsSpan();
//     }
// }
//
//
// public class b2Array<T>
// {
//     private List<T> _list;
//     
//     public int count => _list.Count;
//     public int capacity => _list.Capacity;
//
//     public b2Array(int capacity)
//     {
//         _list = new(capacity);
//     }
//
//     public void Add(T item)
//     {
//         _list.Add(item);
//     }
//
//     public T this[int index]
//     {
//         get => _list[index];
//         set => _list[index] = value;
//     }
//     
//     public T this[uint index]
//     {
//         get => _list[(int)index];
//         set => _list[(int)index] = value;
//     }
//
//     public void Destroy()
//     {
//         _list.Clear();
//     }
//
//     public void Clear()
//     {
//         _list.Clear();
//     }
//
//     public void EnsureCapacity(int capacity)
//     {
//         _list.EnsureCapacity(capacity);
//     }
//
//     public int RemoveSwap(int index)
//     {
//         int movedIndex = B2_NULL_INDEX;
//         if (index != _list.Count - 1)
//         {
//             movedIndex = _list.Count - 1;
//             _list[index] = _list[movedIndex];
//             _list.RemoveAt(movedIndex);
//         }
//
//         return movedIndex;
//     }
//
//     public b2ArraySegment<T> AsArraySegment()
//     {
//         return new b2ArraySegment<T>(this);
//     }
//     
//     public Span<T> AsSpan()
//     {
//         return CollectionsMarshal.AsSpan(_list);
//     }
// }