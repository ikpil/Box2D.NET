// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Box2D.NET.Benchmark.Fixtures;
using Box2D.NET.Core;

namespace Box2D.NET.Benchmark.Box2D.NET.Core.Benchmark;

[MemoryDiagnoser]
public class ArrayBenchmarks
{
    private const int Count = 100000000;
    private readonly Consumer _consumer = new Consumer();

    [Benchmark]
    public void Benchmark_HeapArray()
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
    public void Benchmark_StackallocArray()
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
    public void Benchmark_StackallocArray1024()
    {
        Span<int> array = stackalloc int[1024];
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
    public void Benchmark_RefFixedArray()
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
    public void Benchmark_UnsafeArray()
    {
        var array = new B2FixedArray2<int>();
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
    public void Benchmark_UnsafeArray1024()
    {
        var array = new B2FixedArray1024<int>();
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
    public void Benchmark_StructBasedFixedArray()
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
    public void Benchmark_ClassBasedFixedArray()
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