// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using NUnit.Framework;
using System;

namespace Box2D.NET.Test;

/// <summary>
/// Tests for B2FixedArray series which provides fixed-size array structures.
/// 
/// Key concepts:
/// 1. Fixed-size arrays with compile-time size checks 
/// 2. Span<T> based memory access for efficient operations
/// 3. Value-type semantics with independent instances
/// 4. Support for unmanaged types only
/// 5. SIMD-friendly memory layout for performance
/// </summary>
public class B2FixedArrayTest
{
    public static void Fill(Span<int> source)
    {
        for (int i = 0; i < source.Length; ++i)
        {
            source[i] = i;
        }
    }

    public static bool IsSame(Func<int, int> fetch, Span<int> source)
    {
        for (int i = 0; i < source.Length; ++i)
        {
            if (fetch(i) != source[i])
            {
                return false;
            }
        }

        return true;
    }

    [Test]
    public void Test_B2FixedArray_Size_And_Length()
    {
        var array1Int = new B2FixedArray1<int>();
        var array2Int = new B2FixedArray2<int>();
        var array3Int = new B2FixedArray3<int>();
        var array4Int = new B2FixedArray4<int>();
        var array7Int = new B2FixedArray7<int>();
        var array8Int = new B2FixedArray8<int>();
        var array11Int = new B2FixedArray11<int>();
        var array12Int = new B2FixedArray12<int>();
        var array16Int = new B2FixedArray16<int>();
        var array64Int = new B2FixedArray64<int>();
        var array1024Int = new B2FixedArray1024<int>();

        // Test array sizes
        Assert.That(B2FixedArray1<int>.Size, Is.EqualTo(1), "B2FixedArray1 should have size 1");
        Assert.That(B2FixedArray2<int>.Size, Is.EqualTo(2), "B2FixedArray2 should have size 2");
        Assert.That(B2FixedArray3<int>.Size, Is.EqualTo(3), "B2FixedArray3 should have size 3");
        Assert.That(B2FixedArray4<int>.Size, Is.EqualTo(4), "B2FixedArray4 should have size 4");
        Assert.That(B2FixedArray7<int>.Size, Is.EqualTo(7), "B2FixedArray7 should have size 7");
        Assert.That(B2FixedArray8<int>.Size, Is.EqualTo(8), "B2FixedArray8 should have size 8");
        Assert.That(B2FixedArray11<int>.Size, Is.EqualTo(11), "B2FixedArray11 should have size 11");
        Assert.That(B2FixedArray12<int>.Size, Is.EqualTo(12), "B2FixedArray12 should have size 12");
        Assert.That(B2FixedArray16<int>.Size, Is.EqualTo(16), "B2FixedArray16 should have size 16");
        Assert.That(B2FixedArray64<int>.Size, Is.EqualTo(64), "B2FixedArray64 should have size 64");
        Assert.That(B2FixedArray1024<int>.Size, Is.EqualTo(1024), "B2FixedArray1024 should have size 1024");

        // Test array lengths
        Assert.That(array1Int.Length, Is.EqualTo(1), "B2FixedArray1 should have length 1");
        Assert.That(array2Int.Length, Is.EqualTo(2), "B2FixedArray2 should have length 2");
        Assert.That(array3Int.Length, Is.EqualTo(3), "B2FixedArray3 should have length 3");
        Assert.That(array4Int.Length, Is.EqualTo(4), "B2FixedArray4 should have length 4");
        Assert.That(array7Int.Length, Is.EqualTo(7), "B2FixedArray7 should have length 7");
        Assert.That(array8Int.Length, Is.EqualTo(8), "B2FixedArray8 should have length 8");
        Assert.That(array11Int.Length, Is.EqualTo(11), "B2FixedArray11 should have length 11");
        Assert.That(array12Int.Length, Is.EqualTo(12), "B2FixedArray12 should have length 12");
        Assert.That(array16Int.Length, Is.EqualTo(16), "B2FixedArray16 should have length 16");
        Assert.That(array64Int.Length, Is.EqualTo(64), "B2FixedArray64 should have length 64");
        Assert.That(array1024Int.Length, Is.EqualTo(1024), "B2FixedArray1024 should have length 1024");

        // Test AsSpan lengths
        Assert.That(array1Int.AsSpan().Length, Is.EqualTo(1), "B2FixedArray1 span should have length 1");
        Assert.That(array2Int.AsSpan().Length, Is.EqualTo(2), "B2FixedArray2 span should have length 2");
        Assert.That(array3Int.AsSpan().Length, Is.EqualTo(3), "B2FixedArray3 span should have length 3");
        Assert.That(array4Int.AsSpan().Length, Is.EqualTo(4), "B2FixedArray4 span should have length 4");
        Assert.That(array7Int.AsSpan().Length, Is.EqualTo(7), "B2FixedArray7 span should have length 7");
        Assert.That(array8Int.AsSpan().Length, Is.EqualTo(8), "B2FixedArray8 span should have length 8");
        Assert.That(array11Int.AsSpan().Length, Is.EqualTo(11), "B2FixedArray11 span should have length 11");
        Assert.That(array12Int.AsSpan().Length, Is.EqualTo(12), "B2FixedArray12 span should have length 12");
        Assert.That(array16Int.AsSpan().Length, Is.EqualTo(16), "B2FixedArray16 span should have length 16");
        Assert.That(array64Int.AsSpan().Length, Is.EqualTo(64), "B2FixedArray64 span should have length 64");
        Assert.That(array1024Int.AsSpan().Length, Is.EqualTo(1024), "B2FixedArray1024 span should have length 1024");
    }


    [Test]
    public void Test_B2FixedArray_AsSpan()
    {
        var array1Int = new B2FixedArray1<int>();
        var array2Int = new B2FixedArray2<int>();
        var array3Int = new B2FixedArray3<int>();
        var array4Int = new B2FixedArray4<int>();
        var array7Int = new B2FixedArray7<int>();
        var array8Int = new B2FixedArray8<int>();
        var array11Int = new B2FixedArray11<int>();
        var array12Int = new B2FixedArray12<int>();
        var array16Int = new B2FixedArray16<int>();
        var array64Int = new B2FixedArray64<int>();
        var array1024Int = new B2FixedArray1024<int>();

        Fill(array1Int.AsSpan());
        Fill(array2Int.AsSpan());
        Fill(array3Int.AsSpan());
        Fill(array4Int.AsSpan());
        Fill(array7Int.AsSpan());
        Fill(array8Int.AsSpan());
        Fill(array11Int.AsSpan());
        Fill(array12Int.AsSpan());
        Fill(array16Int.AsSpan());
        Fill(array64Int.AsSpan());
        Fill(array1024Int.AsSpan());

        Assert.That(IsSame(i => array1Int[i], array1Int.AsSpan()), Is.True, "B2FixedArray1 should store values correctly");
        Assert.That(IsSame(i => array2Int[i], array2Int.AsSpan()), Is.True, "B2FixedArray2 should store values correctly");
        Assert.That(IsSame(i => array3Int[i], array3Int.AsSpan()), Is.True, "B2FixedArray3 should store values correctly");
        Assert.That(IsSame(i => array4Int[i], array4Int.AsSpan()), Is.True, "B2FixedArray4 should store values correctly");
        Assert.That(IsSame(i => array7Int[i], array7Int.AsSpan()), Is.True, "B2FixedArray7 should store values correctly");
        Assert.That(IsSame(i => array8Int[i], array8Int.AsSpan()), Is.True, "B2FixedArray8 should store values correctly");
        Assert.That(IsSame(i => array11Int[i], array11Int.AsSpan()), Is.True, "B2FixedArray11 should store values correctly");
        Assert.That(IsSame(i => array12Int[i], array12Int.AsSpan()), Is.True, "B2FixedArray12 should store values correctly");
        Assert.That(IsSame(i => array16Int[i], array16Int.AsSpan()), Is.True, "B2FixedArray16 should store values correctly");
        Assert.That(IsSame(i => array64Int[i], array64Int.AsSpan()), Is.True, "B2FixedArray64 should store values correctly");
        Assert.That(IsSame(i => array1024Int[i], array1024Int.AsSpan()), Is.True, "B2FixedArray1024 should store values correctly");
    }

} 
