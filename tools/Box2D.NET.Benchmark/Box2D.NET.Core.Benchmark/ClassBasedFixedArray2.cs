﻿using System.Runtime.CompilerServices;

namespace Box2D.NET.Benchmark.Box2D.NET.Core.Benchmark;

public class ClassBasedFixedArray2<T> where T : unmanaged
{
    private readonly T[] _array = new T[2];

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _array[index];
    }
}