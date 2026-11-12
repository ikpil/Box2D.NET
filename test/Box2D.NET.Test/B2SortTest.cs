// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using NUnit.Framework;
using static Box2D.NET.B2BroadPhases;

namespace Box2D.NET.Test;

public class B2SortTest
{
    private const int MAX_KEYS = 4096;

    private readonly ulong[] s_keys = new ulong[MAX_KEYS];
    private readonly ulong[] s_expected = new ulong[MAX_KEYS];
    private readonly ulong[] s_temp = new ulong[MAX_KEYS];

    private ulong s_state = 0x9E3779B97F4A7C15ul;

    private ulong NextRandom()
    {
        // xorshift64*
        s_state ^= s_state >> 12;
        s_state ^= s_state << 25;
        s_state ^= s_state >> 27;
        return s_state * 0x2545F4914F6CDD1Dul;
    }

    private void SortAndCheck(int count)
    {
        Array.Copy(s_keys, s_expected, count);
        Array.Sort(s_expected, 0, count);

        b2RadixSortKeys(s_keys.AsSpan(0, count), s_temp.AsSpan(0, count), count);

        for (int i = 0; i < count; ++i)
        {
            Assert.That(s_keys[i], Is.EqualTo(s_expected[i]));
        }
    }

    [Test]
    public void TestRandomFullWidth()
    {
        int[] counts = { 2, 3, 17, 255, 256, 257, 1000, MAX_KEYS };
        for (int c = 0; c < counts.Length; ++c)
        {
            for (int i = 0; i < counts[c]; ++i)
            {
                s_keys[i] = NextRandom();
            }

            SortAndCheck(counts[c]);
        }
    }

    [Test]
    public void TestShapePairKeys()
    {
        int count = 2000;
        for (int i = 0; i < count; ++i)
        {
            ulong shapeA = NextRandom() % 300;
            ulong shapeB = NextRandom() % 300;
            s_keys[i] = (shapeA << 32) | shapeB;
        }

        SortAndCheck(count);
    }

    [Test]
    public void TestSingleDigit()
    {
        // One active digit leaves the result in the temp buffer
        int count = 500;
        for (int i = 0; i < count; ++i)
        {
            s_keys[i] = 0xABCD000000000000ul | (NextRandom() & 0xFF);
        }

        SortAndCheck(count);

        for (int i = 0; i < count; ++i)
        {
            s_keys[i] = 0x1234ul | ((NextRandom() & 0xFF) << 56);
        }

        SortAndCheck(count);
    }

    [Test]
    public void TestTwoDigits()
    {
        int count = 500;
        for (int i = 0; i < count; ++i)
        {
            s_keys[i] = (NextRandom() & 0xFF) | ((NextRandom() & 0xFF) << 40);
        }

        SortAndCheck(count);
    }

    [Test]
    public void TestAllEqual()
    {
        int count = 100;
        for (int i = 0; i < count; ++i)
        {
            s_keys[i] = 0x0102030405060708ul;
        }

        SortAndCheck(count);
    }

    [Test]
    public void TestOrdered()
    {
        int count = 1000;
        for (int i = 0; i < count; ++i)
        {
            s_keys[i] = (ulong)i * 0x0101010101ul;
        }

        SortAndCheck(count);

        for (int i = 0; i < count; ++i)
        {
            s_keys[i] = (ulong)(count - i) * 0x0101010101ul;
        }

        SortAndCheck(count);
    }

    [Test]
    public void TestExtremes()
    {
        s_keys[0] = ulong.MaxValue;
        s_keys[1] = 0;
        s_keys[2] = 1ul << 63;
        s_keys[3] = (1ul << 63) - 1;
        s_keys[4] = 0;
        s_keys[5] = ulong.MaxValue;

        SortAndCheck(6);
    }
}
