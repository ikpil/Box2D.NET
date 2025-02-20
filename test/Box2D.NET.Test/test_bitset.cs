// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using NUnit.Framework;

namespace Box2D.NET.Test;

using static NET.bitset;

public class test_bitset
{
    private const int COUNT = 169;

    [Test]
    public void BitSetTest()
    {
        b2BitSet bitSet = b2CreateBitSet(COUNT);

        b2SetBitCountAndClear(bitSet, COUNT);
        bool[] values = new bool[COUNT];

        int i1 = 0, i2 = 1;
        b2SetBit(bitSet, i1);
        values[i1] = true;

        while (i2 < COUNT)
        {
            b2SetBit(bitSet, i2);
            values[i2] = true;
            int next = i1 + i2;
            i1 = i2;
            i2 = next;
        }

        for (int i = 0; i < COUNT; ++i)
        {
            bool value = b2GetBit(bitSet, i);
            Assert.That(value, Is.EqualTo(values[i]));
        }

        b2DestroyBitSet(bitSet);
    }
}