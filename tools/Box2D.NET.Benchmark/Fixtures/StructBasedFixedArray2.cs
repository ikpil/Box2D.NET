// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;

namespace Box2D.NET.Benchmark.Fixtures;

public struct StructBasedFixedArray2<T> where T : unmanaged
{
    public T v0;
    public T v1;

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            switch (index)
            {
                case 0: return v0;
                case 1: return v1;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            switch (index)
            {
                case 0: v0 = value; break;
                case 1: v1 = value; break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}