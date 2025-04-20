// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using NUnit.Framework;
using static Box2D.NET.B2BitSets;

namespace Box2D.NET.Test;

public class B2BitSetTest
{
    private const int COUNT = 169;

    [Test]
    public void BitSetTest()
    {
        B2BitSet bitSet = b2CreateBitSet(COUNT);

        b2SetBitCountAndClear(ref bitSet, COUNT);
        bool[] values = new bool[COUNT];

        int i1 = 0, i2 = 1;
        b2SetBit(ref bitSet, i1);
        values[i1] = true;

        while (i2 < COUNT)
        {
            b2SetBit(ref bitSet, i2);
            values[i2] = true;
            int next = i1 + i2;
            i1 = i2;
            i2 = next;
        }

        for (int i = 0; i < COUNT; ++i)
        {
            bool value = b2GetBit(ref bitSet, i);
            Assert.That(value, Is.EqualTo(values[i]));
        }

        b2DestroyBitSet(ref bitSet);
    }


    [Test]
    public void Test_B2BitSet_b2CreateBitSet()
    {
        // Arrange: Create a BitSet with a capacity of 169 bits
        var bitSet = b2CreateBitSet(COUNT);

        // Assert - Success case: Verify the BitSet capacity and block count
        Assert.That(bitSet.bits.Length, Is.EqualTo(3)); // 169 bits require 3 blocks (169 / 64 = 2.65, round up to 3 blocks)
        Assert.That(bitSet.blockCapacity, Is.EqualTo(3)); // The block capacity should be 3
        Assert.That(bitSet.blockCount, Is.EqualTo(0)); // Initial block count should be 0

        // Additional validation: Check that all blocks are initialized to 0
        Assert.That(bitSet.bits[0], Is.EqualTo(0UL)); // First block should be 0
        Assert.That(bitSet.bits[1], Is.EqualTo(0UL)); // Second block should be 0
        Assert.That(bitSet.bits[2], Is.EqualTo(0UL)); // Third block should be 0

        // Clean-up: Destroy the BitSet after the test
        b2DestroyBitSet(ref bitSet);
    }

    [Test]
    public void Test_B2BitSet_b2DestroyBitSet()
    {
        // Arrange
        var bitSet = b2CreateBitSet(COUNT);

        // Act
        b2DestroyBitSet(ref bitSet);

        // Assert - Success
        Assert.That(bitSet.blockCapacity, Is.EqualTo(0));
        Assert.That(bitSet.blockCount, Is.EqualTo(0));
        Assert.That(bitSet.bits, Is.Null);
    }

    // Test for the b2SetBitCountAndClear function with different scenarios
    [Test]
    public void Test_B2BitSet_b2SetBitCountAndClear()
    {
        // Arrange
        B2BitSet bitSet = b2CreateBitSet(100); // Create a bitset with initial capacity

        // Test 1: Setting a valid bit count
        int newBitCount = 128;
        b2SetBitCountAndClear(ref bitSet, newBitCount); // Call the function to update bit count and clear bits
        Assert.That(bitSet.blockCount, Is.EqualTo(2)); // Check if the block count is correct (2 blocks for 128 bits)
        Assert.That(bitSet.bits[0], Is.EqualTo(0UL)); // Check if the first block is cleared
        Assert.That(bitSet.bits[1], Is.EqualTo(0UL)); // Check if the second block is cleared

        // Test 2: Setting a bit count larger than the current capacity
        int largerBitCount = 200;
        b2SetBitCountAndClear(ref bitSet, largerBitCount); // Call the function to update bit count and clear bits
        Assert.That(bitSet.blockCount, Is.EqualTo(4)); // Check if the block count is correctly updated (4 blocks for 200 bits)
        Assert.That(bitSet.bits[3], Is.EqualTo(0UL)); // Check if the new block is cleared

        // Test 3: Setting a smaller bit count than the current block count
        int smallerBitCount = 50;
        b2SetBitCountAndClear(ref bitSet, smallerBitCount); // Call the function to update bit count and clear bits
        Assert.That(bitSet.blockCount, Is.EqualTo(1)); // Check if the block count is correctly updated (1 block for 50 bits)
        Assert.That(bitSet.bits[0], Is.EqualTo(0UL)); // Check if the first block is cleared

        // Cleanup
        b2DestroyBitSet(ref bitSet);
    }

    [Test]
    public void Test_B2BitSet_b2SetBit()
    {
        // Arrange
        var bitSet = b2CreateBitSet(COUNT);
        b2SetBitCountAndClear(ref bitSet, COUNT);

        // Act
        b2SetBit(ref bitSet, 0);
        b2SetBit(ref bitSet, COUNT / 2 - 1);
        b2SetBit(ref bitSet, COUNT - 1);

        for (int i = 0; i < COUNT; ++i)
        {
            switch (i)
            {
                case 0:
                case COUNT / 2 - 1:
                case COUNT - 1:
                    Assert.That(b2GetBit(ref bitSet, i), Is.True);
                    break;

                default:
                    Assert.That(b2GetBit(ref bitSet, i), Is.False);
                    break;
            }
        }

        b2DestroyBitSet(ref bitSet);
    }

    [Test]
    public void Test_B2BitSet_b2SetBitGrow()
    {
        // Arrange
        var bitSet = b2CreateBitSet(COUNT);

        // Act
        b2SetBitGrow(ref bitSet, 130);

        // Assert - Success
        Assert.That(b2GetBit(ref bitSet, 130), Is.True);

        // Assert - Fail
        Assert.That(b2GetBit(ref bitSet, 129), Is.False);

        b2DestroyBitSet(ref bitSet);
    }

    [Test]
    public void Test_B2BitSet_b2ClearBit()
    {
        // Arrange
        var bitSet = b2CreateBitSet(COUNT);
        b2SetBitCountAndClear(ref bitSet, 128);
        b2SetBit(ref bitSet, 10);

        // Act
        b2ClearBit(ref bitSet, 10);

        // Assert - Success
        Assert.That(b2GetBit(ref bitSet, 10), Is.False);

        // Assert - Fail
        Assert.That(b2GetBit(ref bitSet, 9), Is.False);
        Assert.That(b2GetBit(ref bitSet, 11), Is.False);

        b2DestroyBitSet(ref bitSet);
    }

    [Test]
    public void Test_B2BitSet_b2GetBit()
    {
        // Arrange
        var bitSet = b2CreateBitSet(COUNT);
        b2SetBitCountAndClear(ref bitSet, 128);
        b2SetBit(ref bitSet, 10);

        // Act & Assert - Success
        Assert.That(b2GetBit(ref bitSet, 10), Is.True);

        // Act & Assert - Fail
        Assert.That(b2GetBit(ref bitSet, 9), Is.False);

        b2DestroyBitSet(ref bitSet);
    }

    [Test]
    public void Test_B2BitSet_b2GetBitSetBytes()
    {
        // Arrange
        var bitSet = b2CreateBitSet(COUNT);
        b2SetBitCountAndClear(ref bitSet, 128);

        // Act & Assert - Success
        Assert.That(b2GetBitSetBytes(ref bitSet), Is.EqualTo(24));

        b2DestroyBitSet(ref bitSet);
    }

    [Test]
    public void Test_B2BitSet_b2GrowBitSet()
    {
        // Create a bit set with initial capacity for 64 bits (1 block)
        B2BitSet bitSet = b2CreateBitSet(64);
        Assert.That(bitSet.blockCapacity, Is.GreaterThanOrEqualTo(1));
        Assert.That(bitSet.blockCount, Is.EqualTo(0));

        // Grow to use 2 blocks
        b2GrowBitSet(ref bitSet, 2);

        Assert.That(bitSet.blockCount, Is.EqualTo(2));
        Assert.That(bitSet.blockCapacity, Is.GreaterThanOrEqualTo(2));
        Assert.That(bitSet.bits.Length, Is.EqualTo(bitSet.blockCapacity));

        // Set some bit and make sure it's preserved after grow
        b2SetBit(ref bitSet, 65); // This should be in block index 1
        Assert.That(b2GetBit(ref bitSet, 65), Is.True);

        // Grow again to a larger size (more than current capacity)
        int newBlockCount = bitSet.blockCapacity + 1;
        b2GrowBitSet(ref bitSet, newBlockCount);
        Assert.That(bitSet.blockCount, Is.EqualTo(newBlockCount));
        Assert.That(bitSet.blockCapacity, Is.GreaterThanOrEqualTo(newBlockCount));

        // Check existing bit is still set
        Assert.That(b2GetBit(ref bitSet, 65), Is.True);

        // Trying to grow with a smaller or equal block count should trigger Debug.Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            b2GrowBitSet(ref bitSet, bitSet.blockCount); // equal
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            b2GrowBitSet(ref bitSet, bitSet.blockCount - 1); // smaller
        });

        b2DestroyBitSet(ref bitSet);
    }

    [Test]
    public void Test_B2BitSet_b2InPlaceUnion()
    {
        // Create two bit sets with the same capacity
        var setA = b2CreateBitSet(128); // 2 blocks
        var setB = b2CreateBitSet(128); // 2 blocks

        b2SetBitCountAndClear(ref setA, 128);
        b2SetBitCountAndClear(ref setB, 128);

        // Set different bits
        b2SetBit(ref setA, 1); // setA: bit 1
        b2SetBit(ref setB, 64); // setB: bit 64

        // Perform union: setA |= setB
        b2InPlaceUnion(ref setA, ref setB);

        // setA should now have both bit 1 and bit 64
        Assert.That(b2GetBit(ref setA, 1), Is.True);
        Assert.That(b2GetBit(ref setA, 64), Is.True);

        // Confirm setB remains unchanged
        Assert.That(b2GetBit(ref setB, 64), Is.True);
        Assert.That(b2GetBit(ref setB, 1), Is.False);

        // Fail case: mismatched blockCount
        var setMismatch = b2CreateBitSet(256); // 4 blocks
        b2SetBitCountAndClear(ref setMismatch, 256);

        Assert.Throws<InvalidOperationException>(() =>
        {
            b2InPlaceUnion(ref setA, ref setMismatch);
        });
    }
}