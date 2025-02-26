// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using NUnit.Framework;
using static Box2D.NET.B2CTZs;

namespace Box2D.NET.Test;

public class B2CTZTest
{
    // uint에 대해 trailing zero count (CTZ)
    public static uint CTZ32(uint block)
    {
        return (uint)BitOperations.TrailingZeroCount(block);
    }

    // uint에 대해 leading zero count (CLZ)
    public static uint CLZ32(uint value)
    {
        return (uint)BitOperations.LeadingZeroCount(value);
    }

    // ulong에 대해 trailing zero count (CTZ)
    public static uint CTZ64(ulong block)
    {
        return (uint)BitOperations.TrailingZeroCount(block);
    }

    [Test]
    public void Test()
    {
        // 추가적인 엣지 케이스 테스트
        uint[] testCases32 = { 0, 1, 2, 4, 8, 16, 31, 32, 64, 128, 255, 256, 1023, 1024, 4294967295, uint.MaxValue };
        ulong[] testCases64 = { 0, 1, 2, 4, 8, 16, 31, 32, 64, 128, 255, 256, 1023, 1024, 18446744073709551615, ulong.MaxValue };

        foreach (uint value in testCases32)
        {
            Assert.That(b2CTZ32(value), Is.EqualTo(CTZ32(value)), $"CTZ32 failed for {value}");
            Assert.That(b2CLZ32(value), Is.EqualTo(CLZ32(value)), $"CLZ32 failed for {value}");
        }

        foreach (ulong value in testCases64)
        {
            Assert.That(b2CTZ64(value), Is.EqualTo(CTZ64(value)), $"CTZ64 failed for {value}");
        }
    }
}
