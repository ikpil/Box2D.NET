// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using Box2D.NET.Test.Helpers;
using NUnit.Framework;
using static Box2D.NET.B2BroadPhases;
using static Box2D.NET.B2DynamicTrees;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Diagnostics;

namespace Box2D.NET.Test;

public class B2BroadPhasesTests
{
    // A proxy is marked when its leaf carries the moved flag
    private static bool IsMoved(B2BroadPhase bp, int proxyKey)
    {
        B2DynamicTree tree = bp.trees[(int)B2_PROXY_TYPE(proxyKey)];
        int proxyId = B2_PROXY_ID(proxyKey);
        return b2IsNodeMoved(tree.nodes[tree.proxies[proxyId].node]);
    }

    private static int GetProxyCount(B2BroadPhase bp)
    {
        int count = 0;
        for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
        {
            count += b2DynamicTree_GetProxyCount(bp.trees[i]);
        }

        return count;
    }

    private static B2AABB Box(float x0, float y0, float x1, float y1)
    {
        return new B2AABB(new B2Vec2(x0, y0), new B2Vec2(x1, y1));
    }

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
    public void Test_Test_B2BoardPhases_b2CreateBroadPhase()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        Assert.That(bp, Is.Not.Null, "B2BroadPhase object should be initialized.");
        Assert.That(bp.trees.Length, Is.EqualTo((int)B2BodyType.b2_bodyTypeCount), "The trees array should have the same length as B2BodyType.b2_bodyTypeCount.");
        Assert.That(bp.pairSet.capacity, Is.EqualTo(32), "pairSet should be initialized.");
        Assert.That(bp.movedSiblings.Array, Is.Null, "movedSiblings should be null initially.");
        foreach (var tree in bp.trees)
        {
            Assert.That(tree, Is.Not.Null, "Each tree in the trees array should be initialized.");
            Assert.That(b2HasTreeMoved(tree), Is.False, "A new tree has no marks.");
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
        Assert.That(bp.movedSiblings.Array, Is.Null);
        Assert.That(bp.pairSet.capacity, Is.EqualTo(0));
    }

    [Test]
    public void Test_B2BoardPhases_b2BroadPhase_CreateProxy()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        B2AABB aabb = Box(0, 0, 1, 1);
        ulong categoryBits = 0x0001;
        int shapeIndex = 123;

        // case 1: static body, forcePairCreation = false
        int proxyKeyA = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_staticBody, aabb, categoryBits, shapeIndex, false);

        Assert.That(IsMoved(bp, proxyKeyA), Is.False, "A static proxy is not marked unless pair creation is forced");
        Assert.That(b2HasTreeMoved(bp.trees[(int)B2BodyType.b2_staticBody]), Is.False);

        // case 2: static body, forcePairCreation = true
        int proxyKeyB = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_staticBody, aabb, categoryBits, shapeIndex + 1, true);

        Assert.That(IsMoved(bp, proxyKeyB), Is.True, "A static proxy is marked when pair creation is forced");
        Assert.That(b2HasTreeMoved(bp.trees[(int)B2BodyType.b2_staticBody]), Is.True);

        // case 3: dynamic body, forcePairCreation irrelevant
        int proxyKeyC = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb, categoryBits, shapeIndex + 2, false);

        Assert.That(IsMoved(bp, proxyKeyC), Is.True, "A dynamic proxy is always marked");
        Assert.That(B2_PROXY_TYPE(proxyKeyC), Is.EqualTo(B2BodyType.b2_dynamicBody));

        // The shape index is the user data
        Assert.That(b2DynamicTree_GetUserData(bp.trees[(int)B2BodyType.b2_dynamicBody], B2_PROXY_ID(proxyKeyC)), Is.EqualTo((ulong)(shapeIndex + 2)));
        Assert.That(GetProxyCount(bp), Is.EqualTo(3));
    }

    [Test]
    public void Test_B2BoardPhases_b2BroadPhase_DestroyProxy()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        B2AABB aabb = Box(0, 0, 1, 1);
        ulong categoryBits = 0x0001;
        int shapeIndex = 123;

        int proxyKeyA = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb, categoryBits, shapeIndex, false);
        int proxyKeyB = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_dynamicBody, aabb, categoryBits, shapeIndex + 1, false);

        B2DynamicTree tree = bp.trees[(int)B2BodyType.b2_dynamicBody];
        Assert.That(GetProxyCount(bp), Is.EqualTo(2));
        Assert.That(b2HasTreeMoved(tree), Is.True);

        // Destroy proxy A, the survivor keeps its mark
        b2BroadPhase_DestroyProxy(bp, proxyKeyA);

        Assert.That(GetProxyCount(bp), Is.EqualTo(1));
        Assert.That(IsMoved(bp, proxyKeyB), Is.True);
        Assert.That(b2HasTreeMoved(tree), Is.True);

        // Destroy proxy B, the marks go with it
        b2BroadPhase_DestroyProxy(bp, proxyKeyB);

        Assert.That(GetProxyCount(bp), Is.EqualTo(0));
        Assert.That(b2HasTreeMoved(tree), Is.False);
    }

    [Test]
    public void Test_B2BoardPhases_b2BroadPhase_MoveProxy()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        B2AABB aabb1 = Box(0, 0, 1, 1);
        B2AABB aabb2 = Box(10, 10, 11, 11);

        // A static proxy starts unmarked and a move marks it
        int proxyKey = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_staticBody, aabb1, 0x0001, 1, false);
        B2DynamicTree tree = bp.trees[(int)B2_PROXY_TYPE(proxyKey)];
        int proxyId = B2_PROXY_ID(proxyKey);
        Assert.That(IsMoved(bp, proxyKey), Is.False);

        b2BroadPhase_MoveProxy(bp, proxyKey, aabb2);

        Assert.That(b2DynamicTree_GetAABB(tree, proxyId), Is.EqualTo(aabb2));
        Assert.That(IsMoved(bp, proxyKey), Is.True);
        Assert.That(b2HasTreeMoved(tree), Is.True);
    }

    [Test]
    public void Test_B2BoardPhases_b2BroadPhase_MarkProxyMoved()
    {
        B2BroadPhase bp = null;
        b2CreateBroadPhase(ref bp);

        B2AABB aabb1 = Box(0, 0, 1, 1);
        B2AABB aabb2 = Box(-1, -1, 2, 2);

        int proxyKeyA = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_staticBody, aabb1, 0x0001, 1, false);
        int proxyKeyB = b2BroadPhase_CreateProxy(bp, B2BodyType.b2_staticBody, Box(5, 5, 6, 6), 0x0001, 2, false);
        Assert.That(IsMoved(bp, proxyKeyA), Is.False);

        // The serial mark leaves the box alone
        b2BroadPhase_MarkProxyMovedSerial(bp, proxyKeyA);
        Assert.That(IsMoved(bp, proxyKeyA), Is.True);
        Assert.That(IsMoved(bp, proxyKeyB), Is.False);
        Assert.That(b2DynamicTree_GetAABB(bp.trees[(int)B2BodyType.b2_staticBody], B2_PROXY_ID(proxyKeyA)), Is.EqualTo(aabb1));

        // The enlarging mark stores the fat box in the leaf
        b2BroadPhase_MarkProxyMoved(bp, proxyKeyB, aabb2);
        Assert.That(IsMoved(bp, proxyKeyB), Is.True);
        Assert.That(b2DynamicTree_GetAABB(bp.trees[(int)B2BodyType.b2_staticBody], B2_PROXY_ID(proxyKeyB)), Is.EqualTo(aabb2));
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
        B2ShapeId shapeIdB = B2TestHelper.Circle(worldId, new B2Vec2(0.5f, 0.0f), 1.0f, B2BodyType.b2_dynamicBody);
        B2ShapeId shapeIdC = B2TestHelper.Circle(worldId, new B2Vec2(3.0f, 0.0f), 1.0f, B2BodyType.b2_staticBody);
        B2ShapeId shapeIdD = B2TestHelper.Circle(worldId, new B2Vec2(3.5f, 0.0f), 1.0f, B2BodyType.b2_dynamicBody);
        B2ShapeId shapeIdE = B2TestHelper.Circle(worldId, new B2Vec2(50.0f, 50.0f), 1.0f, B2BodyType.b2_dynamicBody);

        Assert.That(b2HasTreeMoved(world.broadPhase.trees[(int)B2BodyType.b2_dynamicBody]), Is.True);

        // Act: Update broad phase pairs to detect collisions
        b2UpdateBroadPhasePairs(world);

        // Assert

        // 1. Every mark is consumed
        for (int i = 0; i < (int)B2BodyType.b2_bodyTypeCount; ++i)
        {
            Assert.That(b2HasTreeMoved(world.broadPhase.trees[i]), Is.False, "marks should be cleared");
        }

        // 2. Exactly two pairs: A-B and C-D
        Assert.That(world.contacts.count, Is.EqualTo(2),
            "Should create exactly two contacts: one for A-B collision and one for C-D collision");

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

        // 3. A second update finds nothing new
        b2UpdateBroadPhasePairs(world);
        Assert.That(world.contacts.count, Is.EqualTo(2));
        B2_UNUSED(shapeIdE);
    }
}
