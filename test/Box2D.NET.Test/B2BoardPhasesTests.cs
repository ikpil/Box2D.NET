using System;
using NUnit.Framework;
using static Box2D.NET.B2BoardPhases;
using static Box2D.NET.B2Tables;
using static Box2D.NET.B2Atomics;
using static Box2D.NET.B2Constants;

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

    [Test]
    public void Test_B2BoardPhases_b2BroadPhase_CreateProxy()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        var aabb = new B2AABB
        {
            lowerBound = new B2Vec2(0, 0),
            upperBound = new B2Vec2(1, 1)
        };
        ulong categoryBits = 0x0001;
        int shapeIndex = 123;

        // case 1: static body, forcePairCreation = false
        int proxyKeyA = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_staticBody, aabb, categoryBits, shapeIndex, false);

        Assert.That(bp.moveSet.count, Is.EqualTo(0), "Should not add to moveSet if static body and forcePairCreation == false");
        Assert.That(bp.moveArray.count, Is.EqualTo(0), "Should not add to moveArray if static body and forcePairCreation == false");

        // case 2: static body, forcePairCreation = true
        int proxyKeyB = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_staticBody, aabb, categoryBits, shapeIndex + 1, true);

        Assert.That(bp.moveSet.count, Is.EqualTo(1), "Should add to moveSet if static body but forcePairCreation == true");
        Assert.That(bp.moveArray.count, Is.EqualTo(1), "Should add to moveArray if static body but forcePairCreation == true");
        Assert.That(bp.moveArray.data, Does.Contain(proxyKeyB), "moveArray should contain proxyKeyB");

        // case 3: dynamic body, forcePairCreation irrelevant
        int proxyKeyC = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb, categoryBits, shapeIndex + 2, false);

        Assert.That(bp.moveSet.count, Is.EqualTo(2), "Should add to moveSet if body is dynamic");
        Assert.That(bp.moveArray.count, Is.EqualTo(2), "Should add to moveArray if body is dynamic");
        Assert.That(bp.moveArray.data, Does.Contain(proxyKeyC), "moveArray should contain proxyKeyC");
    }

    [Test]
    public void Test_B2BoardPhases_b2BroadPhase_DestroyProxy()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        var aabb = new B2AABB
        {
            lowerBound = new B2Vec2(0, 0),
            upperBound = new B2Vec2(1, 1)
        };
        ulong categoryBits = 0x0001;
        int shapeIndex = 123;

        // Create and buffer proxies
        int proxyKeyA = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb, categoryBits, shapeIndex, false);
        int proxyKeyB = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb, categoryBits, shapeIndex + 1, false);

        Assert.That(bp.moveSet.count, Is.EqualTo(2), "moveSet should have 2 items before destroying proxies");
        Assert.That(bp.moveArray.count, Is.EqualTo(2), "moveArray should have 2 items before destroying proxies");
        Assert.That(bp.proxyCount, Is.EqualTo(0));

        // Destroy proxy A
        b2BroadPhase_DestroyProxy(bp, proxyKeyA);

        Assert.That(bp.moveSet.count, Is.EqualTo(1), "moveSet should have 1 item after destroying proxy A");
        Assert.That(bp.moveArray.count, Is.EqualTo(1), "moveArray should have 1 item after destroying proxy A");
        Assert.That(bp.proxyCount, Is.EqualTo(-1), "proxyCount should decrease after destroying a proxy");

        // Destroy proxy B
        b2BroadPhase_DestroyProxy(bp, proxyKeyB);

        Assert.That(bp.moveSet.count, Is.EqualTo(0), "moveSet should have 0 items after destroying proxy B");
        Assert.That(bp.moveArray.count, Is.EqualTo(0), "moveArray should have 0 items after destroying proxy B");
        Assert.That(bp.proxyCount, Is.EqualTo(-2), "proxyCount should decrease to 0 after destroying all proxies");

#if DEBUG
        // Case 3: Destroy a non-existent proxy
        Assert.Throws<InvalidOperationException>(() => b2BroadPhase_DestroyProxy(bp, proxyKeyB), "Destroying a non-existent proxy should throw an exception");
#endif
    }

    [Test]
    public void Test_B2BoardPhases_b2BroadPhase_MoveProxy()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        var aabb1 = new B2AABB
        {
            lowerBound = new B2Vec2(0, 0),
            upperBound = new B2Vec2(1, 1)
        };

        var aabb2 = new B2AABB
        {
            lowerBound = new B2Vec2(10, 10),
            upperBound = new B2Vec2(11, 11)
        };

        ulong categoryBits = 0x0001;
        int shapeIndex = 1;

        int proxyKey = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb1, categoryBits, shapeIndex, false);
        B2BodyType proxyType = B2_PROXY_TYPE(proxyKey);
        int proxyId = B2_PROXY_ID(proxyKey);

        {
            // move valid proxy
            b2BroadPhase_MoveProxy(bp, proxyKey, aabb2);
            Assert.That(bp.trees[(int)proxyType].nodes[proxyId].aabb, Is.EqualTo(aabb2));

            Assert.That(bp.moveSet.count, Is.EqualTo(1));
            Assert.That(bp.moveArray.count, Is.EqualTo(1));
            Assert.That(bp.moveArray.data, Does.Contain(proxyKey));
        }

#if DEBUG
        {
            // move invalid (nonexistent) proxy
            int invalidProxyKey = B2_PROXY_KEY(9999, B2BodyType.b2_dynamicBody);
            Assert.Throws<InvalidOperationException>(() => b2BroadPhase_MoveProxy(bp, invalidProxyKey, aabb2));
        }
#endif
    }

    [Test]
    public void Test_B2BroadPhases_b2BroadPhase_EnlargeProxy()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        B2AABB aabb1 = new B2AABB
        {
            lowerBound = new B2Vec2(0, 0),
            upperBound = new B2Vec2(1, 1)
        };

        B2AABB aabb2 = new B2AABB
        {
            lowerBound = new B2Vec2(-1, -1),
            upperBound = new B2Vec2(2, 2)
        };

        // Create proxy
        int proxyKey = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb1, 0x0001, 123, false);

        {
            // enlarge valid proxy
            b2BroadPhase_EnlargeProxy(bp, proxyKey, aabb2);

            var node = bp.trees[(int)B2_PROXY_TYPE(proxyKey)].nodes[B2_PROXY_ID(proxyKey)];

            Assert.That(node.aabb.lowerBound.X, Is.EqualTo(aabb2.lowerBound.X), "LowerBound.X should match enlarged AABB");
            Assert.That(node.aabb.lowerBound.Y, Is.EqualTo(aabb2.lowerBound.Y), "LowerBound.Y should match enlarged AABB");
            Assert.That(node.aabb.upperBound.X, Is.EqualTo(aabb2.upperBound.X), "UpperBound.X should match enlarged AABB");
            Assert.That(node.aabb.upperBound.Y, Is.EqualTo(aabb2.upperBound.Y), "UpperBound.Y should match enlarged AABB");

            Assert.That(bp.moveSet.count, Is.EqualTo(1), "moveSet should contain one key after enlarge");
            Assert.That(bp.moveArray.count, Is.EqualTo(1), "moveArray should contain one key after enlarge");
            Assert.That(bp.moveArray.data, Does.Contain(proxyKey), "moveArray should contain the enlarged proxyKey");
        }

#if DEBUG
        {
            // enlarge invalid proxy (null index)
            Assert.That(() => b2BroadPhase_EnlargeProxy(bp, B2_NULL_INDEX, aabb2), Throws.Exception, "Enlarging with null proxyKey should throw");

            // enlarge static body
            int staticProxyKey = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_staticBody, aabb1, 0x0001, 124, true);
            Assert.That(() => b2BroadPhase_EnlargeProxy(bp, staticProxyKey, aabb2), Throws.Exception, "Enlarging staticBody proxyKey should throw");
        }
#endif
    }
}