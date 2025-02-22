using System;
using System.Runtime.CompilerServices;

namespace Box2D.NET.Benchmark.Fixtures;

public ref struct RefFixedArray2<T> where T : unmanaged
{
    public const int Length = 2;
    public ref T Value0;
    public ref T Value1;

    public RefFixedArray2(ref T v0, ref T v1)
    {
        Value0 = ref v0;
        Value1 = ref v1;
    }

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            switch (index)
            {
                case 0: return ref Value0;
                case 1: return ref Value1;
            }

            throw new IndexOutOfRangeException();
        }
    }
}