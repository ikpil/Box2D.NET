// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

// * Summary *

// BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.3476)
// AMD Ryzen 5 3600, 1 CPU, 12 logical and 6 physical cores
// .NET SDK 9.0.101
//   [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
//   DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
//
//
// | Method                      | Mean    | Error    | StdDev   | Allocated |
// |---------------------------- |--------:|---------:|---------:|----------:|
// | Benchmark_HeapArray         | 3.431 s | 0.0219 s | 0.0205 s |     680 B |
// | Benchmark_StackallocArray   | 2.562 s | 0.0115 s | 0.0102 s |     400 B |
// | Benchmark_UnsafeArray       | 3.449 s | 0.0155 s | 0.0137 s |     400 B |
// | Benchmark_UnsafeArrayAsSpan | 3.464 s | 0.0201 s | 0.0188 s |     400 B |


using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Box2D.NET.Memory;


namespace Box2D.NET.Benchmark.Box2D.NET.Core.Benchmark;

[MemoryDiagnoser]
public class ArrayBenchmarks
{
    private const int Count = 100000000;
    private readonly Consumer _consumer = new Consumer();

    [Benchmark]
    public void Benchmark_HeapArray()
    {
        int[] array = new int[64];

        for (int i = 0; i < Count; ++i)
        {
            for (int ii = 0; ii < array.Length; ++ii)
            {
                array[ii] += 1;
            }

            _consumer.Consume(array[0]);
            _consumer.Consume(array[1]);
        }
    }

    [Benchmark]
    public void Benchmark_StackallocArray()
    {
        Span<int> array = stackalloc int[64];

        for (int i = 0; i < Count; ++i)
        {
            for (int ii = 0; ii < array.Length; ++ii)
            {
                array[ii] += 1;
            }

            _consumer.Consume(array[0]);
            _consumer.Consume(array[1]);
        }
    }
    //
    // [Benchmark]
    // public void Benchmark_StackallocArray1024()
    // {
    //     Span<int> array = stackalloc int[1024];
    //     array[0] = 3;
    //     array[1] = 4;
    //
    //     for (int i = 0; i < Count; ++i)
    //     {
    //         array[0] += 1;
    //         array[1] += 1;
    //     }
    //
    //     if (array[0] != Count + 3)
    //         throw new InvalidOperationException("");
    //
    //     if (array[1] != Count + 4)
    //         throw new InvalidOperationException("");
    //
    //     _consumer.Consume(array[0]);
    //     _consumer.Consume(array[1]);
    // }
    //
    // [Benchmark]
    // public void Benchmark_RefFixedArray()
    // {
    //     int a = 3;
    //     int b = 4;
    //
    //     for (int i = 0; i < Count; ++i)
    //     {
    //         var array = new RefFixedArray2<int>(ref a, ref b);
    //         array[0] += 1;
    //         array[1] += 1;
    //     }
    //
    //     if (a != Count + 3)
    //         throw new InvalidOperationException("");
    //
    //     if (b != Count + 4)
    //         throw new InvalidOperationException("");
    //
    //     _consumer.Consume(a);
    //     _consumer.Consume(b);
    // }

    [Benchmark]
    public void Benchmark_UnsafeArray()
    {
        var array = new B2FixedArray64<int>();

        for (int i = 0; i < Count; ++i)
        {
            for (int ii = 0; ii < array.Length; ++ii)
            {
                array[ii] += 1;
            }

            _consumer.Consume(array[0]);
            _consumer.Consume(array[1]);
        }
    }

    [Benchmark]
    public void Benchmark_UnsafeArrayAsSpan()
    {
        var array = new B2FixedArray64<int>();

        for (int i = 0; i < Count; ++i)
        {
            for (int ii = 0; ii < array.Length; ++ii)
            {
                array.AsSpan()[ii] += 1;
            }

            _consumer.Consume(array.AsSpan()[0]);
            _consumer.Consume(array.AsSpan()[1]);
        }
    }

    // [Benchmark]
    // public void Benchmark_UnsafeArray1024()
    // {
    //     var array = new B2FixedArray1024<int>();
    //     array[0] = 3;
    //     array[1] = 4;
    //
    //     for (int i = 0; i < Count; ++i)
    //     {
    //         array[0] += 1;
    //         array[1] += 1;
    //     }
    //
    //     if (array[0] != Count + 3)
    //         throw new InvalidOperationException("");
    //
    //     if (array[1] != Count + 4)
    //         throw new InvalidOperationException("");
    //
    //     _consumer.Consume(array[0]);
    //     _consumer.Consume(array[1]);
    // }
    //
    //
    // [Benchmark]
    // public void Benchmark_StructBasedFixedArray()
    // {
    //     var array = new StructBasedFixedArray2<int>();
    //     array[0] = 3;
    //     array[1] = 4;
    //
    //     for (int i = 0; i < Count; ++i)
    //     {
    //         array[0] += 1;
    //         array[1] += 1;
    //     }
    //
    //     if (array[0] != Count + 3)
    //         throw new InvalidOperationException("");
    //
    //     if (array[1] != Count + 4)
    //         throw new InvalidOperationException("");
    //
    //     _consumer.Consume(array[0]);
    //     _consumer.Consume(array[1]);
    // }
    //
    //
    // [Benchmark]
    // public void Benchmark_ClassBasedFixedArray()
    // {
    //     var array = new ClassBasedFixedArray2<int>();
    //     array[0] = 3;
    //     array[1] = 4;
    //
    //     for (int i = 0; i < Count; ++i)
    //     {
    //         array[0] += 1;
    //         array[1] += 1;
    //     }
    //
    //     if (array[0] != Count + 3)
    //         throw new InvalidOperationException("");
    //
    //     if (array[1] != Count + 4)
    //         throw new InvalidOperationException("");
    //
    //     _consumer.Consume(array[0]);
    //     _consumer.Consume(array[1]);
    // }
}