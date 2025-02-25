// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;

namespace Box2D.NET.Benchmark.Fixtures;

public class ClassBasedFixedArray2<T> where T : unmanaged
{
    private readonly T[] _array = new T[2];

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _array[index];
    }
}