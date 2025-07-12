using System;
using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace Box2D.NET.Benchmark.Box2D.NET.Core.Benchmark;

/*

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4652/24H2/2024Update/HudsonValley)
AMD Ryzen 7 5800X 3.80GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2


| Method              | Mean      | Error     | StdDev    | Allocated |
|-------------------- |----------:|----------:|----------:|----------:|
| CTZ32               |  4.404 us | 0.0198 us | 0.0166 us |         - |
| CTZ32_DeBrujin      |  6.896 us | 0.1373 us | 0.1925 us |         - |
| CTZ32_BitOperations |  6.544 us | 0.0325 us | 0.0272 us |         - |
| CLZ32               | 13.590 us | 0.2677 us | 0.2749 us |         - |
| CLZ32_BitOperations |  4.594 us | 0.0914 us | 0.1250 us |         - |
| CLZ32_Old           | 13.247 us | 0.2080 us | 0.1844 us |         - |
| CTZ64_DeBrujin      |  8.215 us | 0.1581 us | 0.2267 us |         - |
| CTZ64_BitOperations |  6.577 us | 0.0437 us | 0.0408 us |         - |
| CTZ64               |  4.474 us | 0.0727 us | 0.0680 us |         - |

 */

[MemoryDiagnoser]
public class BitOpBenchmarks
{
    private const int Size = 10_000;
    private static uint[] _randomU32;
    private static ulong[] _randomU64;

    [GlobalSetup]
    public void Setup()
    {
        if (null == _randomU32)
        {
            // 고정 시드로 반복 가능한 테스트 
            var rnd = new Random((int)(DateTime.Now.Ticks / TimeSpan.TicksPerSecond));
            _randomU32 = new uint[Size];
            _randomU64 = new ulong[Size];

            for (int i = 0; i < Size; i++)
            {
                _randomU32[i] = (uint)rnd.Next() | 1; // 0 피해서 최소한 1
                _randomU64[i] = ((ulong)(uint)rnd.Next() << 32) | (uint)rnd.Next() | 1UL;
            }
        }
    }


    [Benchmark]
    public uint CTZ32()
    {
        uint sum = 0;
        for (int i = 0; i < Size; i++)
            sum += B2CTZs.b2CTZ32(_randomU32[i]);
        return sum;
    }

    [Benchmark]
    public uint CTZ32_DeBrujin()
    {
        uint sum = 0;
        for (int i = 0; i < Size; i++)
            sum += B2CTZs.b2CTZ32_DeRrujin(_randomU32[i]);
        return sum;
    }

    [Benchmark]
    public int CTZ32_BitOperations()
    {
        int sum = 0;
        for (int i = 0; i < Size; i++)
            sum += BitOperations.TrailingZeroCount(_randomU32[i]);
        return sum;
    }

    [Benchmark]
    public uint CLZ32()
    {
        uint sum = 0;
        for (int i = 0; i < Size; i++)
            sum += B2CTZs.b2CLZ32(_randomU32[i]);
        return sum;
    }

    [Benchmark]
    public int CLZ32_BitOperations()
    {
        int sum = 0;
        for (int i = 0; i < Size; i++)
            sum += BitOperations.LeadingZeroCount(_randomU32[i]);
        return sum;
    }


    [Benchmark]
    public uint CLZ32_Old()
    {
        uint sum = 0;
        for (int i = 0; i < Size; i++)
            sum += B2CTZs.b2CLZ32_Old(_randomU32[i]);
        return sum;
    }

    [Benchmark]
    public uint CTZ64_DeBrujin()
    {
        uint sum = 0;
        for (int i = 0; i < Size; i++)
            sum += B2CTZs.b2CTZ64_DeBrujin(_randomU64[i]);
        return sum;
    }

    [Benchmark]
    public int CTZ64_BitOperations()
    {
        int sum = 0;
        for (int i = 0; i < Size; i++)
            sum += BitOperations.TrailingZeroCount(_randomU64[i]);
        return sum;
    }

    [Benchmark]
    public uint CTZ64()
    {
        uint sum = 0;
        for (int i = 0; i < Size; i++)
            sum += B2CTZs.b2CTZ64(_randomU64[i]);
        return sum;
    }
}