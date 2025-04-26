using System;
using NUnit.Framework;
using static Box2D.NET.B2BoardPhases;
using static Box2D.NET.B2Tables;
using static Box2D.NET.B2Atomics;

namespace Box2D.NET.Test;

public class B2BoardPhasesTests
{
    [Test]
    public void Test_B2BoardPhases_B2_PROXY_ID_TYPE_KEY()
    {
        int max = int.MaxValue >> 2;
        for (int id = 0; id < max; id += 997)
        {
            foreach (B2BodyType type in Enum.GetValues(typeof(B2BodyType)))
            {
                int key = B2_PROXY_KEY(id, type);
                Assert.That(B2_PROXY_ID(key), Is.EqualTo(id));
                Assert.That(B2_PROXY_TYPE(key), Is.EqualTo(type));
            }
        }
    }

    [Test]
    public void Test_B2BoardPhases_b2BufferMove()
    {
        // Arrange: Create a new BroadPhase object with an empty moveSet and moveArray
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        int proxyKey = 42;

        // Act 1: Call b2BufferMove for the first time. It should add to moveArray
        b2BufferMove(bp, proxyKey);

        Assert.That(b2ContainsKey(ref bp.moveSet, (ulong)proxyKey + 1), Is.True, "moveSet should contain proxyKey + 1");
        Assert.That(bp.moveArray.count, Is.EqualTo(1), "moveArray should have exactly one element after the first call");
        Assert.That(bp.moveArray.data[0], Is.EqualTo(proxyKey), "moveArray should contain the correct proxyKey");

        // Act 2: Call b2BufferMove again with the same proxyKey. It should not add to moveArray again
        b2BufferMove(bp, proxyKey);

        Assert.That(bp.moveArray.count, Is.EqualTo(1), "moveArray should not change size on subsequent calls with the same proxyKey");
    }

    [Test]
    public void Test_Test_B2BoardPhases_b2CreateBroadPhase()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        Assert.That(bp, Is.Not.Null, "B2BroadPhase object should be initialized.");
        Assert.That(bp.trees.Length, Is.EqualTo((int)B2BodyType.b2_bodyTypeCount), "The trees array should have the same length as B2BodyType.b2_bodyTypeCount.");
        Assert.That(bp.moveSet.capacity, Is.EqualTo(16), "moveSet should be initialized.");
        Assert.That(bp.moveArray.capacity, Is.EqualTo(16), "moveArray should be initialized.");
        Assert.That(bp.moveResults.Array, Is.Null, "moveResults should be null initially.");
        Assert.That(bp.movePairs.Array, Is.Null, "movePairs should be null initially.");
        Assert.That(bp.movePairCapacity, Is.EqualTo(0), "movePairCapacity should be 0 initially.");
        Assert.That(b2AtomicLoadInt(ref bp.movePairIndex), Is.EqualTo(0), "movePairIndex should be initialized to 0.");
        Assert.That(bp.pairSet.capacity, Is.EqualTo(32), "pairSet should be initialized.");
        foreach (var tree in bp.trees)
        {
            Assert.That(tree, Is.Not.Null, "Each tree in the trees array should be initialized.");
        }
    }

    [Test]
    public void Test_Test_B2BoardPhases_b2DestroyBroadPhase()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        // test!
        b2DestroyBroadPhase(bp);

        Assert.That(bp.trees, Is.Null);
        Assert.That(bp.proxyCount, Is.EqualTo(0));
        Assert.That(bp.moveSet.capacity, Is.EqualTo(0));
        Assert.That(bp.moveArray.count, Is.EqualTo(0));
        Assert.That(bp.moveResults.Array, Is.Null);
        Assert.That(bp.movePairs.Array, Is.Null);
        Assert.That(bp.movePairCapacity, Is.EqualTo(0));
        Assert.That(bp.movePairIndex.value, Is.EqualTo(0));
        Assert.That(bp.pairSet.capacity, Is.EqualTo(0));
    }

    [Test]
    public void Test_B2BoardPhases_b2UnBufferMove()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);
        
        int proxyKeyA = 42;
        int proxyKeyB = 99;
        int proxyKeyC = 88;

        b2BufferMove(bp, proxyKeyA);
        b2BufferMove(bp, proxyKeyB);

        Assert.That(bp.moveSet.count, Is.EqualTo(2));
        Assert.That(bp.moveArray.count, Is.EqualTo(2));

        // not found key C
        b2UnBufferMove(bp, proxyKeyC);

        Assert.That(bp.moveSet.count, Is.EqualTo(2));
        Assert.That(b2ContainsKey(ref bp.moveSet, (ulong)proxyKeyA + 1), Is.True);
        Assert.That(b2ContainsKey(ref bp.moveSet, (ulong)proxyKeyB + 1), Is.True);
        Assert.That(b2ContainsKey(ref bp.moveSet, (ulong)proxyKeyC + 1), Is.False);

        Assert.That(bp.moveArray.count, Is.EqualTo(2));
        Assert.That(bp.moveArray.data, Does.Contain(proxyKeyA));
        Assert.That(bp.moveArray.data, Does.Contain(proxyKeyB));
        Assert.That(bp.moveArray.data, Does.Not.Contain(proxyKeyC));

        // delete A
        b2UnBufferMove(bp, proxyKeyA);

        Assert.That(bp.moveSet.count, Is.EqualTo(1));
        Assert.That(b2ContainsKey(ref bp.moveSet, (ulong)proxyKeyA + 1), Is.False);
        Assert.That(b2ContainsKey(ref bp.moveSet, (ulong)proxyKeyB + 1), Is.True);
        Assert.That(b2ContainsKey(ref bp.moveSet, (ulong)proxyKeyC + 1), Is.False);

        Assert.That(bp.moveArray.count, Is.EqualTo(1));
        Assert.That(bp.moveArray.data, Does.Not.Contain(proxyKeyA));
        Assert.That(bp.moveArray.data, Does.Contain(proxyKeyB));
        Assert.That(bp.moveArray.data, Does.Not.Contain(proxyKeyC));

        // delete B
        b2UnBufferMove(bp, proxyKeyB);

        Assert.That(bp.moveSet.count, Is.EqualTo(0));
        Assert.That(b2ContainsKey(ref bp.moveSet, (ulong)proxyKeyA + 1), Is.False);
        Assert.That(b2ContainsKey(ref bp.moveSet, (ulong)proxyKeyB + 1), Is.False);
        Assert.That(b2ContainsKey(ref bp.moveSet, (ulong)proxyKeyC + 1), Is.False);
        
        Assert.That(bp.moveArray.count, Is.EqualTo(0));
    }
}