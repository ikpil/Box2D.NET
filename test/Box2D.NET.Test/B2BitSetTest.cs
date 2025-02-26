// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using Box2D.NET.Primitives;
using NUnit.Framework;

namespace Box2D.NET.Test;

using static NET.B2BitSets;

public class B2BitSetTest
{
    private const int COUNT = 169;

    [Test]
    public void BitSetTest()
    {
        B2BitSet bitSet = b2CreateBitSet(COUNT);

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
