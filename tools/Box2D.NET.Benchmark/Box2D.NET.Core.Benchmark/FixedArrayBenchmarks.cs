using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Box2D.NET.Core;

namespace Box2D.NET.Benchmark.Box2D.NET.Core.Benchmark;

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

[MemoryDiagnoser]
public class FixedArrayBenchmarks
{
    private const int Count = 100000000;
    private readonly Consumer _consumer = new Consumer();

    [Benchmark]
    public void HeapAllocArray()
    {
        int[] array = new int[2];
        array[0] = 3;
        array[1] = 4;

        for (int i = 0; i < Count; ++i)
        {
            array[0] += 1;
            array[1] += 1;
        }

        if (array[0] != Count + 3)
            throw new InvalidOperationException("");

        if (array[1] != Count + 4)
            throw new InvalidOperationException("");

        _consumer.Consume(array[0]);
        _consumer.Consume(array[1]);
    }

    [Benchmark]
    public void StackallocArray()
    {
        Span<int> array = stackalloc int[2];
        array[0] = 3;
        array[1] = 4;

        for (int i = 0; i < Count; ++i)
        {
            array[0] += 1;
            array[1] += 1;
        }

        if (array[0] != Count + 3)
            throw new InvalidOperationException("");

        if (array[1] != Count + 4)
            throw new InvalidOperationException("");

        _consumer.Consume(array[0]);
        _consumer.Consume(array[1]);
    }

    [Benchmark]
    public void RefFixedArray()
    {
        int a = 3;
        int b = 4;

        for (int i = 0; i < Count; ++i)
        {
            var array = new RefFixedArray2<int>(ref a, ref b);
            array[0] += 1;
            array[1] += 1;
        }

        if (a != Count + 3)
            throw new InvalidOperationException("");

        if (b != Count + 4)
            throw new InvalidOperationException("");

        _consumer.Consume(a);
        _consumer.Consume(b);
    }

    [Benchmark]
    public void UnsafeFixedArray()
    {
        var array = new UnsafeArray2<int>();
        array[0] = 3;
        array[1] = 4;

        for (int i = 0; i < Count; ++i)
        {
            array[0] += 1;
            array[1] += 1;
        }

        if (array[0] != Count + 3)
            throw new InvalidOperationException("");

        if (array[1] != Count + 4)
            throw new InvalidOperationException("");

        _consumer.Consume(array[0]);
        _consumer.Consume(array[1]);
    }

    [Benchmark]
    public void StructBasedFixedArray2()
    {
        var array = new StructBasedFixedArray2<int>();
        array[0] = 3;
        array[1] = 4;

        for (int i = 0; i < Count; ++i)
        {
            array[0] += 1;
            array[1] += 1;
        }

        if (array[0] != Count + 3)
            throw new InvalidOperationException("");

        if (array[1] != Count + 4)
            throw new InvalidOperationException("");

        _consumer.Consume(array[0]);
        _consumer.Consume(array[1]);
    }

    [Benchmark]
    public void FixedArray2()
    {
        var array = new ClassBasedFixedArray2<int>();
        array[0] = 3;
        array[1] = 4;

        for (int i = 0; i < Count; ++i)
        {
            array[0] += 1;
            array[1] += 1;
        }

        if (array[0] != Count + 3)
            throw new InvalidOperationException("");

        if (array[1] != Count + 4)
            throw new InvalidOperationException("");

        _consumer.Consume(array[0]);
        _consumer.Consume(array[1]);
    }
}