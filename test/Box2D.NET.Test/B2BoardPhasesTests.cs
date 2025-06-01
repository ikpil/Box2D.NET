// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using Box2D.NET.Test.Helpers;
using NUnit.Framework;
using static Box2D.NET.B2BoardPhases;
using static Box2D.NET.B2Tables;
using static Box2D.NET.B2Atomics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Worlds;

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

        // Destroy proxy A
        b2BroadPhase_DestroyProxy(bp, proxyKeyA);

        Assert.That(bp.moveSet.count, Is.EqualTo(1), "moveSet should have 1 item after destroying proxy A");
        Assert.That(bp.moveArray.count, Is.EqualTo(1), "moveArray should have 1 item after destroying proxy A");

        // Destroy proxy B
        b2BroadPhase_DestroyProxy(bp, proxyKeyB);

        Assert.That(bp.moveSet.count, Is.EqualTo(0), "moveSet should have 0 items after destroying proxy B");
        Assert.That(bp.moveArray.count, Is.EqualTo(0), "moveArray should have 0 items after destroying proxy B");

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

    [Test]
    public void Test_B2BroadPhases_b2BroadPhase_b2PairQueryCallback()
    {
        // Arrange
        using B2TestContext context = B2TestContext.CreateFor();
        var worldId = context.WorldId;
        var world = b2GetWorldFromId(worldId);

        B2ShapeId shapeIdA = B2TestHelper.Circle(worldId, new B2Vec2(0.0f, 0.0f), 1.0f, B2BodyType.b2_dynamicBody);
        B2ShapeId shapeIdB = B2TestHelper.Circle(worldId, new B2Vec2(1.0f, 1.0f), 2.0f, B2BodyType.b2_dynamicBody);
        B2ShapeId shapeIdC = B2TestHelper.Circle(worldId, new B2Vec2(50.0f, 40.0f), 1.0f, B2BodyType.b2_dynamicBody);

        var proxyKeyA = world.broadPhase.moveArray.data[0];
        var proxyKeyB = world.broadPhase.moveArray.data[1];
        var proxyKeyC = world.broadPhase.moveArray.data[2];

        B2BodyType proxyTypeA = B2_PROXY_TYPE(proxyKeyA);
        B2BodyType proxyTypeB = B2_PROXY_TYPE(proxyKeyB);
        B2BodyType proxyTypeC = B2_PROXY_TYPE(proxyKeyC);

        var proxyIdA = B2_PROXY_ID(proxyKeyA);
        var proxyIdB = B2_PROXY_ID(proxyKeyB);
        var proxyIdC = B2_PROXY_ID(proxyKeyC);

        // QueryContext setting
        B2QueryPairContext queryContext = new B2QueryPairContext();
        queryContext.world = world;
        queryContext.queryTreeType = proxyTypeA;
        queryContext.queryProxyKey = proxyKeyA;
        queryContext.queryShapeIndex = shapeIdA.index1 - 1;

        // a <-> a
        {
            queryContext.moveResult = new B2MoveResult();
            bool result = b2PairQueryCallback(proxyIdA, (ulong)shapeIdA.index1 - 1, ref queryContext);
            Assert.That(result, Is.True);
            Assert.That(queryContext.moveResult.pairList, Is.Null);
        }

        // a <-> b
        {
            queryContext.moveResult = new B2MoveResult();
            bool result = b2PairQueryCallback(proxyIdB, (ulong)shapeIdB.index1 - 1, ref queryContext);

            Assert.That(result, Is.True);
            Assert.That(queryContext.moveResult.pairList, Is.Not.Null);
            Assert.That(queryContext.moveResult.pairList.shapeIndexA, Is.EqualTo(queryContext.queryShapeIndex));
            Assert.That(queryContext.moveResult.pairList.shapeIndexB, Is.EqualTo((ulong)shapeIdB.index1 - 1));
        }

        // a <-> c
        {
            queryContext.moveResult = new B2MoveResult();
            bool result = b2PairQueryCallback(proxyIdC, (ulong)shapeIdC.index1 - 1, ref queryContext);

            Assert.That(result, Is.True);
            Assert.That(queryContext.moveResult.pairList, Is.Not.Null);
            Assert.That(queryContext.moveResult.pairList.shapeIndexA, Is.EqualTo(queryContext.queryShapeIndex));
            Assert.That(queryContext.moveResult.pairList.shapeIndexB, Is.EqualTo((ulong)shapeIdC.index1 - 1));
        }
    }

    [Test]
    public void Test_B2BoardPhases_b2FindPairsTask()
    {
        // Arrange
        using B2TestContext context = B2TestContext.CreateFor();
        var worldId = context.WorldId;
        var world = b2GetWorldFromId(worldId);

        // Create shapes in the world
        B2ShapeId shapeIdA = B2TestHelper.Circle(worldId, new B2Vec2(0.0f, 0.0f), 1.0f, B2BodyType.b2_dynamicBody);
        B2ShapeId shapeIdB = B2TestHelper.Circle(worldId, new B2Vec2(1.0f, 1.0f), 2.0f, B2BodyType.b2_dynamicBody);
        B2ShapeId shapeIdC = B2TestHelper.Circle(worldId, new B2Vec2(50.0f, 40.0f), 1.0f, B2BodyType.b2_dynamicBody);

        // Get proxy keys from the created shapes
        var proxyKeyA = world.broadPhase.moveArray.data[0];
        var proxyKeyB = world.broadPhase.moveArray.data[1];
        var proxyKeyC = world.broadPhase.moveArray.data[2];

        // Add A to moveArray (it will collide with B but not C)
        b2BufferMove(world.broadPhase, proxyKeyA);

        // Allocate moveResults array
        world.broadPhase.moveResults = new B2MoveResult[1];
        world.broadPhase.moveResults[0] = new B2MoveResult { pairList = null };

        // Act: Call b2FindPairsTask
        b2FindPairsTask(0, 1, 0, world);

        // Assert: Check that A collides with B but not C
        B2MovePair pair = world.broadPhase.moveResults[0].pairList;
        Assert.That(pair, Is.Not.Null, "Should find at least one collision pair");
        Assert.That(pair.shapeIndexA, Is.EqualTo(shapeIdA.index1 - 1), "First shape should be A");
        Assert.That(pair.shapeIndexB, Is.EqualTo(shapeIdB.index1 - 1), "Second shape should be B");
    }

    [Test]
    public void Test_B2BoardPhases_b2FindPairsTask_NoCollision()
    {
        // Arrange
        using B2TestContext context = B2TestContext.CreateFor();
        var worldId = context.WorldId;
        var world = b2GetWorldFromId(worldId);

        // Create shapes in the world that don't overlap
        B2ShapeId shapeIdA = B2TestHelper.Circle(worldId, new B2Vec2(0.0f, 0.0f), 1.0f, B2BodyType.b2_dynamicBody);
        B2ShapeId shapeIdB = B2TestHelper.Circle(worldId, new B2Vec2(50.0f, 50.0f), 1.0f, B2BodyType.b2_dynamicBody);

        // Get proxy keys from the created shapes
        var proxyKeyA = world.broadPhase.moveArray.data[0];
        var proxyKeyB = world.broadPhase.moveArray.data[1];

        // Add A to moveArray
        b2BufferMove(world.broadPhase, proxyKeyA);

        // Allocate moveResults array
        world.broadPhase.moveResults = new B2MoveResult[1];
        world.broadPhase.moveResults[0] = new B2MoveResult { pairList = null };

        // Act: Call b2FindPairsTask
        b2FindPairsTask(0, 1, 0, world);

        // Assert: Check that no collision pairs were found
        Assert.That(world.broadPhase.moveResults[0].pairList, Is.Null, "Should not find any collision pairs");
    }

    [Test]
    public void Test_B2BoardPhases_b2FindPairsTask_DynamicDynamicCollision()
    {
        // Arrange
        using B2TestContext context = B2TestContext.CreateFor();
        var worldId = context.WorldId;
        var world = b2GetWorldFromId(worldId);

        // Create two dynamic shapes that overlap
        B2ShapeId shapeIdA = B2TestHelper.Circle(worldId, new B2Vec2(0.0f, 0.0f), 1.0f, B2BodyType.b2_dynamicBody);
        B2ShapeId shapeIdB = B2TestHelper.Circle(worldId, new B2Vec2(1.0f, 1.0f), 1.0f, B2BodyType.b2_dynamicBody);

        // Get proxy keys from the created shapes
        var proxyKeyA = world.broadPhase.moveArray.data[0];
        var proxyKeyB = world.broadPhase.moveArray.data[1];

        // Add A to moveArray
        b2BufferMove(world.broadPhase, proxyKeyA);

        // Allocate moveResults array
        world.broadPhase.moveResults = new B2MoveResult[1];
        world.broadPhase.moveResults[0] = new B2MoveResult { pairList = null };

        // Act: Call b2FindPairsTask
        b2FindPairsTask(0, 1, 0, world);

        // Assert: Check that A collides with B
        B2MovePair pair = world.broadPhase.moveResults[0].pairList;
        Assert.That(pair, Is.Not.Null, "Should find collision pair between dynamic bodies");
        Assert.That(pair.shapeIndexA, Is.EqualTo(shapeIdA.index1 - 1), "First shape should be A");
        Assert.That(pair.shapeIndexB, Is.EqualTo(shapeIdB.index1 - 1), "Second shape should be B");
    }

    [Test]
    public void Test_B2BoardPhases_b2UpdateBroadPhasePairs()
    {
        // Arrange
        using B2TestContext context = B2TestContext.CreateFor();
        var worldId = context.WorldId;
        var world = b2GetWorldFromId(worldId);

        // Create test shapes with different collision scenarios:
        // A and B: Dynamic-Dynamic collision (overlapping)
        // C and D: Static-Dynamic collision (overlapping)
        // E: Isolated dynamic body (no collision)
        B2ShapeId shapeIdA = B2TestHelper.Circle(worldId, new B2Vec2(0.0f, 0.0f), 1.0f, B2BodyType.b2_dynamicBody);
        B2ShapeId shapeIdB = B2TestHelper.Circle(worldId, new B2Vec2(0.5f, 0.0f), 1.0f, B2BodyType.b2_dynamicBody); // Moved closer to A
        B2ShapeId shapeIdC = B2TestHelper.Circle(worldId, new B2Vec2(3.0f, 0.0f), 1.0f, B2BodyType.b2_staticBody);
        B2ShapeId shapeIdD = B2TestHelper.Circle(worldId, new B2Vec2(3.5f, 0.0f), 1.0f, B2BodyType.b2_dynamicBody); // Moved closer to C
        B2ShapeId shapeIdE = B2TestHelper.Circle(worldId, new B2Vec2(50.0f, 50.0f), 1.0f, B2BodyType.b2_dynamicBody);

        // Get proxy keys for each shape
        var proxyKeyA = world.broadPhase.moveArray.data[0];
        var proxyKeyB = world.broadPhase.moveArray.data[1];
        var proxyKeyC = world.broadPhase.moveArray.data[2];
        var proxyKeyD = world.broadPhase.moveArray.data[3];
        var proxyKeyE = world.broadPhase.moveArray.data[4];

        b2BufferMove(world.broadPhase, proxyKeyA); // Dynamic A
        b2BufferMove(world.broadPhase, proxyKeyD); // Dynamic D (will collide with static C)

        // Act: Update broad phase pairs to detect collisions
        b2UpdateBroadPhasePairs(world);

        // Assert

        // 1. Memory Management Verification
        // - Verify that all temporary buffers are properly cleared
        // - Check that all allocated memory is freed
        // - Ensure indices are reset to initial state
        Assert.That(world.broadPhase.moveArray.count, Is.EqualTo(0), "moveArray should be cleared");
        Assert.That(world.broadPhase.moveSet.count, Is.EqualTo(0), "moveSet should be cleared");
        Assert.That(world.broadPhase.moveResults.Array, Is.Null, "moveResults should be freed");
        Assert.That(world.broadPhase.movePairs.Array, Is.Null, "movePairs should be freed");
        Assert.That(world.broadPhase.movePairIndex.value, Is.EqualTo(2), "movePairIndex should reflect the number of collision pairs created");

        // 2. Collision Detection Results
        // - Verify that exactly two collision pairs are created:
        //   1. Between dynamic shapes A and B
        //   2. Between static shape C and dynamic shape D
        Assert.That(world.contacts.count, Is.EqualTo(2),
            "Should create exactly two contacts: one for A-B collision and one for C-D collision");

        // 3. Contact Pair Verification
        // - Verify that the correct shapes are paired in collisions
        // - Check both dynamic-dynamic and static-dynamic collision pairs
        Assert.That(world.contacts.data, Is.Not.Null, "Should have contacts array");
        Assert.That(world.contacts.count, Is.EqualTo(2), "Should have exactly two contacts");

        // Verify A-B contact (Dynamic-Dynamic collision)
        bool foundAB = false;
        bool foundCD = false;
        for (int i = 0; i < world.contacts.count; i++)
        {
            var contact = world.contacts.data[i];
            if ((contact.shapeIdA == shapeIdA.index1 - 1 && contact.shapeIdB == shapeIdB.index1 - 1) ||
                (contact.shapeIdA == shapeIdB.index1 - 1 && contact.shapeIdB == shapeIdA.index1 - 1))
            {
                foundAB = true;
            }
            else if ((contact.shapeIdA == shapeIdC.index1 - 1 && contact.shapeIdB == shapeIdD.index1 - 1) ||
                     (contact.shapeIdA == shapeIdD.index1 - 1 && contact.shapeIdB == shapeIdC.index1 - 1))
            {
                foundCD = true;
            }
        }

        Assert.That(foundAB, Is.True, "Should have contact between shapes A and B");
        Assert.That(foundCD, Is.True, "Should have contact between shapes C and D");
    }

    [Test]
    public void Test_B2BoardPhases_b2BroadPhase_TestOverlap()
    {
        // Arrange: Create a new BroadPhase object
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        // Test 1: Overlapping dynamic bodies
        // Purpose: Verify that two overlapping dynamic bodies are detected as colliding
        var aabb1 = new B2AABB
        {
            lowerBound = new B2Vec2(0, 0),
            upperBound = new B2Vec2(1, 1)
        };
        var aabb2 = new B2AABB
        {
            lowerBound = new B2Vec2(0.5f, 0.5f),
            upperBound = new B2Vec2(1.5f, 1.5f)
        };
        int proxyKey1 = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb1, 0x0001, 1, false);
        int proxyKey2 = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb2, 0x0001, 2, false);
        Assert.That(b2BroadPhase_TestOverlap(bp, proxyKey1, proxyKey2), Is.True, "Overlapping dynamic bodies should be detected");

        // Test 2: Non-overlapping bodies
        // Purpose: Verify that non-overlapping bodies are not detected as colliding
        var aabb3 = new B2AABB
        {
            lowerBound = new B2Vec2(10, 10),
            upperBound = new B2Vec2(11, 11)
        };
        int proxyKey3 = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb3, 0x0001, 3, false);
        Assert.That(b2BroadPhase_TestOverlap(bp, proxyKey1, proxyKey3), Is.False, "Non-overlapping bodies should not be detected");

        // Test 3: Static-Dynamic overlap
        // Purpose: Verify that static and dynamic bodies can be tested for overlap
        var aabb4 = new B2AABB
        {
            lowerBound = new B2Vec2(0.25f, 0.25f),
            upperBound = new B2Vec2(0.75f, 0.75f)
        };
        int proxyKey4 = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_staticBody, aabb4, 0x0001, 4, false);
        Assert.That(b2BroadPhase_TestOverlap(bp, proxyKey1, proxyKey4), Is.True, "Static-Dynamic overlap should be detected");

        // Test 4: Edge case - touching but not overlapping
        // Purpose: Verify that bodies that just touch at edges are considered overlapping in Box2D
        var aabb5 = new B2AABB
        {
            lowerBound = new B2Vec2(1, 0),
            upperBound = new B2Vec2(2, 1)
        };
        int proxyKey5 = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb5, 0x0001, 5, false);
        Assert.That(b2BroadPhase_TestOverlap(bp, proxyKey1, proxyKey5), Is.True,
            "Bodies touching at edges should be considered overlapping in Box2D");

        // Test 4.1: Truly non-overlapping bodies
        // Purpose: Verify that bodies with a gap between them are not considered overlapping
        var aabb6 = new B2AABB
        {
            lowerBound = new B2Vec2(1.1f, 0),
            upperBound = new B2Vec2(2.1f, 1)
        };
        int proxyKey6 = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb6, 0x0001, 6, false);
        Assert.That(b2BroadPhase_TestOverlap(bp, proxyKey1, proxyKey6), Is.False,
            "Bodies with a gap between them should not be considered overlapping");

#if DEBUG
        // Test 5: Invalid proxy keys
        // Purpose: Verify that invalid proxy keys are handled gracefully
        int invalidProxyKey = B2_PROXY_KEY(9999, B2BodyType.b2_dynamicBody);
        Assert.Throws<InvalidOperationException>(() => b2BroadPhase_TestOverlap(bp, proxyKey1, invalidProxyKey),
            "Testing overlap with invalid proxy key should throw exception");
#endif

        // Test 6: Same proxy key
        // Purpose: Verify that a proxy is considered to overlap with itself
        Assert.That(b2BroadPhase_TestOverlap(bp, proxyKey1, proxyKey1), Is.True, 
            "A proxy should be considered to overlap with itself");
    }

    [Test]
    public void Test_B2BoardPhases_b2BroadPhase_GetShapeIndex()
    {
        // Arrange: Create a new BroadPhase object
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        // Create a proxy with a known shape index
        var aabb = new B2AABB
        {
            lowerBound = new B2Vec2(0, 0),
            upperBound = new B2Vec2(1, 1)
        };
        int shapeIndex = 123;
        int proxyKey = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb, 0x0001, shapeIndex, false);

        // Act & Assert: Verify that the shape index is correctly extracted
        Assert.That(b2BroadPhase_GetShapeIndex(bp, proxyKey), Is.EqualTo(shapeIndex), 
            "Should correctly extract shape index from proxy key");
    }
}
