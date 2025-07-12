// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using NUnit.Framework;
using static Box2D.NET.B2CTZs;

namespace Box2D.NET.Test;

public class B2CTZTest
{
    [Test]
    public void Test()
    {
        // 추가적인 엣지 케이스 테스트
        uint[] testCases32 =
        {
            0, 1, 2, 4, 
            8, 16, 31, 32, 
            64, 128, 255, 256, 
            1023, 1024, 4294967295, uint.MaxValue,
            0x0000FFFF, 0xFFFF0000, 0x0F0F0F0F, 0xF0F0F0F0,
            0xAAAAAAAA, 0x55555555, 0x80000001, 0x7FFFFFFF,
            0x0000F000, 0x00FF00FF, 0xC0000000, 0x00000003, 0xFFFFFFFF
        };
        ulong[] testCases64 =
        {
            0, 1, 2, 4, 
            8, 16, 31, 32, 
            64, 128, 255, 256, 
            1023, 1024, 18446744073709551615, ulong.MaxValue,
            0x00000000FFFFFFFFul, 0xFFFFFFFF00000000ul, 0xAAAAAAAAAAAAAAAAul, 0x8000000000000001ul,
            0x7FFFFFFFFFFFFFFFul, 0x000F000000000000ul, 0xF0F0F0F0F0F0F0F0ul, 0x000000000000FFFFul,
            0x00FF00FF00FF00FFul
        };

        foreach (uint value in testCases32)
        {
            Assert.That(b2CTZ32(value), Is.EqualTo(BitOperations.TrailingZeroCount(value)), $"CTZ32 failed for {value}");
            Assert.That(b2CLZ32(value), Is.EqualTo(BitOperations.LeadingZeroCount(value)), $"CLZ32 failed for {value}");
            Assert.That(b2PopCount64(value), Is.EqualTo(BitOperations.PopCount(value)), $"PopCount64 failed for {value}");
        }

        foreach (ulong value in testCases64)
        {
            Assert.That(b2CTZ64(value), Is.EqualTo(BitOperations.TrailingZeroCount(value)), $"CTZ64 failed for {value}");
            Assert.That(b2PopCount64(value), Is.EqualTo(BitOperations.PopCount(value)), $"PopCount64 failed for {value}");
        }
    }
}